using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIDebugPro.Core.Models;

/// <summary>
/// Represents a console message captured from the browser
/// </summary>
public class ConsoleMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public ConsoleMessageLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Source { get; set; }
    public int LineNumber { get; set; }
    public int ColumnNumber { get; set; }
    public string? StackTrace { get; set; }
    public string? Url { get; set; }
}

/// <summary>
/// Represents a network request/response captured via CDP
/// </summary>
public class NetworkRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string RequestId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Url { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string? StatusText { get; set; }
    public Dictionary<string, string> RequestHeaders { get; set; } = new();
    public Dictionary<string, string> ResponseHeaders { get; set; } = new();
    public string? RequestBody { get; set; }
    public string? ResponseBody { get; set; }
    public double DurationMs { get; set; }
    public long? ResponseSize { get; set; }
    public string? MimeType { get; set; }
    public bool IsFailed { get; set; }
    public string? ErrorText { get; set; }
}

/// <summary>
/// Represents performance metrics captured from the browser
/// </summary>
public class PerformanceMetrics
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public double CpuUsage { get; set; }
    public long MemoryUsageBytes { get; set; }
    public double DomContentLoadedMs { get; set; }
    public double LoadEventMs { get; set; }
    public double FirstPaintMs { get; set; }
    public double FirstContentfulPaintMs { get; set; }
    public double LargestContentfulPaintMs { get; set; }
    public int DomNodeCount { get; set; }
    public int JavaScriptHeapSizeBytes { get; set; }
    public Dictionary<string, double> CustomMetrics { get; set; } = new();
}

/// <summary>
/// Represents a DOM snapshot captured from the browser
/// </summary>
public class DOMSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Url { get; set; } = string.Empty;
    public string DocumentTitle { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
    public List<DOMNode> Nodes { get; set; } = new();
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Represents a node in the DOM tree
/// </summary>
public class DOMNode
{
    public int NodeId { get; set; }
    public string NodeType { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;
    public string? NodeValue { get; set; }
    public Dictionary<string, string> Attributes { get; set; } = new();
    public List<DOMNode> Children { get; set; } = new();
}

/// <summary>
/// Aggregated telemetry data for a session
/// </summary>
public class TelemetrySnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;
    public List<ConsoleMessage> ConsoleMessages { get; set; } = new();
    public List<NetworkRequest> NetworkRequests { get; set; } = new();
    public List<PerformanceMetrics> PerformanceMetrics { get; set; } = new();
    public DOMSnapshot? DomSnapshot { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}
