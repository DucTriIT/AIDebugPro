using AIDebugPro.Core.Models;

namespace AIDebugPro.Core.Interfaces;

/// <summary>
/// Manages debug sessions lifecycle and operations
/// </summary>
public interface ISessionManager
{
    /// <summary>
    /// Creates and starts a new debug session
    /// </summary>
    Task<DebugSession> CreateSessionAsync(string name, string url, string? description = null);

    /// <summary>
    /// Gets a session by its ID
    /// </summary>
    Task<DebugSession?> GetSessionAsync(Guid sessionId);

    /// <summary>
    /// Gets all sessions
    /// </summary>
    Task<IEnumerable<DebugSession>> GetAllSessionsAsync();

    /// <summary>
    /// Gets active sessions
    /// </summary>
    Task<IEnumerable<DebugSession>> GetActiveSessionsAsync();

    /// <summary>
    /// Updates an existing session
    /// </summary>
    Task UpdateSessionAsync(DebugSession session);

    /// <summary>
    /// Ends a session
    /// </summary>
    Task EndSessionAsync(Guid sessionId);

    /// <summary>
    /// Pauses a session
    /// </summary>
    Task PauseSessionAsync(Guid sessionId);

    /// <summary>
    /// Resumes a paused session
    /// </summary>
    Task ResumeSessionAsync(Guid sessionId);

    /// <summary>
    /// Deletes a session and all its data
    /// </summary>
    Task DeleteSessionAsync(Guid sessionId);

    /// <summary>
    /// Archives a session
    /// </summary>
    Task ArchiveSessionAsync(Guid sessionId);

    /// <summary>
    /// Adds a snapshot to a session
    /// </summary>
    Task AddSnapshotAsync(Guid sessionId, TelemetrySnapshot snapshot);

    /// <summary>
    /// Adds an AI analysis result to a session
    /// </summary>
    Task AddAnalysisResultAsync(Guid sessionId, AIAnalysisResult analysisResult);

    /// <summary>
    /// Gets session statistics
    /// </summary>
    Task<SessionStatistics> GetSessionStatisticsAsync(Guid sessionId);

    /// <summary>
    /// Exports session data
    /// </summary>
    Task<string> ExportSessionAsync(Guid sessionId, ReportFormat format);

    /// <summary>
    /// Imports a session from external source (e.g., loaded from file)
    /// </summary>
    Task<DebugSession> ImportSessionAsync(DebugSession session);
}
