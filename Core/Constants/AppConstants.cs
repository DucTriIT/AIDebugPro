namespace AIDebugPro.Core.Constants;

/// <summary>
/// Application-wide constants
/// </summary>
public static class AppConstants
{
    public const string ApplicationName = "AIDebugPro";
    public const string ApplicationVersion = "1.0.0";
    public const string DatabaseFileName = "aidebugpro.db";
    public const string LogFileName = "aidebugpro-.txt";
    
    // Default values
    public const int DefaultMaxConsoleMessages = 1000;
    public const int DefaultMaxNetworkRequests = 500;
    public const int DefaultSnapshotRetentionDays = 30;
    
    // Timeouts
    public const int DefaultRequestTimeoutSeconds = 30;
    public const int DefaultAIRequestTimeoutSeconds = 120;
    
    // File paths
    public const string LogsFolder = "logs";
    public const string DataFolder = "data";
    public const string ReportsFolder = "reports";
    public const string TemplatesFolder = "templates";
}

/// <summary>
/// AI-related constants
/// </summary>
public static class AIConstants
{
    // OpenAI Models
    public const string GPT4Model = "gpt-4";
    public const string GPT4TurboModel = "gpt-4-turbo";
    public const string GPT35TurboModel = "gpt-3.5-turbo";
    
    // Token limits
    public const int MaxTokensGPT4 = 8192;
    public const int MaxTokensGPT4Turbo = 128000;
    public const int MaxTokensGPT35Turbo = 16385;
    
    // Default analysis parameters
    public const double DefaultTemperature = 0.7;
    public const int DefaultMaxTokens = 4000;
    
    // Prompts
    public const string SystemPromptPrefix = "You are an expert web developer and debugger analyzing browser telemetry data.";
}

/// <summary>
/// Performance thresholds
/// </summary>
public static class PerformanceThresholds
{
    // Load times (milliseconds)
    public const double GoodLoadTimeMs = 2000;
    public const double FairLoadTimeMs = 4000;
    public const double PoorLoadTimeMs = 8000;
    
    // First Contentful Paint (milliseconds)
    public const double GoodFCPMs = 1800;
    public const double FairFCPMs = 3000;
    public const double PoorFCPMs = 5000;
    
    // Largest Contentful Paint (milliseconds)
    public const double GoodLCPMs = 2500;
    public const double FairLCPMs = 4000;
    public const double PoorLCPMs = 6000;
    
    // Network response time (milliseconds)
    public const double GoodResponseTimeMs = 200;
    public const double FairResponseTimeMs = 500;
    public const double PoorResponseTimeMs = 1000;
    
    // Memory (bytes)
    public const long GoodMemoryUsageBytes = 100 * 1024 * 1024;  // 100 MB
    public const long FairMemoryUsageBytes = 250 * 1024 * 1024;  // 250 MB
    public const long PoorMemoryUsageBytes = 500 * 1024 * 1024;  // 500 MB
    
    // CPU usage (percentage)
    public const double GoodCpuUsage = 30;
    public const double FairCpuUsage = 60;
    public const double PoorCpuUsage = 90;
}

/// <summary>
/// Chrome DevTools Protocol event names
/// </summary>
public static class CDPEvents
{
    // Console events
    public const string ConsoleMessageAdded = "Runtime.consoleAPICalled";
    public const string ExceptionThrown = "Runtime.exceptionThrown";
    
    // Network events
    public const string RequestWillBeSent = "Network.requestWillBeSent";
    public const string ResponseReceived = "Network.responseReceived";
    public const string LoadingFinished = "Network.loadingFinished";
    public const string LoadingFailed = "Network.loadingFailed";
    
    // Performance events
    public const string Metrics = "Performance.metrics";
    
    // DOM events
    public const string DocumentUpdated = "DOM.documentUpdated";
}

/// <summary>
/// UI-related constants
/// </summary>
public static class UIConstants
{
    // Window sizes
    public const int MinWindowWidth = 1024;
    public const int MinWindowHeight = 768;
    public const int DefaultWindowWidth = 1400;
    public const int DefaultWindowHeight = 900;
    
    // Panel sizes
    public const int WebViewMinWidth = 600;
    public const int AIAssistantMinWidth = 400;
    public const int LogsDashboardMinHeight = 200;
    
    // Colors (for styling reference)
    public const string ErrorColor = "#DC3545";
    public const string WarningColor = "#FFC107";
    public const string SuccessColor = "#28A745";
    public const string InfoColor = "#17A2B8";
}

/// <summary>
/// Configuration keys
/// </summary>
public static class ConfigurationKeys
{
    public const string OpenAIApiKey = "OpenAI:ApiKey";
    public const string OpenAIModel = "OpenAI:Model";
    public const string OpenAIOrganization = "OpenAI:Organization";
    
    public const string DatabasePath = "Database:Path";
    public const string LogLevel = "Logging:LogLevel";
    public const string LogPath = "Logging:Path";
    
    public const string MaxConsoleMessages = "Telemetry:MaxConsoleMessages";
    public const string MaxNetworkRequests = "Telemetry:MaxNetworkRequests";
    public const string SnapshotRetentionDays = "Telemetry:SnapshotRetentionDays";
    
    public const string EnableAutoAnalysis = "AI:EnableAutoAnalysis";
    public const string AnalysisThreshold = "AI:AnalysisThreshold";
}
