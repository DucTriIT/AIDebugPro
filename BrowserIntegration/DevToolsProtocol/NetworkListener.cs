using Microsoft.Web.WebView2.Core;
using Microsoft.Extensions.Logging;
using AIDebugPro.Core.Interfaces;
using AIDebugPro.Core.Models;
using System.Text.Json;
using System.Collections.Concurrent;

namespace AIDebugPro.BrowserIntegration.DevToolsProtocol;

/// <summary>
/// Listens to network requests and responses
/// </summary>
public class NetworkListener : INetworkListener,IDisposable
{
    private readonly CoreWebView2 _coreWebView;
    private readonly ITelemetryAggregator _telemetryAggregator;
    private readonly ILogger? _logger;
    private Guid _currentSessionId;
    private bool _isListening;
    
    // Track requests in flight
    private readonly ConcurrentDictionary<string, NetworkRequestBuilder> _requests = new();

    public NetworkListener(
        CoreWebView2 coreWebView,
        ITelemetryAggregator telemetryAggregator,
        ILogger? logger = null)
    {
        _coreWebView = coreWebView ?? throw new ArgumentNullException(nameof(coreWebView));
        _telemetryAggregator = telemetryAggregator ?? throw new ArgumentNullException(nameof(telemetryAggregator));
        _logger = logger;
    }

    /// <summary>
    /// Starts listening to network events
    /// </summary>
    public async Task StartAsync(Guid sessionId)
    {
        if (_isListening)
        {
            _logger?.LogWarning("NetworkListener already started");
            return;
        }

        _currentSessionId = sessionId;

        try
        {
            _logger?.LogInformation("?? STARTING NetworkListener for session {SessionId}", sessionId);
            
            // Enable Network domain
            _logger?.LogDebug("Enabling CDP Network domain...");
            await _coreWebView.CallDevToolsProtocolMethodAsync("Network.enable", "{}");
            _logger?.LogDebug("? CDP Network domain enabled");

            // Subscribe to network events
            _logger?.LogDebug("Subscribing to CDP Network events...");
            
            _coreWebView.GetDevToolsProtocolEventReceiver("Network.requestWillBeSent")
                .DevToolsProtocolEventReceived += OnRequestWillBeSent;

            _coreWebView.GetDevToolsProtocolEventReceiver("Network.responseReceived")
                .DevToolsProtocolEventReceived += OnResponseReceived;

            _coreWebView.GetDevToolsProtocolEventReceiver("Network.loadingFinished")
                .DevToolsProtocolEventReceived += OnLoadingFinished;

            _coreWebView.GetDevToolsProtocolEventReceiver("Network.loadingFailed")
                .DevToolsProtocolEventReceived += OnLoadingFailed;

            _coreWebView.GetDevToolsProtocolEventReceiver("Network.requestServedFromCache")
                .DevToolsProtocolEventReceived += OnRequestServedFromCache;

            _isListening = true;
            _logger?.LogInformation("? NetworkListener STARTED successfully - Listening for network events");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "? Failed to start NetworkListener");
            throw;
        }
    }

    /// <summary>
    /// Stops listening to network events
    /// </summary>
    public async Task StopAsync()
    {
        if (!_isListening)
            return;

        try
        {
            // Unsubscribe from events
            _coreWebView.GetDevToolsProtocolEventReceiver("Network.requestWillBeSent")
                .DevToolsProtocolEventReceived -= OnRequestWillBeSent;

            _coreWebView.GetDevToolsProtocolEventReceiver("Network.responseReceived")
                .DevToolsProtocolEventReceived -= OnResponseReceived;

            _coreWebView.GetDevToolsProtocolEventReceiver("Network.loadingFinished")
                .DevToolsProtocolEventReceived -= OnLoadingFinished;

            _coreWebView.GetDevToolsProtocolEventReceiver("Network.loadingFailed")
                .DevToolsProtocolEventReceived -= OnLoadingFailed;

            _coreWebView.GetDevToolsProtocolEventReceiver("Network.requestServedFromCache")
                .DevToolsProtocolEventReceived -= OnRequestServedFromCache;

            // Disable Network domain
            await _coreWebView.CallDevToolsProtocolMethodAsync("Network.disable", "{}");

            _isListening = false;
            _logger?.LogInformation("NetworkListener stopped");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error stopping NetworkListener");
        }
    }

    #region Event Handlers

    private void OnRequestWillBeSent(object? sender, CoreWebView2DevToolsProtocolEventReceivedEventArgs e)
    {
        try
        {
            _logger?.LogDebug("?? Network request intercepted");
            
            var json = JsonDocument.Parse(e.ParameterObjectAsJson);
            var root = json.RootElement;

            var requestId = root.GetProperty("requestId").GetString() ?? Guid.NewGuid().ToString();
            var request = root.GetProperty("request");

            var builder = new NetworkRequestBuilder
            {
                RequestId = requestId,
                Url = request.GetProperty("url").GetString() ?? "",
                Method = request.GetProperty("method").GetString() ?? "GET",
                StartTime = DateTime.UtcNow
            };

            // Extract headers
            if (request.TryGetProperty("headers", out var headers))
            {
                foreach (var header in headers.EnumerateObject())
                {
                    builder.RequestHeaders[header.Name] = header.Value.GetString() ?? "";
                }
            }

            // Extract request body if available
            if (request.TryGetProperty("postData", out var postData))
            {
                builder.RequestBody = postData.GetString();
            }

            // Get timestamp
            if (root.TryGetProperty("timestamp", out var timestamp))
            {
                builder.Timestamp = DateTimeOffset.FromUnixTimeSeconds((long)timestamp.GetDouble()).UtcDateTime;
            }

            _requests[requestId] = builder;

            _logger?.LogInformation("?? NETWORK REQUEST: {Method} {Url} (ID: {RequestId})", 
                builder.Method, builder.Url.Truncate(80), requestId.Truncate(10));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing request will be sent");
        }
    }

    private void OnResponseReceived(object? sender, CoreWebView2DevToolsProtocolEventReceivedEventArgs e)
    {
        try
        {
            var json = JsonDocument.Parse(e.ParameterObjectAsJson);
            var root = json.RootElement;

            var requestId = root.GetProperty("requestId").GetString() ?? "";
            
            if (!_requests.TryGetValue(requestId, out var builder))
            {
                _logger?.LogWarning("Response received for unknown request {RequestId}", requestId);
                return;
            }

            var response = root.GetProperty("response");

            builder.StatusCode = response.GetProperty("status").GetInt32();
            builder.StatusText = response.TryGetProperty("statusText", out var statusText)
                ? statusText.GetString()
                : null;

            // Extract response headers
            if (response.TryGetProperty("headers", out var headers))
            {
                foreach (var header in headers.EnumerateObject())
                {
                    builder.ResponseHeaders[header.Name] = header.Value.GetString() ?? "";
                }
            }

            // Get MIME type
            if (response.TryGetProperty("mimeType", out var mimeType))
            {
                builder.MimeType = mimeType.GetString();
            }

            // Get timing information
            if (response.TryGetProperty("timing", out var timing))
            {
                if (timing.TryGetProperty("requestTime", out var requestTime))
                {
                    builder.RequestTime = requestTime.GetDouble();
                }
            }

            _logger?.LogDebug("Response received: {StatusCode} for {Url}",
                builder.StatusCode, builder.Url.Truncate(80));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing response received");
        }
    }

    private async void OnLoadingFinished(object? sender, CoreWebView2DevToolsProtocolEventReceivedEventArgs e)
    {
        try
        {
            var json = JsonDocument.Parse(e.ParameterObjectAsJson);
            var root = json.RootElement;

            var requestId = root.GetProperty("requestId").GetString() ?? "";
            
            if (!_requests.TryRemove(requestId, out var builder))
            {
                _logger?.LogTrace("Loading finished for unknown request {RequestId} (might have been cleared)", requestId);
                return;
            }

            builder.EndTime = DateTime.UtcNow;
            builder.DurationMs = (builder.EndTime - builder.StartTime).TotalMilliseconds;

            // Get encoded data length
            if (root.TryGetProperty("encodedDataLength", out var encodedLength))
            {
                builder.ResponseSize = encodedLength.GetInt64();
            }

            // Try to get response body (may not always be available)
            try
            {
                var bodyResult = await _coreWebView.CallDevToolsProtocolMethodAsync(
                    "Network.getResponseBody",
                    $"{{\"requestId\":\"{requestId}\"}}");

                var bodyJson = JsonDocument.Parse(bodyResult);
                if (bodyJson.RootElement.TryGetProperty("body", out var body))
                {
                    builder.ResponseBody = body.GetString();
                }
            }
            catch
            {
                // Response body not available (expected for some resources)
            }

            var networkRequest = builder.Build();
            
            _logger?.LogInformation("?? NETWORK REQUEST FINISHED: {Method} {Url} -> {Status} ({Duration}ms, {Size} bytes)",
                networkRequest.Method,
                networkRequest.Url.Truncate(60),
                networkRequest.StatusCode,
                networkRequest.DurationMs,
                networkRequest.ResponseSize);

            await _telemetryAggregator.AddNetworkRequestAsync(_currentSessionId, networkRequest);
            
            _logger?.LogDebug("? Network request added to aggregator: {RequestId}", networkRequest.RequestId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "? Error processing loading finished");
        }
    }

    private async void OnLoadingFailed(object? sender, CoreWebView2DevToolsProtocolEventReceivedEventArgs e)
    {
        try
        {
            var json = JsonDocument.Parse(e.ParameterObjectAsJson);
            var root = json.RootElement;

            var requestId = root.GetProperty("requestId").GetString() ?? "";
            
            if (!_requests.TryRemove(requestId, out var builder))
            {
                return;
            }

            builder.EndTime = DateTime.UtcNow;
            builder.DurationMs = (builder.EndTime - builder.StartTime).TotalMilliseconds;
            builder.IsFailed = true;

            // Get error details
            if (root.TryGetProperty("errorText", out var errorText))
            {
                builder.ErrorText = errorText.GetString();
            }

            if (root.TryGetProperty("canceled", out var canceled) && canceled.GetBoolean())
            {
                builder.ErrorText = "Request canceled";
            }

            var networkRequest = builder.Build();
            await _telemetryAggregator.AddNetworkRequestAsync(_currentSessionId, networkRequest);

            _logger?.LogWarning("Request failed: {Method} {Url} - {Error}",
                networkRequest.Method,
                networkRequest.Url.Truncate(80),
                networkRequest.ErrorText);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing loading failed");
        }
    }

    private void OnRequestServedFromCache(object? sender, CoreWebView2DevToolsProtocolEventReceivedEventArgs e)
    {
        try
        {
            var json = JsonDocument.Parse(e.ParameterObjectAsJson);
            var requestId = json.RootElement.GetProperty("requestId").GetString() ?? "";
            
            if (_requests.TryGetValue(requestId, out var builder))
            {
                builder.FromCache = true;
                _logger?.LogDebug("Request served from cache: {Url}", builder.Url.Truncate(80));
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing request served from cache");
        }
    }

    #endregion

    public void Dispose()
    {
        if (_isListening)
        {
            StopAsync().Wait();
        }
        _requests.Clear();
    }

}

/// <summary>
/// Builder for constructing NetworkRequest objects
/// </summary>
internal class NetworkRequestBuilder
{
    public string RequestId { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Method { get; set; } = "GET";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public double DurationMs { get; set; }
    public int StatusCode { get; set; }
    public string? StatusText { get; set; }
    public Dictionary<string, string> RequestHeaders { get; set; } = new();
    public Dictionary<string, string> ResponseHeaders { get; set; } = new();
    public string? RequestBody { get; set; }
    public string? ResponseBody { get; set; }
    public long ResponseSize { get; set; }
    public string? MimeType { get; set; }
    public bool IsFailed { get; set; }
    public string? ErrorText { get; set; }
    public bool FromCache { get; set; }
    public double RequestTime { get; set; }

    public NetworkRequest Build()
    {
        return new NetworkRequest
        {
            Id = Guid.NewGuid(),
            RequestId = RequestId,
            Timestamp = Timestamp,
            Url = Url,
            Method = Method,
            StatusCode = StatusCode,
            StatusText = StatusText,
            RequestHeaders = RequestHeaders,
            ResponseHeaders = ResponseHeaders,
            RequestBody = RequestBody,
            ResponseBody = ResponseBody,
            DurationMs = DurationMs,
            ResponseSize = ResponseSize,
            MimeType = MimeType,
            IsFailed = IsFailed,
            ErrorText = ErrorText
        };
    }
}
