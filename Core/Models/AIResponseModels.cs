using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIDebugPro.Core.Models
{
    /// <summary>
    /// Represents the result of an AI analysis
    /// </summary>
    public class AIAnalysisResult
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid SessionId { get; set; }
        public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
        public string Model { get; set; } = string.Empty;
        public AIAnalysisStatus Status { get; set; } = AIAnalysisStatus.Pending;
        public List<Issue> Issues { get; set; } = new();
        public List<Recommendation> Recommendations { get; set; } = new();
        public PerformanceAssessment? PerformanceAssessment { get; set; }
        public string? Summary { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
        public int TokensUsed { get; set; }
        public double AnalysisDurationMs { get; set; }
    }

    /// <summary>
    /// Represents an issue identified by AI
    /// </summary>
    public class Issue
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public IssueSeverity Severity { get; set; }
        public IssueCategory Category { get; set; }
        public string? Source { get; set; }
        public int? LineNumber { get; set; }
        public string? CodeSnippet { get; set; }
        public List<string> PotentialCauses { get; set; } = new();
        public List<string> SuggestedFixes { get; set; } = new();
        public string? ImpactAnalysis { get; set; }
        public List<string> RelatedUrls { get; set; } = new();
    }

    /// <summary>
    /// Represents an AI recommendation for improvement
    /// </summary>
    public class Recommendation
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public RecommendationType Type { get; set; }
        public RecommendationPriority Priority { get; set; }
        public string? Rationale { get; set; }
        public List<string> ImplementationSteps { get; set; } = new();
        public string? ExpectedImpact { get; set; }
        public List<string> References { get; set; } = new();
    }

    /// <summary>
    /// Performance assessment from AI analysis
    /// </summary>
    public class PerformanceAssessment
    {
        public double OverallScore { get; set; }
        public PerformanceGrade Grade { get; set; }
        public Dictionary<string, MetricAssessment> MetricAssessments { get; set; } = new();
        public List<string> Strengths { get; set; } = new();
        public List<string> Weaknesses { get; set; } = new();
        public string? Summary { get; set; }
    }

    /// <summary>
    /// Assessment of a specific performance metric
    /// </summary>
    public class MetricAssessment
    {
        public string MetricName { get; set; } = string.Empty;
        public double Value { get; set; }
        public double Threshold { get; set; }
        public bool IsWithinThreshold { get; set; }
        public string? Assessment { get; set; }
        public string? Suggestion { get; set; }
    }

    /// <summary>
    /// Request to analyze telemetry data with AI
    /// </summary>
    public class AIAnalysisRequest
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid SessionId { get; set; }
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public TelemetrySnapshot TelemetryData { get; set; } = new();
        public AIAnalysisOptions Options { get; set; } = new();
        public string? CustomPrompt { get; set; }
    }

    /// <summary>
    /// Options for AI analysis
    /// </summary>
    public class AIAnalysisOptions
    {
        public string Model { get; set; } = "gpt-4";
        public bool AnalyzeErrors { get; set; } = true;
        public bool AnalyzePerformance { get; set; } = true;
        public bool AnalyzeNetworkIssues { get; set; } = true;
        public bool ProvideFixes { get; set; } = true;
        public bool IncludeCodeSnippets { get; set; } = true;
        public int MaxTokens { get; set; } = 4000;
        public double Temperature { get; set; } = 0.7;
    }

    /// <summary>
    /// Raw response from AI provider
    /// </summary>
    public class AIProviderResponse
    {
        public string Model { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int TokensUsed { get; set; }
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> RawResponse { get; set; } = new();
    }
}
