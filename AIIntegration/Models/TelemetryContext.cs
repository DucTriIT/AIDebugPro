using AIDebugPro.Core.Models;
using AIDebugPro.Core.Interfaces;

namespace AIDebugPro.AIIntegration.Models;

/// <summary>
/// Telemetry context for AI analysis
/// </summary>
public class TelemetryContext
{
    public Guid SessionId { get; set; }
    public ActiveTab CurrentTab { get; set; }
    public List<ConsoleMessage> RecentConsoleMessages { get; set; } = new();
    public List<NetworkRequest> RecentNetworkRequests { get; set; } = new();
    public PerformanceMetrics? LatestPerformanceMetrics { get; set; }
    public ConsoleMessage? SelectedConsoleMessage { get; set; }
    public NetworkRequest? SelectedNetworkRequest { get; set; }
    public TelemetryStatistics SessionStatistics { get; set; } = new();
    public string? CurrentUrl { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Active telemetry tab
/// </summary>
public enum ActiveTab
{
    Console,
    Network,
    Performance
}
