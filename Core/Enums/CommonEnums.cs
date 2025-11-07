namespace AIDebugPro.Core.Models;

/// <summary>
/// Console message severity levels
/// </summary>
public enum ConsoleMessageLevel
{
    Verbose,
    Debug,
    Info,
    Warning,
    Error
}

/// <summary>
/// Debug session status
/// </summary>
public enum SessionStatus
{
    Active,
    Paused,
    Completed,
    Failed,
    Archived
}

/// <summary>
/// AI analysis status
/// </summary>
public enum AIAnalysisStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    PartiallyCompleted
}

/// <summary>
/// Issue severity levels
/// </summary>
public enum IssueSeverity
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Issue categories
/// </summary>
public enum IssueCategory
{
    JavaScriptError,
    NetworkError,
    PerformanceIssue,
    SecurityVulnerability,
    Accessibility,
    BestPractice,
    CodeQuality,
    ResourceLoading,
    MemoryLeak,
    Other
}

/// <summary>
/// Recommendation types
/// </summary>
public enum RecommendationType
{
    Performance,
    Security,
    BestPractice,
    CodeOptimization,
    ErrorHandling,
    Accessibility,
    SEO,
    UserExperience,
    Maintenance
}

/// <summary>
/// Recommendation priority levels
/// </summary>
public enum RecommendationPriority
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Performance grade
/// </summary>
public enum PerformanceGrade
{
    A,  // Excellent
    B,  // Good
    C,  // Fair
    D,  // Poor
    F   // Failing
}

/// <summary>
/// Telemetry data types
/// </summary>
public enum TelemetryDataType
{
    Console,
    Network,
    Performance,
    DOM,
    All
}

/// <summary>
/// Report format types
/// </summary>
public enum ReportFormat
{
    PDF,
    HTML,
    JSON,
    Markdown
}

/// <summary>
/// AI provider types
/// </summary>
public enum AIProviderType
{
    OpenAI,
    AzureOpenAI,
    LocalLLM,
    Ollama,
    Custom
}
