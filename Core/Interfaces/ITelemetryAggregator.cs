using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AIDebugPro.Core.Models;

namespace AIDebugPro.Core.Interfaces
{
    /// <summary>
    /// Aggregates and manages telemetry data from browser
    /// </summary>
    public interface ITelemetryAggregator
    {
        /// <summary>
        /// Adds a console message to the aggregator
        /// </summary>
        Task AddConsoleMessageAsync(Guid sessionId, ConsoleMessage message);

        /// <summary>
        /// Adds a network request to the aggregator
        /// </summary>
        Task AddNetworkRequestAsync(Guid sessionId, NetworkRequest request);

        /// <summary>
        /// Adds performance metrics to the aggregator
        /// </summary>
        Task AddPerformanceMetricsAsync(Guid sessionId, PerformanceMetrics metrics);

        /// <summary>
        /// Sets the DOM snapshot for a session
        /// </summary>
        Task SetDomSnapshotAsync(Guid sessionId, DOMSnapshot snapshot);

        /// <summary>
        /// Creates a telemetry snapshot for a session
        /// </summary>
        Task<TelemetrySnapshot> CreateSnapshotAsync(Guid sessionId, CaptureOptions? options = null);

        /// <summary>
        /// Gets all console messages for a session
        /// </summary>
        Task<IEnumerable<ConsoleMessage>> GetConsoleMessagesAsync(Guid sessionId, TimeSpan? timeWindow = null);

        /// <summary>
        /// Gets all network requests for a session
        /// </summary>
        Task<IEnumerable<NetworkRequest>> GetNetworkRequestsAsync(Guid sessionId, TimeSpan? timeWindow = null);

        /// <summary>
        /// Gets performance metrics for a session
        /// </summary>
        Task<IEnumerable<PerformanceMetrics>> GetPerformanceMetricsAsync(Guid sessionId, TimeSpan? timeWindow = null);

        /// <summary>
        /// Gets the current DOM snapshot for a session
        /// </summary>
        Task<DOMSnapshot?> GetDomSnapshotAsync(Guid sessionId);

        /// <summary>
        /// Clears all telemetry data for a session
        /// </summary>
        Task ClearTelemetryAsync(Guid sessionId);

        /// <summary>
        /// Gets telemetry statistics for a session
        /// </summary>
        Task<TelemetryStatistics> GetStatisticsAsync(Guid sessionId);

        /// <summary>
        /// Filters console messages by level
        /// </summary>
        Task<IEnumerable<ConsoleMessage>> FilterConsoleMessagesByLevelAsync(
            Guid sessionId,
            ConsoleMessageLevel level);

        /// <summary>
        /// Gets failed network requests
        /// </summary>
        Task<IEnumerable<NetworkRequest>> GetFailedNetworkRequestsAsync(Guid sessionId);
    }

    /// <summary>
    /// Statistics about collected telemetry data
    /// </summary>
    public class TelemetryStatistics
    {
        public int TotalConsoleMessages { get; set; }
        public int ConsoleErrors { get; set; }
        public int ConsoleWarnings { get; set; }
        public int TotalNetworkRequests { get; set; }
        public int FailedNetworkRequests { get; set; }
        public int SuccessfulNetworkRequests { get; set; }
        public double AverageNetworkResponseTimeMs { get; set; }
        public int PerformanceMetricsCount { get; set; }
        public bool HasDomSnapshot { get; set; }
        public DateTime? OldestDataTimestamp { get; set; }
        public DateTime? NewestDataTimestamp { get; set; }
    }
}
