using Microsoft.Web.WebView2.Core;
using Microsoft.Extensions.Logging;
using AIDebugPro.Core.Interfaces;
using AIDebugPro.Core.Models;
using System.Text.Json;

namespace AIDebugPro.BrowserIntegration.DevToolsProtocol;

/// <summary>
/// Collects performance metrics from the browser
/// </summary>
public class PerformanceCollector : IPerformanceCollector, IDisposable
{
    private readonly CoreWebView2 _coreWebView;
    private readonly ITelemetryAggregator _telemetryAggregator;
    private readonly ILogger? _logger;
    private readonly SynchronizationContext? _uiContext;
    private Guid _currentSessionId;
    private bool _isCollecting;
    private System.Threading.Timer? _collectTimer;

    public PerformanceCollector(
        CoreWebView2 coreWebView,
        ITelemetryAggregator telemetryAggregator,
        ILogger? logger = null)
    {
        _coreWebView = coreWebView ?? throw new ArgumentNullException(nameof(coreWebView));
        _telemetryAggregator = telemetryAggregator ?? throw new ArgumentNullException(nameof(telemetryAggregator));
        _logger = logger;
        
        // Capture the current synchronization context (should be UI thread)
        _uiContext = SynchronizationContext.Current;
    }

    /// <summary>
    /// Starts collecting performance metrics
    /// /// </summary>
    public async Task StartAsync(Guid sessionId)
    {
        if (_isCollecting)
        {
            _logger?.LogWarning("PerformanceCollector already started");
            return;
        }

        _currentSessionId = sessionId;

        try
        {
            // Enable Performance domain
            await _coreWebView.CallDevToolsProtocolMethodAsync("Performance.enable", "{}");

            // Start periodic collection (every 5 seconds)
            _collectTimer = new System.Threading.Timer(
                _ => 
                {
                    // Marshal the call to the UI thread
                    if (_uiContext != null)
                    {
                        _uiContext.Post(async state => 
                        {
                            try
                            {
                                await CollectMetricsAsync(_currentSessionId);
                            }
                            catch (Exception ex)
                            {
                                _logger?.LogError(ex, "Error in timer callback");
                            }
                        }, null);
                    }
                    else
                    {
                        _logger?.LogWarning("SynchronizationContext not available, skipping metric collection");
                    }
                },
                null,
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(5));


            _isCollecting = true;
            _logger?.LogInformation("PerformanceCollector started for session {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to start PerformanceCollector");
            throw;
        }
    }

    /// <summary>
    /// Stops collecting performance metrics
    /// </summary>
    public async Task StopAsync()
    {
        if (!_isCollecting)
            return;

        try
        {
            _collectTimer?.Dispose();
            _collectTimer = null;

            // Disable Performance domain
            await _coreWebView.CallDevToolsProtocolMethodAsync("Performance.disable", "{}");

            _isCollecting = false;
            _logger?.LogInformation("PerformanceCollector stopped");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error stopping PerformanceCollector");
        }
    }

    /// <summary>
    /// Collects current performance metrics
    /// </summary>
    public async Task CollectMetricsAsync(Guid sessionId)
    {
        if (!_isCollecting)
            return;

        try
        {
            var metrics = new PerformanceMetrics
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow
            };

            // Get Performance metrics
            var metricsResult = await _coreWebView.CallDevToolsProtocolMethodAsync(
                "Performance.getMetrics",
                "{}");

            var metricsJson = JsonDocument.Parse(metricsResult);
            var metricsArray = metricsJson.RootElement.GetProperty("metrics");

            var metricsDict = new Dictionary<string, double>();
            foreach (var metric in metricsArray.EnumerateArray())
            {
                var name = metric.GetProperty("name").GetString() ?? "";
                var value = metric.GetProperty("value").GetDouble();
                metricsDict[name] = value;
            }

            // Get Navigation Timing (for page load metrics)
            var timingScript = @"
                (function() {
                    const perf = window.performance;
                    const timing = perf.timing;
                    const navigation = perf.navigation;
                    const paint = perf.getEntriesByType('paint');
                    
                    return {
                        domContentLoaded: timing.domContentLoadedEventEnd - timing.navigationStart,
                        loadComplete: timing.loadEventEnd - timing.navigationStart,
                        firstPaint: paint.find(p => p.name === 'first-paint')?.startTime || 0,
                        firstContentfulPaint: paint.find(p => p.name === 'first-contentful-paint')?.startTime || 0,
                        domNodes: document.getElementsByTagName('*').length,
                        memory: performance.memory ? {
                            used: performance.memory.usedJSHeapSize,
                            total: performance.memory.totalJSHeapSize,
                            limit: performance.memory.jsHeapSizeLimit
                        } : null
                    };
                })();
            ";

            var timingResult = await _coreWebView.ExecuteScriptAsync(timingScript);
            var timingJson = JsonDocument.Parse(timingResult);
            var timing = timingJson.RootElement;

            // Populate metrics
            metrics.DomContentLoadedMs = timing.GetProperty("domContentLoaded").GetDouble();
            metrics.LoadEventMs = timing.GetProperty("loadComplete").GetDouble();
            metrics.FirstPaintMs = timing.GetProperty("firstPaint").GetDouble();
            metrics.FirstContentfulPaintMs = timing.GetProperty("firstContentfulPaint").GetDouble();
            metrics.DomNodeCount = timing.GetProperty("domNodes").GetInt32();

            // Get memory info
            if (timing.TryGetProperty("memory", out var memory) && memory.ValueKind != JsonValueKind.Null)
            {
                metrics.MemoryUsageBytes = memory.GetProperty("used").GetInt64();
                metrics.JavaScriptHeapSizeBytes = (int)memory.GetProperty("used").GetInt64();
            }

            // Get LCP (Largest Contentful Paint) if available
            var lcpScript = @"
                (function() {
                    const entries = performance.getEntriesByType('largest-contentful-paint');
                    return entries.length > 0 ? entries[entries.length - 1].startTime : 0;
                })();
            ";

            try
            {
                var lcpResult = await _coreWebView.ExecuteScriptAsync(lcpScript);
                metrics.LargestContentfulPaintMs = double.Parse(lcpResult);
            }
            catch
            {
                // LCP not available
            }

            // Estimate CPU usage from metrics (if available)
            if (metricsDict.TryGetValue("TaskDuration", out var taskDuration))
            {
                metrics.CpuUsage = Math.Min(100, taskDuration * 100);
            }

            // Store custom metrics
            metrics.CustomMetrics = metricsDict;

            await _telemetryAggregator.AddPerformanceMetricsAsync(sessionId, metrics);

            _logger?.LogDebug(
                "Collected metrics: Load={LoadMs}ms, FCP={FCP}ms, LCP={LCP}ms, DOM={Nodes}",
                metrics.LoadEventMs,
                metrics.FirstContentfulPaintMs,
                metrics.LargestContentfulPaintMs,
                metrics.DomNodeCount);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error collecting performance metrics");
        }
    }

    public void Dispose()
    {
        _collectTimer?.Dispose();
        if (_isCollecting)
        {
            StopAsync().Wait();
        }
    }
}
