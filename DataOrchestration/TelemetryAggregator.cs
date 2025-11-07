using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using AIDebugPro.Core.Interfaces;
using AIDebugPro.Core.Models;
using AIDebugPro.Core.Constants;
using AIDebugPro.Core.Exceptions;

namespace AIDebugPro.DataOrchestration;

/// <summary>
/// Aggregates and manages telemetry data from browser
/// </summary>
public class TelemetryAggregator : ITelemetryAggregator
{
    private readonly ConcurrentDictionary<Guid, SessionTelemetryData> _sessionData;
    private readonly ILogger<TelemetryAggregator>? _logger;
    private readonly SemaphoreSlim _semaphore;
    private readonly TelemetryAggregatorOptions _options;

    public TelemetryAggregator(
        TelemetryAggregatorOptions? options = null,
        ILogger<TelemetryAggregator>? logger = null)
    {
        _sessionData = new ConcurrentDictionary<Guid, SessionTelemetryData>();
        _logger = logger;
        _semaphore = new SemaphoreSlim(1, 1);
        _options = options ?? new TelemetryAggregatorOptions();
    }

    #region Add Telemetry Methods

    /// <summary>
    /// Adds a console message to the aggregator
    /// </summary>
    public async Task AddConsoleMessageAsync(Guid sessionId, ConsoleMessage message)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var data = GetOrCreateSessionData(sessionId);

        await _semaphore.WaitAsync();
        try
        {
            data.ConsoleMessages.Add(message);

            // Enforce max limit
            if (data.ConsoleMessages.Count > _options.MaxConsoleMessages)
            {
                var toRemove = data.ConsoleMessages.Count - _options.MaxConsoleMessages;
                data.ConsoleMessages.RemoveRange(0, toRemove);
                
                _logger?.LogDebug(
                    "Trimmed {Count} oldest console messages for session {SessionId}",
                    toRemove,
                    sessionId);
            }

            _logger?.LogTrace(
                "Added console message ({Level}) to session {SessionId}. Total: {Count}",
                message.Level,
                sessionId,
                data.ConsoleMessages.Count);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Adds a network request to the aggregator
    /// </summary>
    public async Task AddNetworkRequestAsync(Guid sessionId, NetworkRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var data = GetOrCreateSessionData(sessionId);

        await _semaphore.WaitAsync();
        try
        {
            data.NetworkRequests.Add(request);

            // Enforce max limit
            if (data.NetworkRequests.Count > _options.MaxNetworkRequests)
            {
                var toRemove = data.NetworkRequests.Count - _options.MaxNetworkRequests;
                data.NetworkRequests.RemoveRange(0, toRemove);
                
                _logger?.LogDebug(
                    "Trimmed {Count} oldest network requests for session {SessionId}",
                    toRemove,
                    sessionId);
            }

            _logger?.LogTrace(
                "Added network request ({Method} {Url}) to session {SessionId}. Total: {Count}",
                request.Method,
                request.Url,
                sessionId,
                data.NetworkRequests.Count);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Adds performance metrics to the aggregator
    /// </summary>
    public async Task AddPerformanceMetricsAsync(Guid sessionId, PerformanceMetrics metrics)
    {
        if (metrics == null)
            throw new ArgumentNullException(nameof(metrics));

        var data = GetOrCreateSessionData(sessionId);

        await _semaphore.WaitAsync();
        try
        {
            data.PerformanceMetrics.Add(metrics);

            // Keep only recent metrics (default: last 100)
            if (data.PerformanceMetrics.Count > _options.MaxPerformanceMetrics)
            {
                var toRemove = data.PerformanceMetrics.Count - _options.MaxPerformanceMetrics;
                data.PerformanceMetrics.RemoveRange(0, toRemove);
            }

            _logger?.LogTrace(
                "Added performance metrics to session {SessionId}. CPU: {Cpu}%, Memory: {Memory}MB",
                sessionId,
                metrics.CpuUsage,
                metrics.MemoryUsageBytes / (1024 * 1024));
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Sets the DOM snapshot for a session
    /// </summary>
    public async Task SetDomSnapshotAsync(Guid sessionId, DOMSnapshot snapshot)
    {
        if (snapshot == null)
            throw new ArgumentNullException(nameof(snapshot));

        var data = GetOrCreateSessionData(sessionId);

        await _semaphore.WaitAsync();
        try
        {
            data.DomSnapshot = snapshot;
            
            _logger?.LogDebug(
                "Set DOM snapshot for session {SessionId}. Nodes: {NodeCount}",
                sessionId,
                snapshot.Nodes.Count);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    #endregion

    #region Snapshot Creation

    /// <summary>
    /// Creates a telemetry snapshot for a session
    /// </summary>
    public async Task<TelemetrySnapshot> CreateSnapshotAsync(Guid sessionId, CaptureOptions? options = null)
    {
        var data = GetOrCreateSessionData(sessionId);
        options ??= new CaptureOptions();

        await _semaphore.WaitAsync();
        try
        {
            var snapshot = new TelemetrySnapshot
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                CapturedAt = DateTime.UtcNow
            };

            // Add console messages if requested
            if (options.CaptureConsole)
            {
                snapshot.ConsoleMessages = GetFilteredConsoleMessages(
                    data.ConsoleMessages,
                    options.TimeWindow,
                    options.MaxConsoleMessages);
            }

            // Add network requests if requested
            if (options.CaptureNetwork)
            {
                snapshot.NetworkRequests = GetFilteredNetworkRequests(
                    data.NetworkRequests,
                    options.TimeWindow,
                    options.MaxNetworkRequests);
            }

            // Add performance metrics if requested
            if (options.CapturePerformance)
            {
                snapshot.PerformanceMetrics = GetFilteredPerformanceMetrics(
                    data.PerformanceMetrics,
                    options.TimeWindow);
            }

            // Add DOM snapshot if requested
            if (options.CaptureDom && data.DomSnapshot != null)
            {
                snapshot.DomSnapshot = data.DomSnapshot;
            }

            _logger?.LogInformation(
                "Created snapshot {SnapshotId} for session {SessionId}: {Console} console, {Network} network, {Perf} metrics",
                snapshot.Id,
                sessionId,
                snapshot.ConsoleMessages.Count,
                snapshot.NetworkRequests.Count,
                snapshot.PerformanceMetrics.Count);

            return snapshot;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    #endregion

    #region Get Telemetry Methods

    /// <summary>
    /// Gets all console messages for a session
    /// </summary>
    public Task<IEnumerable<ConsoleMessage>> GetConsoleMessagesAsync(
        Guid sessionId,
        TimeSpan? timeWindow = null)
    {
        var data = GetOrCreateSessionData(sessionId);
        
        IEnumerable<ConsoleMessage> messages = timeWindow.HasValue
            ? data.ConsoleMessages.Where(m => 
                DateTime.UtcNow - m.Timestamp <= timeWindow.Value)
            : data.ConsoleMessages;

        _logger?.LogDebug(
            "Retrieved {Count} console messages for session {SessionId}",
            messages.Count(),
            sessionId);

        return Task.FromResult(messages);
    }

    /// <summary>
    /// Gets all network requests for a session
    /// </summary>
    public Task<IEnumerable<NetworkRequest>> GetNetworkRequestsAsync(
        Guid sessionId,
        TimeSpan? timeWindow = null)
    {
        var data = GetOrCreateSessionData(sessionId);
        
        IEnumerable<NetworkRequest> requests = timeWindow.HasValue
            ? data.NetworkRequests.Where(r => 
                DateTime.UtcNow - r.Timestamp <= timeWindow.Value)
            : data.NetworkRequests;

        _logger?.LogDebug(
            "Retrieved {Count} network requests for session {SessionId}",
            requests.Count(),
            sessionId);

        return Task.FromResult(requests);
    }

    /// <summary>
    /// Gets performance metrics for a session
    /// </summary>
    public Task<IEnumerable<PerformanceMetrics>> GetPerformanceMetricsAsync(
        Guid sessionId,
        TimeSpan? timeWindow = null)
    {
        var data = GetOrCreateSessionData(sessionId);
        
        IEnumerable<PerformanceMetrics> metrics = timeWindow.HasValue
            ? data.PerformanceMetrics.Where(m => 
                DateTime.UtcNow - m.Timestamp <= timeWindow.Value)
            : data.PerformanceMetrics;

        _logger?.LogDebug(
            "Retrieved {Count} performance metrics for session {SessionId}",
            metrics.Count(),
            sessionId);

        return Task.FromResult(metrics);
    }

    /// <summary>
    /// Gets the current DOM snapshot for a session
    /// </summary>
    public Task<DOMSnapshot?> GetDomSnapshotAsync(Guid sessionId)
    {
        var data = GetOrCreateSessionData(sessionId);
        
        _logger?.LogDebug(
            "Retrieved DOM snapshot for session {SessionId}: {HasSnapshot}",
            sessionId,
            data.DomSnapshot != null);

        return Task.FromResult(data.DomSnapshot);
    }

    #endregion

    #region Clear and Statistics

    /// <summary>
    /// Clears all telemetry data for a session
    /// </summary>
    public async Task ClearTelemetryAsync(Guid sessionId)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_sessionData.TryRemove(sessionId, out var data))
            {
                _logger?.LogInformation(
                    "Cleared telemetry for session {SessionId}: {Console} console, {Network} network, {Perf} metrics",
                    sessionId,
                    data.ConsoleMessages.Count,
                    data.NetworkRequests.Count,
                    data.PerformanceMetrics.Count);
            }
            else
            {
                _logger?.LogWarning(
                    "Attempted to clear telemetry for non-existent session {SessionId}",
                    sessionId);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Gets telemetry statistics for a session
    /// </summary>
    public Task<TelemetryStatistics> GetStatisticsAsync(Guid sessionId)
    {
        var data = GetOrCreateSessionData(sessionId);

        var stats = new TelemetryStatistics
        {
            TotalConsoleMessages = data.ConsoleMessages.Count,
            ConsoleErrors = data.ConsoleMessages.Count(m => m.Level == ConsoleMessageLevel.Error),
            ConsoleWarnings = data.ConsoleMessages.Count(m => m.Level == ConsoleMessageLevel.Warning),
            TotalNetworkRequests = data.NetworkRequests.Count,
            FailedNetworkRequests = data.NetworkRequests.Count(r => r.IsFailed),
            SuccessfulNetworkRequests = data.NetworkRequests.Count(r => !r.IsFailed),
            AverageNetworkResponseTimeMs = data.NetworkRequests.Any()
                ? data.NetworkRequests.Average(r => r.DurationMs)
                : 0,
            PerformanceMetricsCount = data.PerformanceMetrics.Count,
            HasDomSnapshot = data.DomSnapshot != null
        };

        // Find oldest and newest timestamps
        var allTimestamps = new List<DateTime>();
        allTimestamps.AddRange(data.ConsoleMessages.Select(m => m.Timestamp));
        allTimestamps.AddRange(data.NetworkRequests.Select(r => r.Timestamp));
        allTimestamps.AddRange(data.PerformanceMetrics.Select(p => p.Timestamp));

        if (allTimestamps.Any())
        {
            stats.OldestDataTimestamp = allTimestamps.Min();
            stats.NewestDataTimestamp = allTimestamps.Max();
        }

        _logger?.LogDebug(
            "Retrieved statistics for session {SessionId}: {Errors} errors, {Warnings} warnings, {Requests} requests",
            sessionId,
            stats.ConsoleErrors,
            stats.ConsoleWarnings,
            stats.TotalNetworkRequests);

        return Task.FromResult(stats);
    }

    #endregion

    #region Filter Methods

    /// <summary>
    /// Filters console messages by level
    /// </summary>
    public Task<IEnumerable<ConsoleMessage>> FilterConsoleMessagesByLevelAsync(
        Guid sessionId,
        ConsoleMessageLevel level)
    {
        var data = GetOrCreateSessionData(sessionId);
        
        IEnumerable<ConsoleMessage> filtered = data.ConsoleMessages.Where(m => m.Level == level);

        _logger?.LogDebug(
            "Filtered console messages by level {Level} for session {SessionId}: {Count} matches",
            level,
            sessionId,
            filtered.Count());

        return Task.FromResult(filtered);
    }

    /// <summary>
    /// Gets failed network requests
    /// </summary>
    public Task<IEnumerable<NetworkRequest>> GetFailedNetworkRequestsAsync(Guid sessionId)
    {
        var data = GetOrCreateSessionData(sessionId);
        
        IEnumerable<NetworkRequest> failed = data.NetworkRequests.Where(r => r.IsFailed);

        _logger?.LogDebug(
            "Retrieved {Count} failed network requests for session {SessionId}",
            failed.Count(),
            sessionId);

        return Task.FromResult(failed);
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Gets or creates session telemetry data
    /// </summary>
    private SessionTelemetryData GetOrCreateSessionData(Guid sessionId)
    {
        return _sessionData.GetOrAdd(sessionId, _ => new SessionTelemetryData
        {
            SessionId = sessionId,
            CreatedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Filters console messages by time window and limit
    /// </summary>
    private List<ConsoleMessage> GetFilteredConsoleMessages(
        List<ConsoleMessage> messages,
        TimeSpan? timeWindow,
        int? maxCount)
    {
        var filtered = messages.AsEnumerable();

        if (timeWindow.HasValue)
        {
            var cutoff = DateTime.UtcNow - timeWindow.Value;
            filtered = filtered.Where(m => m.Timestamp >= cutoff);
        }

        if (maxCount.HasValue)
        {
            filtered = filtered.OrderByDescending(m => m.Timestamp).Take(maxCount.Value);
        }

        return filtered.ToList();
    }

    /// <summary>
    /// Filters network requests by time window and limit
    /// </summary>
    private List<NetworkRequest> GetFilteredNetworkRequests(
        List<NetworkRequest> requests,
        TimeSpan? timeWindow,
        int? maxCount)
    {
        var filtered = requests.AsEnumerable();

        if (timeWindow.HasValue)
        {
            var cutoff = DateTime.UtcNow - timeWindow.Value;
            filtered = filtered.Where(r => r.Timestamp >= cutoff);
        }

        if (maxCount.HasValue)
        {
            filtered = filtered.OrderByDescending(r => r.Timestamp).Take(maxCount.Value);
        }

        return filtered.ToList();
    }

    /// <summary>
    /// Filters performance metrics by time window
    /// </summary>
    private List<PerformanceMetrics> GetFilteredPerformanceMetrics(
        List<PerformanceMetrics> metrics,
        TimeSpan? timeWindow)
    {
        if (timeWindow.HasValue)
        {
            var cutoff = DateTime.UtcNow - timeWindow.Value;
            return metrics.Where(m => m.Timestamp >= cutoff).ToList();
        }

        return metrics.ToList();
    }

    #endregion
}

#region Supporting Classes

/// <summary>
/// Holds telemetry data for a session
/// </summary>
internal class SessionTelemetryData
{
    public Guid SessionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ConsoleMessage> ConsoleMessages { get; set; } = new();
    public List<NetworkRequest> NetworkRequests { get; set; } = new();
    public List<PerformanceMetrics> PerformanceMetrics { get; set; } = new();
    public DOMSnapshot? DomSnapshot { get; set; }
}

/// <summary>
/// Options for telemetry aggregator configuration
/// </summary>
public class TelemetryAggregatorOptions
{
    public int MaxConsoleMessages { get; set; } = AppConstants.DefaultMaxConsoleMessages;
    public int MaxNetworkRequests { get; set; } = AppConstants.DefaultMaxNetworkRequests;
    public int MaxPerformanceMetrics { get; set; } = 100;
    public bool AutoTrimOldData { get; set; } = true;
    public TimeSpan DataRetention { get; set; } = TimeSpan.FromHours(1);
}

#endregion
