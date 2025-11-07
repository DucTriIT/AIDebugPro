using AIDebugPro.Core.Models;

namespace AIDebugPro.Core.Interfaces;

/// <summary>
/// Builds AI analysis context from telemetry data
/// </summary>
public interface IContextBuilder
{
    /// <summary>
    /// Builds AI prompt context from a telemetry snapshot
    /// </summary>
    Task<string> BuildPromptContextAsync(TelemetrySnapshot snapshot, AIAnalysisOptions options);

    /// <summary>
    /// Builds a structured context object for AI analysis
    /// </summary>
    Task<AIAnalysisContext> BuildStructuredContextAsync(TelemetrySnapshot snapshot);

    /// <summary>
    /// Redacts sensitive data from context
    /// </summary>
    string RedactSensitiveData(string context, RedactionOptions options);

    /// <summary>
    /// Summarizes telemetry data for context
    /// </summary>
    Task<string> SummarizeTelemetryAsync(TelemetrySnapshot snapshot);
}

/// <summary>
/// Structured context for AI analysis
/// </summary>
public class AIAnalysisContext
{
    public string Url { get; set; } = string.Empty;
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
    public List<string> ErrorMessages { get; set; } = new();
    public List<string> WarningMessages { get; set; } = new();
    public NetworkSummary NetworkSummary { get; set; } = new();
    public PerformanceSummary PerformanceSummary { get; set; } = new();
    public string? DomStructure { get; set; }
    public Dictionary<string, object> AdditionalContext { get; set; } = new();
}

/// <summary>
/// Summary of network activity
/// </summary>
public class NetworkSummary
{
    public int TotalRequests { get; set; }
    public int FailedRequests { get; set; }
    public double AverageResponseTimeMs { get; set; }
    public List<string> FailedUrls { get; set; } = new();
    public List<string> SlowRequests { get; set; } = new();
}

/// <summary>
/// Summary of performance metrics
/// </summary>
public class PerformanceSummary
{
    public double AverageCpuUsage { get; set; }
    public long PeakMemoryUsageBytes { get; set; }
    public double LoadTimeMs { get; set; }
    public double FirstContentfulPaintMs { get; set; }
    public int DomNodeCount { get; set; }
}

/// <summary>
/// Options for data redaction
/// </summary>
public class RedactionOptions
{
    public bool RedactUrls { get; set; } = false;
    public bool RedactApiKeys { get; set; } = true;
    public bool RedactTokens { get; set; } = true;
    public bool RedactPasswords { get; set; } = true;
    public bool RedactEmails { get; set; } = true;
    public bool RedactPhoneNumbers { get; set; } = true;
    public List<string> CustomPatterns { get; set; } = new();
}
