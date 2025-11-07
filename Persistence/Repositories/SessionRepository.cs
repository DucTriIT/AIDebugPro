using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using AIDebugPro.Core.Models;
using AIDebugPro.Persistence.Database;

namespace AIDebugPro.Persistence.Repositories;

/// <summary>
/// Repository for debug session operations
/// </summary>
public class SessionRepository
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<SessionRepository>? _logger;

    public SessionRepository(AppDbContext dbContext, ILogger<SessionRepository>? logger = null)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger;
    }

    #region Create & Update

    /// <summary>
    /// Saves a debug session to the database
    /// </summary>
    public async Task<Guid> SaveSessionAsync(DebugSession session)
    {
        if (session == null)
            throw new ArgumentNullException(nameof(session));

        return await Task.Run(() =>
        {
            var dbSession = MapToDbModel(session);
            _dbContext.Sessions.Upsert(dbSession);

            _logger?.LogInformation("Saved session {SessionId} ({Name})", session.Id, session.Name);

            return session.Id;
        });
    }

    /// <summary>
    /// Updates session statistics
    /// </summary>
    public async Task UpdateSessionStatisticsAsync(Guid sessionId, SessionStatistics stats)
    {
        await Task.Run(() =>
        {
            var dbSession = _dbContext.Sessions.FindById(sessionId);
            if (dbSession == null)
                throw new KeyNotFoundException($"Session {sessionId} not found");

            dbSession.TotalConsoleErrors = stats.TotalConsoleErrors;
            dbSession.TotalConsoleWarnings = stats.TotalConsoleWarnings;
            dbSession.TotalNetworkRequests = stats.TotalNetworkRequests;
            dbSession.FailedNetworkRequests = stats.FailedNetworkRequests;
            dbSession.AverageResponseTimeMs = stats.AverageResponseTimeMs;
            dbSession.PeakCpuUsage = stats.PeakCpuUsage;
            dbSession.PeakMemoryUsageBytes = stats.PeakMemoryUsageBytes;
            dbSession.SnapshotCount = stats.SnapshotCount;
            dbSession.AIAnalysisCount = stats.AIAnalysisCount;

            _dbContext.Sessions.Update(dbSession);

            _logger?.LogDebug("Updated statistics for session {SessionId}", sessionId);
        });
    }

    #endregion

    #region Read

    /// <summary>
    /// Gets a session by ID
    /// </summary>
    public async Task<DebugSession?> GetSessionAsync(Guid sessionId)
    {
        return await Task.Run(() =>
        {
            var dbSession = _dbContext.Sessions.FindById(sessionId);
            if (dbSession == null)
                return null;

            var session = MapFromDbModel(dbSession);

            _logger?.LogDebug("Retrieved session {SessionId}", sessionId);

            return session;
        });
    }

    /// <summary>
    /// Gets all sessions
    /// </summary>
    public async Task<List<DebugSession>> GetAllSessionsAsync()
    {
        return await Task.Run(() =>
        {
            var dbSessions = _dbContext.Sessions
                .FindAll()
                .OrderByDescending(s => s.StartedAt)
                .ToList();

            var sessions = dbSessions.Select(MapFromDbModel).ToList();

            _logger?.LogDebug("Retrieved {Count} sessions", sessions.Count);

            return sessions;
        });
    }

    /// <summary>
    /// Gets sessions by status
    /// </summary>
    public async Task<List<DebugSession>> GetSessionsByStatusAsync(string status)
    {
        return await Task.Run(() =>
        {
            var dbSessions = _dbContext.Sessions
                .Find(s => s.Status == status)
                .OrderByDescending(s => s.StartedAt)
                .ToList();

            return dbSessions.Select(MapFromDbModel).ToList();
        });
    }

    /// <summary>
    /// Gets recent sessions
    /// </summary>
    public async Task<List<DebugSession>> GetRecentSessionsAsync(int count = 10)
    {
        return await Task.Run(() =>
        {
            var dbSessions = _dbContext.Sessions
                .FindAll()
                .OrderByDescending(s => s.StartedAt)
                .Take(count)
                .ToList();

            return dbSessions.Select(MapFromDbModel).ToList();
        });
    }

    /// <summary>
    /// Searches sessions by name or URL
    /// </summary>
    public async Task<List<DebugSession>> SearchSessionsAsync(string searchTerm)
    {
        return await Task.Run(() =>
        {
            var dbSessions = _dbContext.Sessions
                .Find(s => s.Name.Contains(searchTerm) || s.Url.Contains(searchTerm))
                .OrderByDescending(s => s.StartedAt)
                .ToList();

            return dbSessions.Select(MapFromDbModel).ToList();
        });
    }

    #endregion

    #region Delete

    /// <summary>
    /// Deletes a session
    /// </summary>
    public async Task<bool> DeleteSessionAsync(Guid sessionId)
    {
        return await Task.Run(() =>
        {
            var success = _dbContext.Sessions.Delete(sessionId);

            if (success)
            {
                // Also delete associated snapshots and analysis results
                _dbContext.Snapshots.DeleteMany(s => s.SessionId == sessionId);
                _dbContext.AnalysisResults.DeleteMany(a => a.SessionId == sessionId);

                _logger?.LogInformation("Deleted session {SessionId} and associated data", sessionId);
            }

            return success;
        });
    }

    #endregion

    #region Snapshots

    /// <summary>
    /// Saves a telemetry snapshot
    /// </summary>
    public async Task<Guid> SaveSnapshotAsync(TelemetrySnapshot snapshot)
    {
        if (snapshot == null)
            throw new ArgumentNullException(nameof(snapshot));

        return await Task.Run(() =>
        {
            var dbSnapshot = new DbTelemetrySnapshot
            {
                Id = snapshot.Id,
                SessionId = snapshot.SessionId,
                CapturedAt = snapshot.CapturedAt,
                DataJson = JsonConvert.SerializeObject(snapshot)
            };

            _dbContext.Snapshots.Upsert(dbSnapshot);

            _logger?.LogDebug("Saved snapshot {SnapshotId} for session {SessionId}",
                snapshot.Id, snapshot.SessionId);

            return snapshot.Id;
        });
    }

    /// <summary>
    /// Gets snapshots for a session
    /// </summary>
    public async Task<List<TelemetrySnapshot>> GetSnapshotsAsync(Guid sessionId)
    {
        return await Task.Run(() =>
        {
            var dbSnapshots = _dbContext.Snapshots
                .Find(s => s.SessionId == sessionId)
                .OrderByDescending(s => s.CapturedAt)
                .ToList();

            var snapshots = dbSnapshots
                .Select(ds => JsonConvert.DeserializeObject<TelemetrySnapshot>(ds.DataJson))
                .Where(s => s != null)
                .Cast<TelemetrySnapshot>()
                .ToList();

            _logger?.LogDebug("Retrieved {Count} snapshots for session {SessionId}",
                snapshots.Count, sessionId);

            return snapshots;
        });
    }

    #endregion

    #region Analysis Results

    /// <summary>
    /// Saves an AI analysis result
    /// </summary>
    public async Task<Guid> SaveAnalysisResultAsync(AIAnalysisResult result)
    {
        if (result == null)
            throw new ArgumentNullException(nameof(result));

        return await Task.Run(() =>
        {
            var dbResult = new DbAIAnalysisResult
            {
                Id = result.Id,
                SessionId = result.SessionId,
                AnalyzedAt = result.AnalyzedAt,
                Model = result.Model,
                Status = result.Status.ToString(),
                ResultJson = JsonConvert.SerializeObject(result),
                TokensUsed = result.TokensUsed,
                AnalysisDurationMs = result.AnalysisDurationMs
            };

            _dbContext.AnalysisResults.Upsert(dbResult);

            _logger?.LogDebug("Saved analysis result {ResultId} for session {SessionId}",
                result.Id, result.SessionId);

            return result.Id;
        });
    }

    /// <summary>
    /// Gets analysis results for a session
    /// </summary>
    public async Task<List<AIAnalysisResult>> GetAnalysisResultsAsync(Guid sessionId)
    {
        return await Task.Run(() =>
        {
            var dbResults = _dbContext.AnalysisResults
                .Find(a => a.SessionId == sessionId)
                .OrderByDescending(a => a.AnalyzedAt)
                .ToList();

            var results = dbResults
                .Select(dr => JsonConvert.DeserializeObject<AIAnalysisResult>(dr.ResultJson))
                .Where(r => r != null)
                .Cast<AIAnalysisResult>()
                .ToList();

            _logger?.LogDebug("Retrieved {Count} analysis results for session {SessionId}",
                results.Count, sessionId);

            return results;
        });
    }

    #endregion

    #region Mapping

    private DbDebugSession MapToDbModel(DebugSession session)
    {
        return new DbDebugSession
        {
            Id = session.Id,
            Name = session.Name,
            Description = session.Description,
            Url = session.Url,
            StartedAt = session.StartedAt,
            EndedAt = session.EndedAt,
            Status = session.Status.ToString(),
            Tags = session.Tags,
            TotalConsoleErrors = session.Statistics.TotalConsoleErrors,
            TotalConsoleWarnings = session.Statistics.TotalConsoleWarnings,
            TotalNetworkRequests = session.Statistics.TotalNetworkRequests,
            FailedNetworkRequests = session.Statistics.FailedNetworkRequests,
            AverageResponseTimeMs = session.Statistics.AverageResponseTimeMs,
            PeakCpuUsage = session.Statistics.PeakCpuUsage,
            PeakMemoryUsageBytes = session.Statistics.PeakMemoryUsageBytes,
            SnapshotCount = session.Statistics.SnapshotCount,
            AIAnalysisCount = session.Statistics.AIAnalysisCount
        };
    }

    private DebugSession MapFromDbModel(DbDebugSession dbSession)
    {
        return new DebugSession
        {
            Id = dbSession.Id,
            Name = dbSession.Name,
            Description = dbSession.Description,
            Url = dbSession.Url,
            StartedAt = dbSession.StartedAt,
            EndedAt = dbSession.EndedAt,
            Status = Enum.Parse<SessionStatus>(dbSession.Status),
            Tags = dbSession.Tags,
            Statistics = new SessionStatistics
            {
                TotalConsoleErrors = dbSession.TotalConsoleErrors,
                TotalConsoleWarnings = dbSession.TotalConsoleWarnings,
                TotalNetworkRequests = dbSession.TotalNetworkRequests,
                FailedNetworkRequests = dbSession.FailedNetworkRequests,
                AverageResponseTimeMs = dbSession.AverageResponseTimeMs,
                PeakCpuUsage = dbSession.PeakCpuUsage,
                PeakMemoryUsageBytes = dbSession.PeakMemoryUsageBytes,
                SnapshotCount = dbSession.SnapshotCount,
                AIAnalysisCount = dbSession.AIAnalysisCount
            },
            Snapshots = new List<TelemetrySnapshot>(), // Loaded separately
            AnalysisResults = new List<AIAnalysisResult>() // Loaded separately
        };
    }

    #endregion
}
