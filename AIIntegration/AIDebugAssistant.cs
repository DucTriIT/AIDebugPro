using AIDebugPro.Core.Interfaces;
using AIDebugPro.Core.Models;
using AIDebugPro.AIIntegration.Interfaces;
using AIDebugPro.AIIntegration.Models;
using Microsoft.Extensions.Logging;

namespace AIDebugPro.AIIntegration;

/// <summary>
/// AI-powered debug assistant with telemetry context awareness
/// </summary>
public class AIDebugAssistant
{
    private readonly IAIClient _aiClient;
    private readonly PromptComposer _promptComposer;
    private readonly ILogger<AIDebugAssistant>? _logger;

    public AIDebugAssistant(
        IAIClient aiClient,
        PromptComposer promptComposer,
        ILogger<AIDebugAssistant>? logger = null)
    {
        _aiClient = aiClient ?? throw new ArgumentNullException(nameof(aiClient));
        _promptComposer = promptComposer ?? throw new ArgumentNullException(nameof(promptComposer));
        _logger = logger;
    }

    /// <summary>
    /// Analyzes user query with telemetry context
    /// </summary>
    public async Task<AIDebugResponse> AnalyzeAsync(
        string userQuery,
        TelemetryContext context)
    {
        try
        {
            _logger?.LogInformation("?? Analyzing user query with telemetry context");
            _logger?.LogDebug("Query: {Query}, Context: {Tab} tab, {Console} console msgs, {Network} network reqs",
                userQuery, context.CurrentTab, context.RecentConsoleMessages.Count, context.RecentNetworkRequests.Count);

            // Compose context-aware prompt
            var prompt = _promptComposer.ComposeDebugPrompt(
                userQuery,
                context
            );

            _logger?.LogDebug("Calling OpenAI with debug prompt ({Length} chars)",
                prompt.Length);

            // Call OpenAI
            var response = await _aiClient.AnalyzeAsync(prompt);

            // Parse response
            var debugResponse = new AIDebugResponse
            {
                Message = response.Summary ?? string.Join("\n\n", response.Issues.Select(i => $"**{i.Title}**\n{i.Description}")),
                Severity = DetermineSeverity(context),
                RelatedTelemetryIds = ExtractRelatedIds(response, context),
                CodeExamples = ExtractCodeExamples(response),
                RecommendedFixes = response.Recommendations.Select(r => r.Description).ToList()
            };

            _logger?.LogInformation("? AI analysis complete: {Severity} severity, {Fixes} recommended fixes",
                debugResponse.Severity, debugResponse.RecommendedFixes.Count);

            return debugResponse;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "? Error during AI analysis");
            return new AIDebugResponse
            {
                Message = $"Sorry, I encountered an error analyzing your request: {ex.Message}",
                Severity = IssueSeverity.Critical  // Changed from Unknown to Critical
            };
        }
    }

    /// <summary>
    /// Analyzes a specific console error
    /// </summary>
    public async Task<AIDebugResponse> AnalyzeConsoleErrorAsync(
        ConsoleMessage error,
        TelemetryContext context)
    {
        var query = $"Explain this console error: {error.Message}";
        context.SelectedConsoleMessage = error;
        return await AnalyzeAsync(query, context);
    }

    /// <summary>
    /// Analyzes a network failure
    /// </summary>
    public async Task<AIDebugResponse> AnalyzeNetworkFailureAsync(
        NetworkRequest request,
        TelemetryContext context)
    {
        var query = $"Why did this network request fail? {request.Method} {request.Url} -> {request.StatusCode}";
        context.SelectedNetworkRequest = request;
        return await AnalyzeAsync(query, context);
    }

    /// <summary>
    /// Analyzes performance issues
    /// </summary>
    public async Task<AIDebugResponse> AnalyzePerformanceAsync(
        TelemetryContext context)
    {
        var query = "Analyze the performance metrics and identify bottlenecks";
        context.CurrentTab = ActiveTab.Performance;
        return await AnalyzeAsync(query, context);
    }

    /// <summary>
    /// Generates session summary
    /// </summary>
    public async Task<AIDebugResponse> AnalyzeSessionAsync(Guid sessionId, TelemetryContext context)
    {
        var stats = context.SessionStatistics;
        var query = $"Summarize the {stats.ConsoleErrors} errors, {stats.ConsoleWarnings} warnings, " +
                   $"and {stats.FailedNetworkRequests} network failures detected in this debugging session.";
        
        return await AnalyzeAsync(query, context);
    }

    #region Private Helper Methods

    private IssueSeverity DetermineSeverity(TelemetryContext context)
    {
        var errors = context.RecentConsoleMessages.Count(m => m.Level == ConsoleMessageLevel.Error);
        var failures = context.RecentNetworkRequests.Count(r => r.IsFailed);

        if (errors > 5 || failures > 3) return IssueSeverity.Critical;
        if (errors > 0 || failures > 0) return IssueSeverity.High;
        if (context.RecentConsoleMessages.Any(m => m.Level == ConsoleMessageLevel.Warning))
            return IssueSeverity.Medium;
        
        return IssueSeverity.Low;
    }

    private List<Guid> ExtractRelatedIds(AIAnalysisResult result, TelemetryContext context)
    {
        // TODO: Parse AI response for referenced telemetry items
        // For now, return IDs of selected items and recent errors
        var ids = new List<Guid>();
        
        if (context.SelectedConsoleMessage != null)
            ids.Add(context.SelectedConsoleMessage.Id);
        
        if (context.SelectedNetworkRequest != null)
            ids.Add(context.SelectedNetworkRequest.Id);

        // Add IDs of recent errors mentioned in response
        var recentErrors = context.RecentConsoleMessages
            .Where(m => m.Level == ConsoleMessageLevel.Error)
            .Take(3);
        
        ids.AddRange(recentErrors.Select(e => e.Id));

        return ids.Distinct().ToList();
    }

    private List<CodeExample> ExtractCodeExamples(AIAnalysisResult result)
    {
        // TODO: Parse code blocks from AI response
        // For now, return empty list - will be enhanced in Phase 2
        return new List<CodeExample>();
    }

    #endregion
}
