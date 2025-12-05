using AIDebugPro.Core.Interfaces;
using AIDebugPro.Core.Models;
using AIDebugPro.AIIntegration.Models;
using Microsoft.Extensions.Logging;

namespace AIDebugPro.AIIntegration;

/// <summary>
/// Builds telemetry context for AI analysis
/// </summary>
public class TelemetryContextBuilder
{
    private readonly ITelemetryAggregator _aggregator;
    private readonly ILogger<TelemetryContextBuilder>? _logger;
    
    public TelemetryContextBuilder(
        ITelemetryAggregator aggregator,
        ILogger<TelemetryContextBuilder>? logger = null)
    {
        _aggregator = aggregator ?? throw new ArgumentNullException(nameof(aggregator));
        _logger = logger;
    }
    
    /// <summary>
    /// Builds context from current telemetry state
    /// </summary>
    public async Task<TelemetryContext> BuildContextAsync(
        Guid sessionId,
        ActiveTab currentTab,
        string? currentUrl = null,
        object? selectedItem = null)
    {
        try
        {
            _logger?.LogDebug("Building telemetry context for session {SessionId}, tab {Tab}",
                sessionId, currentTab);

            // Get recent telemetry (last 30 seconds)
            var consoleMessages = await _aggregator.GetConsoleMessagesAsync(
                sessionId, TimeSpan.FromSeconds(30));
            
            var networkRequests = await _aggregator.GetNetworkRequestsAsync(
                sessionId, TimeSpan.FromSeconds(30));
            
            var performanceMetrics = await _aggregator.GetPerformanceMetricsAsync(
                sessionId, TimeSpan.FromSeconds(10));
            
            var stats = await _aggregator.GetStatisticsAsync(sessionId);
            
            var context = new TelemetryContext
            {
                SessionId = sessionId,
                CurrentTab = currentTab,
                RecentConsoleMessages = consoleMessages.ToList(),
                RecentNetworkRequests = networkRequests.ToList(),
                LatestPerformanceMetrics = performanceMetrics
                    .OrderByDescending(m => m.Timestamp)
                    .FirstOrDefault(),
                SelectedConsoleMessage = selectedItem as ConsoleMessage,
                SelectedNetworkRequest = selectedItem as NetworkRequest,
                SessionStatistics = stats,
                CurrentUrl = currentUrl,
                Timestamp = DateTime.UtcNow
            };

            _logger?.LogInformation("Context built: {Console} console, {Network} network, {Performance} perf metrics",
                context.RecentConsoleMessages.Count,
                context.RecentNetworkRequests.Count,
                context.LatestPerformanceMetrics != null ? 1 : 0);

            return context;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error building telemetry context");
            throw;
        }
    }
}
