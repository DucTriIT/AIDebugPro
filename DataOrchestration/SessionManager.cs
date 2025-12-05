using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AIDebugPro.Core.Exceptions;
using AIDebugPro.Core.Interfaces;
using AIDebugPro.Core.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AIDebugPro.DataOrchestration
{
    /// <summary>
    /// Manages debug sessions lifecycle and operations
    /// </summary>
    public class SessionManager : ISessionManager
    {
        private readonly ConcurrentDictionary<Guid, DebugSession> _sessions;
        private readonly ILogger<SessionManager>? _logger;
        private readonly SemaphoreSlim _semaphore;

        public SessionManager(ILogger<SessionManager>? logger = null)
        {
            _sessions = new ConcurrentDictionary<Guid, DebugSession>();
            _logger = logger;
            _semaphore = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// Creates and starts a new debug session
        /// </summary>
        public async Task<DebugSession> CreateSessionAsync(string name, string url, string? description = null)
        {
            await _semaphore.WaitAsync();
            try
            {
                var session = new DebugSession
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Url = url,
                    Description = description,
                    StartedAt = DateTime.UtcNow,
                    Status = SessionStatus.Active,
                    Snapshots = new List<TelemetrySnapshot>(),
                    AnalysisResults = new List<AIAnalysisResult>(),
                    Tags = new Dictionary<string, string>(),
                    Statistics = new SessionStatistics()
                };

                if (!_sessions.TryAdd(session.Id, session))
                {
                    throw new InvalidSessionOperationException(
                        session.Id,
                        "Failed to add session to collection");
                }

                _logger?.LogInformation(
                    "Created new session {SessionId} for URL {Url}",
                    session.Id,
                    url);

                return session;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Gets a session by its ID
        /// </summary>
        public Task<DebugSession?> GetSessionAsync(Guid sessionId)
        {
            _sessions.TryGetValue(sessionId, out var session);
            
            if (session == null)
            {
                _logger?.LogWarning("Session {SessionId} not found", sessionId);
            }

            return Task.FromResult(session);
        }

        /// <summary>
        /// Gets all sessions
        /// </summary>
        public Task<IEnumerable<DebugSession>> GetAllSessionsAsync()
        {
            IEnumerable<DebugSession> sessions = _sessions.Values.OrderByDescending(s => s.StartedAt);
            _logger?.LogDebug("Retrieved {Count} sessions", sessions.Count());
            return Task.FromResult(sessions);
        }

        /// <summary>
        /// Gets active sessions
        /// </summary>
        public Task<IEnumerable<DebugSession>> GetActiveSessionsAsync()
        {
            IEnumerable<DebugSession> activeSessions = _sessions.Values
                .Where(s => s.Status == SessionStatus.Active)
                .OrderByDescending(s => s.StartedAt);

            _logger?.LogDebug("Retrieved {Count} active sessions", activeSessions.Count());
            return Task.FromResult(activeSessions);
        }

        /// <summary>
        /// Updates an existing session
        /// </summary>
        public async Task UpdateSessionAsync(DebugSession session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            await _semaphore.WaitAsync();
            try
            {
                if (!_sessions.ContainsKey(session.Id))
                {
                    throw new SessionNotFoundException(session.Id);
                }

                _sessions[session.Id] = session;
                
                _logger?.LogInformation(
                    "Updated session {SessionId}",
                    session.Id);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Ends a session
        /// </summary>
        public async Task EndSessionAsync(Guid sessionId)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (!_sessions.TryGetValue(sessionId, out var session))
                {
                    throw new SessionNotFoundException(sessionId);
                }

                if (session.Status == SessionStatus.Completed)
                {
                    throw new InvalidSessionOperationException(
                        sessionId,
                        "Session is already completed");
                }

                session.Status = SessionStatus.Completed;
                session.EndedAt = DateTime.UtcNow;

                _logger?.LogInformation(
                    "Ended session {SessionId}, Duration: {Duration}",
                    sessionId,
                    session.EndedAt - session.StartedAt);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Pauses a session
        /// </summary>
        public async Task PauseSessionAsync(Guid sessionId)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (!_sessions.TryGetValue(sessionId, out var session))
                {
                    throw new SessionNotFoundException(sessionId);
                }

                if (session.Status != SessionStatus.Active)
                {
                    throw new InvalidSessionOperationException(
                        sessionId,
                        $"Cannot pause session with status {session.Status}");
                }

                session.Status = SessionStatus.Paused;
                
                _logger?.LogInformation("Paused session {SessionId}", sessionId);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Resumes a paused session
        /// </summary>
        public async Task ResumeSessionAsync(Guid sessionId)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (!_sessions.TryGetValue(sessionId, out var session))
                {
                    throw new SessionNotFoundException(sessionId);
                }

                if (session.Status != SessionStatus.Paused)
                {
                    throw new InvalidSessionOperationException(
                        sessionId,
                        $"Cannot resume session with status {session.Status}");
                }

                session.Status = SessionStatus.Active;
                
                _logger?.LogInformation("Resumed session {SessionId}", sessionId);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Deletes a session and all its data
        /// </summary>
        public async Task DeleteSessionAsync(Guid sessionId)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (!_sessions.TryRemove(sessionId, out var session))
                {
                    throw new SessionNotFoundException(sessionId);
                }

                _logger?.LogInformation(
                    "Deleted session {SessionId} with {SnapshotCount} snapshots",
                    sessionId,
                    session.Snapshots.Count);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Archives a session
        /// </summary>
        public async Task ArchiveSessionAsync(Guid sessionId)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (!_sessions.TryGetValue(sessionId, out var session))
                {
                    throw new SessionNotFoundException(sessionId);
                }

                if (session.Status == SessionStatus.Active)
                {
                    throw new InvalidSessionOperationException(
                        sessionId,
                        "Cannot archive an active session. End it first.");
                }

                session.Status = SessionStatus.Archived;
                
                _logger?.LogInformation("Archived session {SessionId}", sessionId);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Adds a snapshot to a session
        /// </summary>
        public async Task AddSnapshotAsync(Guid sessionId, TelemetrySnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            await _semaphore.WaitAsync();
            try
            {
                if (!_sessions.TryGetValue(sessionId, out var session))
                {
                    throw new SessionNotFoundException(sessionId);
                }

                snapshot.SessionId = sessionId;
                snapshot.CapturedAt = DateTime.UtcNow;
                session.Snapshots.Add(snapshot);

                // Update statistics
                UpdateSessionStatistics(session, snapshot);

                _logger?.LogDebug(
                    "Added snapshot to session {SessionId}. Total snapshots: {Count}",
                    sessionId,
                    session.Snapshots.Count);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Adds an AI analysis result to a session
        /// </summary>
        public async Task AddAnalysisResultAsync(Guid sessionId, AIAnalysisResult analysisResult)
        {
            if (analysisResult == null)
                throw new ArgumentNullException(nameof(analysisResult));

            await _semaphore.WaitAsync();
            try
            {
                if (!_sessions.TryGetValue(sessionId, out var session))
                {
                    throw new SessionNotFoundException(sessionId);
                }

                analysisResult.SessionId = sessionId;
                analysisResult.AnalyzedAt = DateTime.UtcNow;
                session.AnalysisResults.Add(analysisResult);

                session.Statistics.AIAnalysisCount = session.AnalysisResults.Count;

                _logger?.LogInformation(
                    "Added AI analysis result to session {SessionId}. Total analyses: {Count}",
                    sessionId,
                    session.AnalysisResults.Count);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Gets session statistics
        /// </summary>
        public Task<SessionStatistics> GetSessionStatisticsAsync(Guid sessionId)
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
            {
                throw new SessionNotFoundException(sessionId);
            }

            // Recalculate statistics from current data
            RecalculateStatistics(session);

            _logger?.LogDebug(
                "Retrieved statistics for session {SessionId}: {Errors} errors, {Warnings} warnings",
                sessionId,
                session.Statistics.TotalConsoleErrors,
                session.Statistics.TotalConsoleWarnings);

            return Task.FromResult(session.Statistics);
        }

        /// <summary>
        /// Exports session data
        /// </summary>
        public async Task<string> ExportSessionAsync(Guid sessionId, ReportFormat format)
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
            {
                throw new SessionNotFoundException(sessionId);
            }

            // Recalculate statistics from snapshots
            RecalculateStatistics(session);

            string exportedData = format switch
            {
                ReportFormat.JSON => ExportAsJson(session),
                ReportFormat.HTML => ExportAsHtml(session),
                ReportFormat.Markdown => ExportAsMarkdown(session),
                ReportFormat.PDF => throw new NotImplementedException("PDF export not yet implemented"),
                _ => throw new ArgumentException($"Unsupported format: {format}", nameof(format))
            };

            _logger?.LogInformation(
                "Exported session {SessionId} as {Format} with {SnapshotCount} snapshots",
                sessionId,
                format,
                session.Snapshots.Count);

            return exportedData;
        }

        /// <summary>
        /// Imports a session from external source (e.g., loaded from file)
        /// </summary>
        public async Task<DebugSession> ImportSessionAsync(DebugSession session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            await _semaphore.WaitAsync();
            try
            {
                // Generate a new ID to avoid conflicts
                var originalId = session.Id;
                session.Id = Guid.NewGuid();
                session.Status = SessionStatus.Completed; // Mark as archived/completed

                // Update all related IDs in snapshots
                foreach (var snapshot in session.Snapshots)
                {
                    snapshot.SessionId = session.Id;
                }

                // Update all related IDs in analysis results
                foreach (var analysis in session.AnalysisResults)
                {
                    analysis.SessionId = session.Id;
                }

                // Add to sessions dictionary
                if (!_sessions.TryAdd(session.Id, session))
                {
                    throw new InvalidSessionOperationException(
                        session.Id,
                        "Failed to import session - ID conflict");
                }

                _logger?.LogInformation(
                    "Imported session {OriginalId} as {NewId}: {Name} with {Snapshots} snapshots, {Analyses} analyses",
                    originalId,
                    session.Id,
                    session.Name,
                    session.Snapshots.Count,
                    session.AnalysisResults.Count);

                return session;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Updates session statistics when a snapshot is added
        /// </summary>
        private void UpdateSessionStatistics(DebugSession session, TelemetrySnapshot snapshot)
        {
            var stats = session.Statistics;

            // Update console message counts
            var errorCount = snapshot.ConsoleMessages.Count(m => m.Level == ConsoleMessageLevel.Error);
            var warningCount = snapshot.ConsoleMessages.Count(m => m.Level == ConsoleMessageLevel.Warning);
            
            stats.TotalConsoleErrors += errorCount;
            stats.TotalConsoleWarnings += warningCount;

            // Update network request counts
            stats.TotalNetworkRequests += snapshot.NetworkRequests.Count;
            stats.FailedNetworkRequests += snapshot.NetworkRequests.Count(r => r.IsFailed);

            // Update average response time
            if (snapshot.NetworkRequests.Any())
            {
                var avgResponseTime = snapshot.NetworkRequests.Average(r => r.DurationMs);
                stats.AverageResponseTimeMs = 
                    (stats.AverageResponseTimeMs * (stats.TotalNetworkRequests - snapshot.NetworkRequests.Count) + 
                     avgResponseTime * snapshot.NetworkRequests.Count) / stats.TotalNetworkRequests;
            }

            // Update performance metrics
            if (snapshot.PerformanceMetrics.Any())
            {
                var maxCpu = snapshot.PerformanceMetrics.Max(p => p.CpuUsage);
                var maxMemory = snapshot.PerformanceMetrics.Max(p => p.MemoryUsageBytes);

                if (maxCpu > stats.PeakCpuUsage)
                    stats.PeakCpuUsage = maxCpu;

                if (maxMemory > stats.PeakMemoryUsageBytes)
                    stats.PeakMemoryUsageBytes = maxMemory;
            }

            stats.SnapshotCount = session.Snapshots.Count;
        }

        /// <summary>
        /// Recalculates all statistics from scratch
        /// </summary>
        private void RecalculateStatistics(DebugSession session)
        {
            var stats = session.Statistics;

            stats.TotalConsoleErrors = session.Snapshots
                .SelectMany(s => s.ConsoleMessages)
                .Count(m => m.Level == ConsoleMessageLevel.Error);

            stats.TotalConsoleWarnings = session.Snapshots
                .SelectMany(s => s.ConsoleMessages)
                .Count(m => m.Level == ConsoleMessageLevel.Warning);

            var allRequests = session.Snapshots.SelectMany(s => s.NetworkRequests).ToList();
            stats.TotalNetworkRequests = allRequests.Count;
            stats.FailedNetworkRequests = allRequests.Count(r => r.IsFailed);

            if (allRequests.Any())
            {
                stats.AverageResponseTimeMs = allRequests.Average(r => r.DurationMs);
            }

            var allMetrics = session.Snapshots.SelectMany(s => s.PerformanceMetrics).ToList();
            if (allMetrics.Any())
            {
                stats.PeakCpuUsage = allMetrics.Max(p => p.CpuUsage);
                stats.PeakMemoryUsageBytes = allMetrics.Max(p => p.MemoryUsageBytes);
            }

            stats.SnapshotCount = session.Snapshots.Count;
            stats.AIAnalysisCount = session.AnalysisResults.Count;
        }

        /// <summary>
        /// Exports session as JSON
        /// </summary>
        private string ExportAsJson(DebugSession session)
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore
            };

            return JsonConvert.SerializeObject(session, settings);
        }

        /// <summary>
        /// Exports session as HTML
        /// </summary>
        private string ExportAsHtml(DebugSession session)
        {
            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Debug Session Report - {session.Name}</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        h1 {{ color: #333; }}
        .section {{ margin: 20px 0; }}
        .stats {{ display: grid; grid-template-columns: repeat(2, 1fr); gap: 10px; }}
        .stat-box {{ border: 1px solid #ddd; padding: 10px; border-radius: 4px; }}
        .error {{ color: #dc3545; }}
        .warning {{ color: #ffc107; }}
        .success {{ color: #28a745; }}
    </style>
</head>
<body>
    <h1>Debug Session: {session.Name}</h1>
    <div class='section'>
        <h2>Session Information</h2>
        <p><strong>URL:</strong> {session.Url}</p>
        <p><strong>Started:</strong> {session.StartedAt:yyyy-MM-dd HH:mm:ss} UTC</p>
        <p><strong>Status:</strong> {session.Status}</p>
        {(session.EndedAt.HasValue ? $"<p><strong>Ended:</strong> {session.EndedAt:yyyy-MM-dd HH:mm:ss} UTC</p>" : "")}
    </div>
    <div class='section'>
        <h2>Statistics</h2>
        <div class='stats'>
            <div class='stat-box error'><strong>Console Errors:</strong> {session.Statistics.TotalConsoleErrors}</div>
            <div class='stat-box warning'><strong>Console Warnings:</strong> {session.Statistics.TotalConsoleWarnings}</div>
            <div class='stat-box'><strong>Network Requests:</strong> {session.Statistics.TotalNetworkRequests}</div>
            <div class='stat-box error'><strong>Failed Requests:</strong> {session.Statistics.FailedNetworkRequests}</div>
            <div class='stat-box'><strong>Avg Response Time:</strong> {session.Statistics.AverageResponseTimeMs:F2} ms</div>
            <div class='stat-box'><strong>Snapshots:</strong> {session.Statistics.SnapshotCount}</div>
            <div class='stat-box'><strong>AI Analyses:</strong> {session.Statistics.AIAnalysisCount}</div>
            <div class='stat-box'><strong>Peak CPU:</strong> {session.Statistics.PeakCpuUsage:F2}%</div>
        </div>
    </div>
</body>
</html>";

            return html;
        }

        /// <summary>
        /// Exports session as Markdown
        /// </summary>
        private string ExportAsMarkdown(DebugSession session)
        {
            var markdown = $@"# Debug Session Report: {session.Name}

## Session Information

- **URL**: {session.Url}
- **Started**: {session.StartedAt:yyyy-MM-dd HH:mm:ss} UTC
- **Status**: {session.Status}
{(session.EndedAt.HasValue ? $"- **Ended**: {session.EndedAt:yyyy-MM-dd HH:mm:ss} UTC" : "")}
{(!string.IsNullOrEmpty(session.Description) ? $"- **Description**: {session.Description}" : "")}

## Statistics

| Metric | Value |
|--------|-------|
| Console Errors | {session.Statistics.TotalConsoleErrors} |
| Console Warnings | {session.Statistics.TotalConsoleWarnings} |
| Network Requests | {session.Statistics.TotalNetworkRequests} |
| Failed Requests | {session.Statistics.FailedNetworkRequests} |
| Avg Response Time | {session.Statistics.AverageResponseTimeMs:F2} ms |
| Snapshots | {session.Statistics.SnapshotCount} |
| AI Analyses | {session.Statistics.AIAnalysisCount} |
| Peak CPU Usage | {session.Statistics.PeakCpuUsage:F2}% |
| Peak Memory Usage | {session.Statistics.PeakMemoryUsageBytes / (1024 * 1024):F2} MB |

## Analysis Results

{(session.AnalysisResults.Any() ? 
    string.Join("\n\n", session.AnalysisResults.Select(a => $@"### Analysis {a.AnalyzedAt:yyyy-MM-dd HH:mm:ss}
- **Model**: {a.Model}
- **Status**: {a.Status}
- **Issues Found**: {a.Issues.Count}
- **Recommendations**: {a.Recommendations.Count}
{(!string.IsNullOrEmpty(a.Summary) ? $"- **Summary**: {a.Summary}" : "")}")) 
    : "_No AI analyses performed yet_")}
";

            return markdown;
        }

        #endregion
    }
}
