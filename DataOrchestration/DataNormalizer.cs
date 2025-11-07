using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using AIDebugPro.Core.Models;

namespace AIDebugPro.DataOrchestration;

/// <summary>
/// Normalizes and standardizes telemetry data for consistent processing
/// </summary>
public class DataNormalizer
{
    private readonly ILogger<DataNormalizer>? _logger;
    private readonly DataNormalizationOptions _options;

    public DataNormalizer(DataNormalizationOptions? options = null, ILogger<DataNormalizer>? logger = null)
    {
        _options = options ?? new DataNormalizationOptions();
        _logger = logger;
    }

    #region Console Message Normalization

    /// <summary>
    /// Normalizes a console message for consistent processing
    /// </summary>
    public ConsoleMessage NormalizeConsoleMessage(ConsoleMessage message)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var normalized = new ConsoleMessage
        {
            Id = message.Id,
            Timestamp = NormalizeTimestamp(message.Timestamp),
            Level = message.Level,
            Message = NormalizeText(message.Message),
            Source = NormalizeUrl(message.Source),
            LineNumber = message.LineNumber,
            ColumnNumber = message.ColumnNumber,
            StackTrace = NormalizeStackTrace(message.StackTrace),
            Url = NormalizeUrl(message.Url)
        };

        return normalized;
    }

    /// <summary>
    /// Normalizes multiple console messages
    /// </summary>
    public List<ConsoleMessage> NormalizeConsoleMessages(IEnumerable<ConsoleMessage> messages)
    {
        return messages.Select(NormalizeConsoleMessage).ToList();
    }

    #endregion

    #region Network Request Normalization

    /// <summary>
    /// Normalizes a network request for consistent processing
    /// </summary>
    public NetworkRequest NormalizeNetworkRequest(NetworkRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var normalized = new NetworkRequest
        {
            Id = request.Id,
            RequestId = request.RequestId,
            Timestamp = NormalizeTimestamp(request.Timestamp),
            Url = NormalizeUrl(request.Url) ?? string.Empty,
            Method = NormalizeHttpMethod(request.Method),
            StatusCode = request.StatusCode,
            StatusText = request.StatusText,
            RequestHeaders = NormalizeHeaders(request.RequestHeaders),
            ResponseHeaders = NormalizeHeaders(request.ResponseHeaders),
            RequestBody = _options.TruncateRequestBodies 
                ? TruncateText(request.RequestBody, _options.MaxBodyLength) 
                : request.RequestBody,
            ResponseBody = _options.TruncateResponseBodies 
                ? TruncateText(request.ResponseBody, _options.MaxBodyLength) 
                : request.ResponseBody,
            DurationMs = Math.Max(0, request.DurationMs),
            ResponseSize = request.ResponseSize,
            MimeType = NormalizeMimeType(request.MimeType),
            IsFailed = request.IsFailed,
            ErrorText = request.ErrorText
        };

        return normalized;
    }

    /// <summary>
    /// Normalizes multiple network requests
    /// </summary>
    public List<NetworkRequest> NormalizeNetworkRequests(IEnumerable<NetworkRequest> requests)
    {
        return requests.Select(NormalizeNetworkRequest).ToList();
    }

    #endregion

    #region Performance Metrics Normalization

    /// <summary>
    /// Normalizes performance metrics for consistent processing
    /// </summary>
    public PerformanceMetrics NormalizePerformanceMetrics(PerformanceMetrics metrics)
    {
        if (metrics == null)
            throw new ArgumentNullException(nameof(metrics));

        var normalized = new PerformanceMetrics
        {
            Id = metrics.Id,
            Timestamp = NormalizeTimestamp(metrics.Timestamp),
            CpuUsage = ClampPercentage(metrics.CpuUsage),
            MemoryUsageBytes = Math.Max(0, metrics.MemoryUsageBytes),
            DomContentLoadedMs = Math.Max(0, metrics.DomContentLoadedMs),
            LoadEventMs = Math.Max(0, metrics.LoadEventMs),
            FirstPaintMs = Math.Max(0, metrics.FirstPaintMs),
            FirstContentfulPaintMs = Math.Max(0, metrics.FirstContentfulPaintMs),
            LargestContentfulPaintMs = Math.Max(0, metrics.LargestContentfulPaintMs),
            DomNodeCount = Math.Max(0, metrics.DomNodeCount),
            JavaScriptHeapSizeBytes = Math.Max(0, metrics.JavaScriptHeapSizeBytes),
            CustomMetrics = NormalizeCustomMetrics(metrics.CustomMetrics)
        };

        return normalized;
    }

    /// <summary>
    /// Normalizes multiple performance metrics
    /// </summary>
    public List<PerformanceMetrics> NormalizePerformanceMetrics(IEnumerable<PerformanceMetrics> metrics)
    {
        return metrics.Select(NormalizePerformanceMetrics).ToList();
    }

    #endregion

    #region DOM Snapshot Normalization

    /// <summary>
    /// Normalizes a DOM snapshot for consistent processing
    /// </summary>
    public DOMSnapshot NormalizeDomSnapshot(DOMSnapshot snapshot)
    {
        if (snapshot == null)
            throw new ArgumentNullException(nameof(snapshot));

        var normalized = new DOMSnapshot
        {
            Id = snapshot.Id,
            Timestamp = NormalizeTimestamp(snapshot.Timestamp),
            Url = NormalizeUrl(snapshot.Url) ?? string.Empty,
            DocumentTitle = NormalizeText(snapshot.DocumentTitle),
            HtmlContent = _options.TruncateHtmlContent 
                ? TruncateText(snapshot.HtmlContent, _options.MaxHtmlLength) 
                : snapshot.HtmlContent,
            Nodes = NormalizeDomNodes(snapshot.Nodes),
            Metadata = NormalizeDictionary(snapshot.Metadata)
        };

        return normalized;
    }

    /// <summary>
    /// Normalizes DOM nodes recursively
    /// </summary>
    private List<DOMNode> NormalizeDomNodes(List<DOMNode> nodes)
    {
        return nodes.Select(node => new DOMNode
        {
            NodeId = node.NodeId,
            NodeType = NormalizeText(node.NodeType),
            NodeName = NormalizeText(node.NodeName),
            NodeValue = NormalizeText(node.NodeValue),
            Attributes = NormalizeDictionary(node.Attributes),
            Children = NormalizeDomNodes(node.Children)
        }).ToList();
    }

    #endregion

    #region Telemetry Snapshot Normalization

    /// <summary>
    /// Normalizes a complete telemetry snapshot
    /// </summary>
    public TelemetrySnapshot NormalizeTelemetrySnapshot(TelemetrySnapshot snapshot)
    {
        if (snapshot == null)
            throw new ArgumentNullException(nameof(snapshot));

        _logger?.LogDebug("Normalizing telemetry snapshot {SnapshotId}", snapshot.Id);

        var normalized = new TelemetrySnapshot
        {
            Id = snapshot.Id,
            SessionId = snapshot.SessionId,
            CapturedAt = NormalizeTimestamp(snapshot.CapturedAt),
            ConsoleMessages = NormalizeConsoleMessages(snapshot.ConsoleMessages),
            NetworkRequests = NormalizeNetworkRequests(snapshot.NetworkRequests),
            PerformanceMetrics = NormalizePerformanceMetrics(snapshot.PerformanceMetrics),
            DomSnapshot = snapshot.DomSnapshot != null ? NormalizeDomSnapshot(snapshot.DomSnapshot) : null,
            Metadata = NormalizeObjectDictionary(snapshot.Metadata)
        };

        _logger?.LogInformation(
            "Normalized snapshot {SnapshotId}: {Console} console messages, {Network} network requests, {Perf} metrics",
            normalized.Id,
            normalized.ConsoleMessages.Count,
            normalized.NetworkRequests.Count,
            normalized.PerformanceMetrics.Count);

        return normalized;
    }

    #endregion

    #region Data Deduplication

    /// <summary>
    /// Removes duplicate console messages based on content similarity
    /// </summary>
    public List<ConsoleMessage> DeduplicateConsoleMessages(List<ConsoleMessage> messages)
    {
        var seen = new HashSet<string>();
        var deduplicated = new List<ConsoleMessage>();

        foreach (var message in messages.OrderBy(m => m.Timestamp))
        {
            var key = GenerateMessageKey(message);
            if (seen.Add(key))
            {
                deduplicated.Add(message);
            }
        }

        if (deduplicated.Count < messages.Count)
        {
            _logger?.LogDebug(
                "Deduplicated console messages: {Original} -> {Deduplicated}",
                messages.Count,
                deduplicated.Count);
        }

        return deduplicated;
    }

    /// <summary>
    /// Removes duplicate network requests based on URL and timestamp proximity
    /// </summary>
    public List<NetworkRequest> DeduplicateNetworkRequests(List<NetworkRequest> requests)
    {
        var seen = new HashSet<string>();
        var deduplicated = new List<NetworkRequest>();

        foreach (var request in requests.OrderBy(r => r.Timestamp))
        {
            var key = GenerateRequestKey(request);
            if (seen.Add(key))
            {
                deduplicated.Add(request);
            }
        }

        if (deduplicated.Count < requests.Count)
        {
            _logger?.LogDebug(
                "Deduplicated network requests: {Original} -> {Deduplicated}",
                requests.Count,
                deduplicated.Count);
        }

        return deduplicated;
    }

    #endregion

    #region Data Validation

    /// <summary>
    /// Validates and filters out invalid console messages
    /// </summary>
    public List<ConsoleMessage> ValidateConsoleMessages(List<ConsoleMessage> messages)
    {
        return messages.Where(IsValidConsoleMessage).ToList();
    }

    /// <summary>
    /// Validates and filters out invalid network requests
    /// </summary>
    public List<NetworkRequest> ValidateNetworkRequests(List<NetworkRequest> requests)
    {
        return requests.Where(IsValidNetworkRequest).ToList();
    }

    /// <summary>
    /// Validates a console message
    /// </summary>
    private bool IsValidConsoleMessage(ConsoleMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.Message))
            return false;

        if (message.Timestamp == default)
            return false;

        return true;
    }

    /// <summary>
    /// Validates a network request
    /// </summary>
    private bool IsValidNetworkRequest(NetworkRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
            return false;

        if (request.Timestamp == default)
            return false;

        if (request.DurationMs < 0)
            return false;

        return true;
    }

    #endregion

    #region Data Transformation

    /// <summary>
    /// Converts telemetry snapshot to a normalized JSON string
    /// </summary>
    public string ToNormalizedJson(TelemetrySnapshot snapshot)
    {
        var normalized = NormalizeTelemetrySnapshot(snapshot);
        
        var options = new JsonSerializerOptions
        {
            WriteIndented = _options.PrettyPrintJson,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        return JsonSerializer.Serialize(normalized, options);
    }

    /// <summary>
    /// Converts normalized data to a summary dictionary
    /// </summary>
    public Dictionary<string, object> ToSummaryDictionary(TelemetrySnapshot snapshot)
    {
        var normalized = NormalizeTelemetrySnapshot(snapshot);

        return new Dictionary<string, object>
        {
            ["sessionId"] = normalized.SessionId,
            ["capturedAt"] = normalized.CapturedAt,
            ["totalConsoleMessages"] = normalized.ConsoleMessages.Count,
            ["consoleErrors"] = normalized.ConsoleMessages.Count(m => m.Level == ConsoleMessageLevel.Error),
            ["consoleWarnings"] = normalized.ConsoleMessages.Count(m => m.Level == ConsoleMessageLevel.Warning),
            ["totalNetworkRequests"] = normalized.NetworkRequests.Count,
            ["failedNetworkRequests"] = normalized.NetworkRequests.Count(r => r.IsFailed),
            ["averageResponseTime"] = normalized.NetworkRequests.Any() 
                ? normalized.NetworkRequests.Average(r => r.DurationMs) 
                : 0,
            ["performanceMetricsCount"] = normalized.PerformanceMetrics.Count,
            ["hasDomSnapshot"] = normalized.DomSnapshot != null
        };
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Normalizes a timestamp to UTC
    /// </summary>
    private DateTime NormalizeTimestamp(DateTime timestamp)
    {
        if (timestamp == default)
            return DateTime.UtcNow;

        return timestamp.Kind == DateTimeKind.Utc 
            ? timestamp 
            : timestamp.ToUniversalTime();
    }

    /// <summary>
    /// Normalizes text by trimming and removing excessive whitespace
    /// </summary>
    private string NormalizeText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        text = text.Trim();

        if (_options.CollapseWhitespace)
        {
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
        }

        return text;
    }

    /// <summary>
    /// Normalizes a URL
    /// </summary>
    private string? NormalizeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        url = url.Trim();

        if (_options.NormalizeUrls)
        {
            try
            {
                var uri = new Uri(url);
                url = uri.GetLeftPart(UriPartial.Path);
                
                if (_options.RemoveQueryStrings && uri.Query.Length > 0)
                {
                    url = url.Replace(uri.Query, "");
                }
            }
            catch
            {
                // If URL parsing fails, return original
            }
        }

        return url;
    }

    /// <summary>
    /// Normalizes a stack trace
    /// </summary>
    private string? NormalizeStackTrace(string? stackTrace)
    {
        if (string.IsNullOrWhiteSpace(stackTrace))
            return null;

        stackTrace = stackTrace.Trim();

        if (_options.TruncateStackTraces && stackTrace.Length > _options.MaxStackTraceLength)
        {
            stackTrace = stackTrace.Substring(0, _options.MaxStackTraceLength) + "...";
        }

        return stackTrace;
    }

    /// <summary>
    /// Normalizes HTTP method to uppercase
    /// </summary>
    private string NormalizeHttpMethod(string method)
    {
        return method?.ToUpperInvariant() ?? "GET";
    }

    /// <summary>
    /// Normalizes HTTP headers dictionary
    /// </summary>
    private Dictionary<string, string> NormalizeHeaders(Dictionary<string, string> headers)
    {
        if (!_options.NormalizeHeaders)
            return headers;

        return headers.ToDictionary(
            kvp => kvp.Key.ToLowerInvariant(),
            kvp => kvp.Value,
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Normalizes MIME type
    /// </summary>
    private string? NormalizeMimeType(string? mimeType)
    {
        if (string.IsNullOrWhiteSpace(mimeType))
            return null;

        return mimeType.ToLowerInvariant().Split(';')[0].Trim();
    }

    /// <summary>
    /// Normalizes custom metrics dictionary
    /// </summary>
    private Dictionary<string, double> NormalizeCustomMetrics(Dictionary<string, double> metrics)
    {
        return metrics.ToDictionary(
            kvp => NormalizeText(kvp.Key),
            kvp => Math.Max(0, kvp.Value));
    }

    /// <summary>
    /// Normalizes a string dictionary
    /// </summary>
    private Dictionary<string, string> NormalizeDictionary(Dictionary<string, string> dict)
    {
        return dict.ToDictionary(
            kvp => NormalizeText(kvp.Key),
            kvp => NormalizeText(kvp.Value));
    }

    /// <summary>
    /// Normalizes an object dictionary
    /// </summary>
    private Dictionary<string, object> NormalizeObjectDictionary(Dictionary<string, object> dict)
    {
        return dict.ToDictionary(
            kvp => NormalizeText(kvp.Key),
            kvp => kvp.Value);
    }

    /// <summary>
    /// Truncates text to specified length
    /// </summary>
    private string? TruncateText(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        if (text.Length <= maxLength)
            return text;

        return text.Substring(0, maxLength) + "...";
    }

    /// <summary>
    /// Clamps a percentage value between 0 and 100
    /// </summary>
    private double ClampPercentage(double value)
    {
        return Math.Max(0, Math.Min(100, value));
    }

    /// <summary>
    /// Generates a unique key for a console message for deduplication
    /// </summary>
    private string GenerateMessageKey(ConsoleMessage message)
    {
        var key = new StringBuilder();
        key.Append(message.Level);
        key.Append('|');
        key.Append(message.Message.GetHashCode());
        key.Append('|');
        key.Append(message.Source ?? "");
        key.Append('|');
        key.Append(message.LineNumber);
        
        return key.ToString();
    }

    /// <summary>
    /// Generates a unique key for a network request for deduplication
    /// </summary>
    private string GenerateRequestKey(NetworkRequest request)
    {
        var key = new StringBuilder();
        key.Append(request.Method);
        key.Append('|');
        key.Append(request.Url.GetHashCode());
        key.Append('|');
        key.Append(request.Timestamp.Ticks / TimeSpan.TicksPerSecond); // Group by second
        
        return key.ToString();
    }

    #endregion
}

#region Configuration Classes

/// <summary>
/// Options for data normalization
/// </summary>
public class DataNormalizationOptions
{
    // Text normalization
    public bool CollapseWhitespace { get; set; } = true;
    public bool TruncateStackTraces { get; set; } = true;
    public int MaxStackTraceLength { get; set; } = 1000;

    // URL normalization
    public bool NormalizeUrls { get; set; } = true;
    public bool RemoveQueryStrings { get; set; } = false;

    // Header normalization
    public bool NormalizeHeaders { get; set; } = true;

    // Content truncation
    public bool TruncateRequestBodies { get; set; } = true;
    public bool TruncateResponseBodies { get; set; } = true;
    public int MaxBodyLength { get; set; } = 10000;

    // HTML normalization
    public bool TruncateHtmlContent { get; set; } = true;
    public int MaxHtmlLength { get; set; } = 50000;

    // JSON options
    public bool PrettyPrintJson { get; set; } = false;
}

#endregion
