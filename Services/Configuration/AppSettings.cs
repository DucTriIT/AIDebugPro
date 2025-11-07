using AIDebugPro.Core.Constants;

namespace AIDebugPro.Services.Configuration;

/// <summary>
/// Application settings loaded from configuration
/// </summary>
public class AppSettings
{
    public OpenAISettings OpenAI { get; set; } = new();
    public DatabaseSettings Database { get; set; } = new();
    public TelemetrySettings Telemetry { get; set; } = new();
    public LoggingSettings Logging { get; set; } = new();
    public AISettings AI { get; set; } = new();
}

/// <summary>
/// OpenAI configuration settings
/// </summary>
public class OpenAISettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = AIConstants.GPT4Model;
    public string? Organization { get; set; }
    public int MaxTokens { get; set; } = AIConstants.DefaultMaxTokens;
    public double Temperature { get; set; } = AIConstants.DefaultTemperature;
    public int TimeoutSeconds { get; set; } = AppConstants.DefaultAIRequestTimeoutSeconds;
}

/// <summary>
/// Database configuration settings
/// </summary>
public class DatabaseSettings
{
    public string Path { get; set; } = "data/aidebugpro.db";
    public bool EnableAutoCompact { get; set; } = true;
    public int BackupRetentionDays { get; set; } = 7;
}

/// <summary>
/// Telemetry collection settings
/// </summary>
public class TelemetrySettings
{
    public int MaxConsoleMessages { get; set; } = AppConstants.DefaultMaxConsoleMessages;
    public int MaxNetworkRequests { get; set; } = AppConstants.DefaultMaxNetworkRequests;
    public int SnapshotRetentionDays { get; set; } = AppConstants.DefaultSnapshotRetentionDays;
    public bool AutoCaptureEnabled { get; set; } = true;
    public int AutoCaptureIntervalSeconds { get; set; } = 30;
}

/// <summary>
/// Logging configuration settings
/// </summary>
public class LoggingSettings
{
    public string LogLevel { get; set; } = "Information";
    public string Path { get; set; } = AppConstants.LogsFolder;
    public int RetentionDays { get; set; } = 30;
    public bool EnableConsoleLogging { get; set; } = true;
    public bool EnableFileLogging { get; set; } = true;
}

/// <summary>
/// AI feature configuration settings
/// </summary>
public class AISettings
{
    public bool EnableAutoAnalysis { get; set; } = false;
    public int AnalysisThreshold { get; set; } = 5; // Trigger analysis after 5 errors
    public bool AnalyzeErrors { get; set; } = true;
    public bool AnalyzePerformance { get; set; } = true;
    public bool AnalyzeNetworkIssues { get; set; } = true;
    public bool ProvideFixes { get; set; } = true;
}
