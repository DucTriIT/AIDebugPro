using AIDebugPro.Core.Models;

namespace AIDebugPro.AIIntegration.Interfaces;

/// <summary>
/// Abstraction for AI provider implementations
/// </summary>
public interface IAIClient
{
    /// <summary>
    /// Analyzes telemetry data and returns AI insights
    /// </summary>
    Task<AIAnalysisResult> AnalyzeAsync(
        string prompt,
        AIAnalysisOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes telemetry snapshot directly
    /// </summary>
    Task<AIAnalysisResult> AnalyzeSnapshotAsync(
        TelemetrySnapshot snapshot,
        AIAnalysisOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a chat message and gets a response
    /// </summary>
    Task<string> ChatAsync(
        string message,
        string? conversationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available AI models
    /// </summary>
    Task<List<string>> GetAvailableModelsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates API configuration
    /// </summary>
    Task<bool> ValidateConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Estimates token count for a prompt
    /// </summary>
    int EstimateTokenCount(string text);

    /// <summary>
    /// Gets provider name
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Checks if the client is configured
    /// </summary>
    bool IsConfigured { get; }
}
