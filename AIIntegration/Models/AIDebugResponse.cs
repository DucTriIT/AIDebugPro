using AIDebugPro.Core.Models;

namespace AIDebugPro.AIIntegration.Models;

/// <summary>
/// AI debug response
/// </summary>
public class AIDebugResponse
{
    public string Message { get; set; } = string.Empty;
    public List<Guid> RelatedTelemetryIds { get; set; } = new();
    public List<CodeExample> CodeExamples { get; set; } = new();
    public IssueSeverity Severity { get; set; }
    public List<string> RecommendedFixes { get; set; } = new();
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Code example from AI
/// </summary>
public class CodeExample
{
    public string Language { get; set; } = "javascript";
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
