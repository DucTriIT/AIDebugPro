using Microsoft.Web.WebView2.Core;
using Microsoft.Extensions.Logging;
using AIDebugPro.Core.Interfaces;
using AIDebugPro.Core.Models;
using System.Text.Json;

namespace AIDebugPro.BrowserIntegration.DevToolsProtocol;

/// <summary>
/// Listens to console API calls and exceptions from the browser
/// </summary>
public class ConsoleListener : IDisposable,IConsoleListener
{
    private readonly CoreWebView2 _coreWebView;
    private readonly ITelemetryAggregator _telemetryAggregator;
    private readonly ILogger? _logger;
    private Guid _currentSessionId;
    private bool _isListening;

    public ConsoleListener(
        CoreWebView2 coreWebView,
        ITelemetryAggregator telemetryAggregator,
        ILogger? logger = null)
    {
        _coreWebView = coreWebView ?? throw new ArgumentNullException(nameof(coreWebView));
        _telemetryAggregator = telemetryAggregator ?? throw new ArgumentNullException(nameof(telemetryAggregator));
        _logger = logger;
    }

    /// <summary>
    /// Starts listening to console events
    /// </summary>
    public async Task StartAsync(Guid sessionId)
    {
        if (_isListening)
        {
            _logger?.LogWarning("ConsoleListener already started");
            return;
        }

        _currentSessionId = sessionId;

        try
        {
            // Enable Runtime domain for console events
            await _coreWebView.CallDevToolsProtocolMethodAsync("Runtime.enable", "{}");

            // Enable Log domain for console logs
            await _coreWebView.CallDevToolsProtocolMethodAsync("Log.enable", "{}");

            // Subscribe to events
            _coreWebView.GetDevToolsProtocolEventReceiver("Runtime.consoleAPICalled")
                .DevToolsProtocolEventReceived += OnConsoleAPICalled;

            _coreWebView.GetDevToolsProtocolEventReceiver("Runtime.exceptionThrown")
                .DevToolsProtocolEventReceived += OnExceptionThrown;

            _coreWebView.GetDevToolsProtocolEventReceiver("Log.entryAdded")
                .DevToolsProtocolEventReceived += OnLogEntryAdded;

            _isListening = true;
            _logger?.LogInformation("ConsoleListener started for session {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to start ConsoleListener");
            throw;
        }
    }

    /// <summary>
    /// Stops listening to console events
    /// </summary>
    public async Task StopAsync()
    {
        if (!_isListening)
            return;

        try
        {
            // Unsubscribe from events
            _coreWebView.GetDevToolsProtocolEventReceiver("Runtime.consoleAPICalled")
                .DevToolsProtocolEventReceived -= OnConsoleAPICalled;

            _coreWebView.GetDevToolsProtocolEventReceiver("Runtime.exceptionThrown")
                .DevToolsProtocolEventReceived -= OnExceptionThrown;

            _coreWebView.GetDevToolsProtocolEventReceiver("Log.entryAdded")
                .DevToolsProtocolEventReceived -= OnLogEntryAdded;

            // Disable domains
            await _coreWebView.CallDevToolsProtocolMethodAsync("Runtime.disable", "{}");
            await _coreWebView.CallDevToolsProtocolMethodAsync("Log.disable", "{}");

            _isListening = false;
            _logger?.LogInformation("ConsoleListener stopped");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error stopping ConsoleListener");
        }
    }

    #region Event Handlers

    private async void OnConsoleAPICalled(object? sender, CoreWebView2DevToolsProtocolEventReceivedEventArgs e)
    {
        try
        {
            var json = JsonDocument.Parse(e.ParameterObjectAsJson);
            var root = json.RootElement;

            var type = root.GetProperty("type").GetString() ?? "log";
            var timestamp = root.GetProperty("timestamp").GetDouble();
            
            // Get console message arguments
            var args = root.GetProperty("args");
            var messages = new List<string>();
            
            foreach (var arg in args.EnumerateArray())
            {
                if (arg.TryGetProperty("value", out var value))
                {
                    messages.Add(value.ToString());
                }
                else if (arg.TryGetProperty("description", out var desc))
                {
                    messages.Add(desc.GetString() ?? "");
                }
            }

            var message = string.Join(" ", messages);

            // Get stack trace if available
            var stackTrace = root.TryGetProperty("stackTrace", out var stack)
                ? JsonSerializer.Serialize(stack)
                : null;

            var consoleMessage = new ConsoleMessage
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds((long)timestamp).UtcDateTime,
                Level = MapConsoleLevel(type),
                Message = message,
                StackTrace = stackTrace,
                Url = _coreWebView.Source
            };

            await _telemetryAggregator.AddConsoleMessageAsync(_currentSessionId, consoleMessage);

            _logger?.LogDebug(
                "Console {Level}: {Message}",
                consoleMessage.Level,
                message.Truncate(100));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing console API call");
        }
    }

    private async void OnExceptionThrown(object? sender, CoreWebView2DevToolsProtocolEventReceivedEventArgs e)
    {
        try
        {
            var json = JsonDocument.Parse(e.ParameterObjectAsJson);
            var exceptionDetails = json.RootElement.GetProperty("exceptionDetails");

            var text = exceptionDetails.GetProperty("text").GetString() ?? "Unknown error";
            var url = exceptionDetails.TryGetProperty("url", out var urlProp)
                ? urlProp.GetString()
                : _coreWebView.Source;
            
            var lineNumber = exceptionDetails.TryGetProperty("lineNumber", out var line)
                ? line.GetInt32()
                : 0;
            
            var columnNumber = exceptionDetails.TryGetProperty("columnNumber", out var column)
                ? column.GetInt32()
                : 0;

            // Get exception details
            var exception = exceptionDetails.TryGetProperty("exception", out var exc)
                ? exc.GetProperty("description").GetString() ?? text
                : text;

            // Get stack trace
            var stackTrace = exceptionDetails.TryGetProperty("stackTrace", out var stack)
                ? FormatStackTrace(stack)
                : null;

            var consoleMessage = new ConsoleMessage
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                Level = ConsoleMessageLevel.Error,
                Message = exception,
                Source = url,
                LineNumber = lineNumber,
                ColumnNumber = columnNumber,
                StackTrace = stackTrace,
                Url = _coreWebView.Source
            };

            await _telemetryAggregator.AddConsoleMessageAsync(_currentSessionId, consoleMessage);

            _logger?.LogWarning(
                "Exception: {Message} at {Url}:{Line}:{Column}",
                exception.Truncate(100),
                url,
                lineNumber,
                columnNumber);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing exception thrown");
        }
    }

    private async void OnLogEntryAdded(object? sender, CoreWebView2DevToolsProtocolEventReceivedEventArgs e)
    {
        try
        {
            var json = JsonDocument.Parse(e.ParameterObjectAsJson);
            var entry = json.RootElement.GetProperty("entry");

            var source = entry.GetProperty("source").GetString() ?? "unknown";
            var level = entry.GetProperty("level").GetString() ?? "info";
            var text = entry.GetProperty("text").GetString() ?? "";
            var timestamp = entry.TryGetProperty("timestamp", out var ts)
                ? ts.GetDouble()
                : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var url = entry.TryGetProperty("url", out var urlProp)
                ? urlProp.GetString()
                : _coreWebView.Source;

            var lineNumber = entry.TryGetProperty("lineNumber", out var line)
                ? line.GetInt32()
                : 0;

            var consoleMessage = new ConsoleMessage
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds((long)timestamp).UtcDateTime,
                Level = MapLogLevel(level),
                Message = text,
                Source = url,
                LineNumber = lineNumber,
                Url = _coreWebView.Source
            };

            await _telemetryAggregator.AddConsoleMessageAsync(_currentSessionId, consoleMessage);

            _logger?.LogDebug("Log {Level}: {Message}", level, text.Truncate(100));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing log entry");
        }
    }

    #endregion

    #region Helper Methods

    private ConsoleMessageLevel MapConsoleLevel(string type)
    {
        return type.ToLowerInvariant() switch
        {
            "error" => ConsoleMessageLevel.Error,
            "warning" => ConsoleMessageLevel.Warning,
            "info" => ConsoleMessageLevel.Info,
            "debug" => ConsoleMessageLevel.Debug,
            "trace" => ConsoleMessageLevel.Verbose,
            _ => ConsoleMessageLevel.Info
        };
    }

    private ConsoleMessageLevel MapLogLevel(string level)
    {
        return level.ToLowerInvariant() switch
        {
            "error" => ConsoleMessageLevel.Error,
            "warning" => ConsoleMessageLevel.Warning,
            "info" => ConsoleMessageLevel.Info,
            "verbose" => ConsoleMessageLevel.Verbose,
            _ => ConsoleMessageLevel.Info
        };
    }

    private string FormatStackTrace(JsonElement stackTrace)
    {
        var sb = new System.Text.StringBuilder();
        
        if (stackTrace.TryGetProperty("callFrames", out var frames))
        {
            foreach (var frame in frames.EnumerateArray())
            {
                var functionName = frame.GetProperty("functionName").GetString() ?? "(anonymous)";
                var url = frame.TryGetProperty("url", out var urlProp) ? urlProp.GetString() : "";
                var line = frame.TryGetProperty("lineNumber", out var lineProp) ? lineProp.GetInt32() : 0;
                var column = frame.TryGetProperty("columnNumber", out var colProp) ? colProp.GetInt32() : 0;

                sb.AppendLine($"  at {functionName} ({url}:{line}:{column})");
            }
        }

        return sb.ToString();
    }

    #endregion

    public void Dispose()
    {
        if (_isListening)
        {
            StopAsync().Wait();
        }
    }
}

// Extension method for string truncation (if not already available)
public static class StringExtensions
{
    public static string Truncate(this string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;
        return value.Substring(0, maxLength) + "...";
    }
}
