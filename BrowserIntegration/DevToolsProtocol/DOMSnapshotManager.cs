using Microsoft.Web.WebView2.Core;
using Microsoft.Extensions.Logging;
using AIDebugPro.Core.Interfaces;
using AIDebugPro.Core.Models;
using System.Text.Json;

namespace AIDebugPro.BrowserIntegration.DevToolsProtocol;

/// <summary>
/// Captures DOM structure snapshots
/// </summary>
public class DOMSnapshotManager : IDOMSnapshotManager, IDisposable
{
    private readonly CoreWebView2 _coreWebView;
    private readonly ITelemetryAggregator _telemetryAggregator;
    private readonly ILogger? _logger;
    private Guid _currentSessionId;
    private bool _isActive;

    public DOMSnapshotManager(
        CoreWebView2 coreWebView,
        ITelemetryAggregator telemetryAggregator,
        ILogger? logger = null)
    {
        _coreWebView = coreWebView ?? throw new ArgumentNullException(nameof(coreWebView));
        _telemetryAggregator = telemetryAggregator ?? throw new ArgumentNullException(nameof(telemetryAggregator));
        _logger = logger;
    }

    /// <summary>
    /// Starts DOM snapshot manager
    /// </summary>
    public async Task StartAsync(Guid sessionId)
    {
        if (_isActive)
        {
            _logger?.LogWarning("DOMSnapshotManager already started");
            return;
        }

        _currentSessionId = sessionId;

        try
        {
            // Enable DOM domain
            await _coreWebView.CallDevToolsProtocolMethodAsync("DOM.enable", "{}");

            _isActive = true;
            _logger?.LogInformation("DOMSnapshotManager started for session {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to start DOMSnapshotManager");
            throw;
        }
    }

    /// <summary>
    /// Stops DOM snapshot manager
    /// </summary>
    public async Task StopAsync()
    {
        if (!_isActive)
            return;

        try
        {
            // Disable DOM domain
            await _coreWebView.CallDevToolsProtocolMethodAsync("DOM.disable", "{}");

            _isActive = false;
            _logger?.LogInformation("DOMSnapshotManager stopped");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error stopping DOMSnapshotManager");
        }
    }

    /// <summary>
    /// Captures current DOM snapshot
    /// </summary>
    public async Task CaptureSnapshotAsync(Guid sessionId)
    {
        if (!_isActive)
        {
            _logger?.LogWarning("DOMSnapshotManager not active");
            return;
        }

        try
        {
            _logger?.LogDebug("Capturing DOM snapshot...");

            var snapshot = new DOMSnapshot
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                Url = _coreWebView.Source
            };

            // Get document HTML
            var htmlScript = "document.documentElement.outerHTML";
            var htmlResult = await _coreWebView.ExecuteScriptAsync(htmlScript);
            snapshot.HtmlContent = JsonSerializer.Deserialize<string>(htmlResult) ?? "";

            // Get document title
            var titleScript = "document.title";
            var titleResult = await _coreWebView.ExecuteScriptAsync(titleScript);
            snapshot.DocumentTitle = JsonSerializer.Deserialize<string>(titleResult) ?? "";

            // Get DOM tree using CDP
            var domResult = await _coreWebView.CallDevToolsProtocolMethodAsync(
                "DOM.getDocument",
                "{\"depth\": -1, \"pierce\": true}");

            var domJson = JsonDocument.Parse(domResult);
            var root = domJson.RootElement.GetProperty("root");

            // Parse DOM tree
            snapshot.Nodes = ParseDOMNode(root);

            // Get metadata
            snapshot.Metadata["nodeCount"] = snapshot.Nodes.Count.ToString();
            snapshot.Metadata["captureTime"] = DateTime.UtcNow.ToString("O");
            snapshot.Metadata["url"] = snapshot.Url;

            await _telemetryAggregator.SetDomSnapshotAsync(sessionId, snapshot);

            _logger?.LogInformation(
                "Captured DOM snapshot: {NodeCount} nodes, {HtmlSize} bytes",
                snapshot.Nodes.Count,
                snapshot.HtmlContent.Length);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error capturing DOM snapshot");
        }
    }

    #region Helper Methods

    /// <summary>
    /// Parses a DOM node from CDP response
    /// </summary>
    private List<DOMNode> ParseDOMNode(JsonElement nodeElement, List<DOMNode>? result = null)
    {
        result ??= new List<DOMNode>();

        var node = new DOMNode
        {
            NodeId = nodeElement.GetProperty("nodeId").GetInt32(),
            NodeType = nodeElement.GetProperty("nodeType").GetInt32().ToString(),
            NodeName = nodeElement.GetProperty("nodeName").GetString() ?? ""
        };

        // Get node value if available
        if (nodeElement.TryGetProperty("nodeValue", out var nodeValue))
        {
            node.NodeValue = nodeValue.GetString();
        }

        // Get attributes if available
        if (nodeElement.TryGetProperty("attributes", out var attributes))
        {
            var attrs = attributes.EnumerateArray().ToList();
            for (int i = 0; i < attrs.Count - 1; i += 2)
            {
                var name = attrs[i].GetString() ?? "";
                var value = attrs[i + 1].GetString() ?? "";
                node.Attributes[name] = value;
            }
        }

        result.Add(node);

        // Process children
        if (nodeElement.TryGetProperty("children", out var children))
        {
            foreach (var child in children.EnumerateArray())
            {
                var childNodes = ParseDOMNode(child, new List<DOMNode>());
                node.Children.AddRange(childNodes);
                result.AddRange(childNodes);
            }
        }

        return result;
    }

    /// <summary>
    /// Captures DOM screenshot (specific element or full page)
    /// </summary>
    public async Task<byte[]> CaptureScreenshotAsync(int? nodeId = null)
    {
        try
        {
            string parameters = nodeId.HasValue
                ? $"{{\"nodeId\": {nodeId.Value}}}"
                : "{}";

            var result = await _coreWebView.CallDevToolsProtocolMethodAsync(
                "Page.captureScreenshot",
                parameters);

            var json = JsonDocument.Parse(result);
            var base64 = json.RootElement.GetProperty("data").GetString();
            
            return Convert.FromBase64String(base64 ?? string.Empty);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error capturing screenshot");
            throw;
        }
    }

    /// <summary>
    /// Gets specific DOM node details
    /// </summary>
    public async Task<string> GetNodeDetailsAsync(int nodeId)
    {
        try
        {
            var result = await _coreWebView.CallDevToolsProtocolMethodAsync(
                "DOM.describeNode",
                $"{{\"nodeId\": {nodeId}}}");

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting node details");
            throw;
        }
    }

    /// <summary>
    /// Highlights a DOM node in the browser
    /// </summary>
    public async Task HighlightNodeAsync(int nodeId)
    {
        try
        {
            var highlightConfig = @"{
                ""contentColor"": {""r"": 255, ""g"": 0, ""b"": 0, ""a"": 0.3},
                ""showInfo"": true
            }";

            await _coreWebView.CallDevToolsProtocolMethodAsync(
                "DOM.highlightNode",
                $"{{\"nodeId\": {nodeId}, \"highlightConfig\": {highlightConfig}}}");

            _logger?.LogDebug("Highlighted node {NodeId}", nodeId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error highlighting node");
        }
    }

    /// <summary>
    /// Hides DOM highlight
    /// </summary>
    public async Task HideHighlightAsync()
    {
        try
        {
            await _coreWebView.CallDevToolsProtocolMethodAsync("DOM.hideHighlight", "{}");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error hiding highlight");
        }
    }

    #endregion

    public void Dispose()
    {
        if (_isActive)
        {
            StopAsync().Wait();
        }
    }
}
