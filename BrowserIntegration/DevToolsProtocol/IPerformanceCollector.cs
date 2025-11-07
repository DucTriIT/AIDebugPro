namespace AIDebugPro.BrowserIntegration.DevToolsProtocol;

/// <summary>
/// Interface for collecting performance metrics from the browser
/// </summary>
public interface IPerformanceCollector : IDisposable
{
    /// <summary>
    /// Starts collecting performance metrics
    /// </summary>
    /// <param name="sessionId">The session identifier</param>
    Task StartAsync(Guid sessionId);

    /// <summary>
    /// Stops collecting performance metrics
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// Collects current performance metrics
    /// </summary>
    /// <param name="sessionId">The session identifier</param>
    Task CollectMetricsAsync(Guid sessionId);
}