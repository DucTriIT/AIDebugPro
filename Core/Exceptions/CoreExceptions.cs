namespace AIDebugPro.Core.Exceptions;

/// <summary>
/// Base exception for AIDebugPro application
/// </summary>
public class AIDebugProException : Exception
{
    public AIDebugProException() : base() { }
    
    public AIDebugProException(string message) : base(message) { }
    
    public AIDebugProException(string message, Exception innerException) 
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when a session is not found
/// </summary>
public class SessionNotFoundException : AIDebugProException
{
    public Guid SessionId { get; }
    
    public SessionNotFoundException(Guid sessionId) 
        : base($"Session with ID '{sessionId}' was not found.")
    {
        SessionId = sessionId;
    }
    
    public SessionNotFoundException(Guid sessionId, string message) : base(message)
    {
        SessionId = sessionId;
    }
}

/// <summary>
/// Exception thrown when a session operation is invalid
/// </summary>
public class InvalidSessionOperationException : AIDebugProException
{
    public Guid SessionId { get; }
    
    public InvalidSessionOperationException(Guid sessionId, string message) : base(message)
    {
        SessionId = sessionId;
    }
    
    public InvalidSessionOperationException(Guid sessionId, string message, Exception innerException) 
        : base(message, innerException)
    {
        SessionId = sessionId;
    }
}

/// <summary>
/// Exception thrown when AI analysis fails
/// </summary>
public class AIAnalysisException : AIDebugProException
{
    public string? Model { get; }
    public int? TokensUsed { get; }
    
    public AIAnalysisException(string message) : base(message) { }
    
    public AIAnalysisException(string message, string model) : base(message)
    {
        Model = model;
    }
    
    public AIAnalysisException(string message, Exception innerException) 
        : base(message, innerException) { }
    
    public AIAnalysisException(string message, string model, int tokensUsed) : base(message)
    {
        Model = model;
        TokensUsed = tokensUsed;
    }
}

/// <summary>
/// Exception thrown when AI provider configuration is invalid
/// </summary>
public class AIProviderConfigurationException : AIDebugProException
{
    public string? ProviderName { get; }
    
    public AIProviderConfigurationException(string message) : base(message) { }
    
    public AIProviderConfigurationException(string message, string providerName) : base(message)
    {
        ProviderName = providerName;
    }
}

/// <summary>
/// Exception thrown when telemetry capture fails
/// </summary>
public class TelemetryCaptureException : AIDebugProException
{
    public string? TelemetryType { get; }
    
    public TelemetryCaptureException(string message) : base(message) { }
    
    public TelemetryCaptureException(string message, string telemetryType) : base(message)
    {
        TelemetryType = telemetryType;
    }
    
    public TelemetryCaptureException(string message, Exception innerException) 
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when WebView2 initialization fails
/// </summary>
public class WebView2InitializationException : AIDebugProException
{
    public WebView2InitializationException(string message) : base(message) { }
    
    public WebView2InitializationException(string message, Exception innerException) 
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when Chrome DevTools Protocol operation fails
/// </summary>
public class CDPException : AIDebugProException
{
    public string? EventName { get; }
    
    public CDPException(string message) : base(message) { }
    
    public CDPException(string message, string eventName) : base(message)
    {
        EventName = eventName;
    }
    
    public CDPException(string message, Exception innerException) 
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when database operation fails
/// </summary>
public class DatabaseException : AIDebugProException
{
    public string? Operation { get; }
    
    public DatabaseException(string message) : base(message) { }
    
    public DatabaseException(string message, string operation) : base(message)
    {
        Operation = operation;
    }
    
    public DatabaseException(string message, Exception innerException) 
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when report generation fails
/// </summary>
public class ReportGenerationException : AIDebugProException
{
    public string? ReportFormat { get; }
    
    public ReportGenerationException(string message) : base(message) { }
    
    public ReportGenerationException(string message, string reportFormat) : base(message)
    {
        ReportFormat = reportFormat;
    }
    
    public ReportGenerationException(string message, Exception innerException) 
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when configuration is invalid or missing
/// </summary>
public class ConfigurationException : AIDebugProException
{
    public string? ConfigurationKey { get; }
    
    public ConfigurationException(string message) : base(message) { }
    
    public ConfigurationException(string message, string configurationKey) : base(message)
    {
        ConfigurationKey = configurationKey;
    }
}
