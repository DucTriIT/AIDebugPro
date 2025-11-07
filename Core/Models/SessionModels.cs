using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIDebugPro.Core.Models
{
    /// <summary>
    /// Represents a debugging session
    /// </summary>
    public class DebugSession
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? EndedAt { get; set; }
        public SessionStatus Status { get; set; } = SessionStatus.Active;
        public string Url { get; set; } = string.Empty;
        public List<TelemetrySnapshot> Snapshots { get; set; } = new();
        public List<AIAnalysisResult> AnalysisResults { get; set; } = new();
        public Dictionary<string, string> Tags { get; set; } = new();
        public SessionStatistics Statistics { get; set; } = new();
    }

    /// <summary>
    /// Statistical summary of a debug session
    /// </summary>
    public class SessionStatistics
    {
        public int TotalConsoleErrors { get; set; }
        public int TotalConsoleWarnings { get; set; }
        public int TotalNetworkRequests { get; set; }
        public int FailedNetworkRequests { get; set; }
        public double AverageResponseTimeMs { get; set; }
        public double PeakCpuUsage { get; set; }
        public long PeakMemoryUsageBytes { get; set; }
        public int SnapshotCount { get; set; }
        public int AIAnalysisCount { get; set; }
        public TimeSpan Duration => CalculateDuration();

        private TimeSpan CalculateDuration()
        {
            // This will be calculated based on session start/end times
            return TimeSpan.Zero;
        }
    }

    /// <summary>
    /// Represents a point-in-time capture request
    /// </summary>
    public class CaptureRequest
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid SessionId { get; set; }
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public CaptureOptions Options { get; set; } = new();
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Options for capturing telemetry data
    /// </summary>
    public class CaptureOptions
    {
        public bool CaptureConsole { get; set; } = true;
        public bool CaptureNetwork { get; set; } = true;
        public bool CapturePerformance { get; set; } = true;
        public bool CaptureDom { get; set; } = true;
        public TimeSpan? TimeWindow { get; set; }
        public int? MaxConsoleMessages { get; set; }
        public int? MaxNetworkRequests { get; set; }
    }
}
