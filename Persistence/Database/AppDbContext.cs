using LiteDB;
using Microsoft.Extensions.Logging;
using AIDebugPro.Core.Constants;

namespace AIDebugPro.Persistence.Database;

/// <summary>
/// Database context for LiteDB
/// </summary>
public class AppDbContext : IDisposable
{
    private readonly LiteDatabase _database;
    private readonly ILogger<AppDbContext>? _logger;
    private readonly string _databasePath;

    public AppDbContext(string? databasePath = null, ILogger<AppDbContext>? logger = null)
    {
        _logger = logger;
        _databasePath = databasePath ?? GetDefaultDatabasePath();

        // Ensure database directory exists
        var directory = Path.GetDirectoryName(_databasePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Initialize LiteDB with configuration
        var connectionString = new ConnectionString
        {
            Filename = _databasePath,
            Connection = ConnectionType.Direct,
            Upgrade = true
        };

        _database = new LiteDatabase(connectionString);

        _logger?.LogInformation("Database initialized at {Path}", _databasePath);

        // Configure collections
        ConfigureCollections();
    }

    #region Collections

    /// <summary>
    /// Debug sessions collection
    /// </summary>
    public ILiteCollection<DbDebugSession> Sessions => _database.GetCollection<DbDebugSession>("sessions");

    /// <summary>
    /// Telemetry snapshots collection
    /// </summary>
    public ILiteCollection<DbTelemetrySnapshot> Snapshots => _database.GetCollection<DbTelemetrySnapshot>("snapshots");

    /// <summary>
    /// AI analysis results collection
    /// </summary>
    public ILiteCollection<DbAIAnalysisResult> AnalysisResults => _database.GetCollection<DbAIAnalysisResult>("analysisResults");

    /// <summary>
    /// Application logs collection
    /// </summary>
    public ILiteCollection<DbLogEntry> Logs => _database.GetCollection<DbLogEntry>("logs");

    /// <summary>
    /// Application settings collection
    /// </summary>
    public ILiteCollection<DbSetting> Settings => _database.GetCollection<DbSetting>("settings");

    #endregion

    #region Configuration

    /// <summary>
    /// Configures database collections with indexes
    /// </summary>
    private void ConfigureCollections()
    {
        // Sessions indexes
        Sessions.EnsureIndex(x => x.Id, unique: true);
        Sessions.EnsureIndex(x => x.StartedAt);
        Sessions.EnsureIndex(x => x.Status);
        Sessions.EnsureIndex(x => x.Name);

        // Snapshots indexes
        Snapshots.EnsureIndex(x => x.Id, unique: true);
        Snapshots.EnsureIndex(x => x.SessionId);
        Snapshots.EnsureIndex(x => x.CapturedAt);

        // Analysis results indexes
        AnalysisResults.EnsureIndex(x => x.Id, unique: true);
        AnalysisResults.EnsureIndex(x => x.SessionId);
        AnalysisResults.EnsureIndex(x => x.AnalyzedAt);

        // Logs indexes
        Logs.EnsureIndex(x => x.Id, unique: true);
        Logs.EnsureIndex(x => x.Timestamp);
        Logs.EnsureIndex(x => x.Level);

        // Settings index
        Settings.EnsureIndex(x => x.Key, unique: true);

        _logger?.LogDebug("Database indexes configured");
    }

    #endregion

    #region Database Operations

    /// <summary>
    /// Compacts the database to reduce file size
    /// </summary>
    public long Compact()
    {
        var sizeBefore = new FileInfo(_databasePath).Length;
        _database.Rebuild();
        var sizeAfter = new FileInfo(_databasePath).Length;
        
        var savedBytes = sizeBefore - sizeAfter;
        _logger?.LogInformation(
            "Database compacted. Size: {Before}KB -> {After}KB (Saved: {Saved}KB)",
            sizeBefore / 1024,
            sizeAfter / 1024,
            savedBytes / 1024);

        return savedBytes;
    }

    /// <summary>
    /// Creates a backup of the database
    /// </summary>
    public string CreateBackup(string? backupPath = null)
    {
        backupPath ??= $"{_databasePath}.backup.{DateTime.UtcNow:yyyyMMddHHmmss}";

        File.Copy(_databasePath, backupPath, overwrite: true);

        _logger?.LogInformation("Database backup created at {Path}", backupPath);

        return backupPath;
    }

    /// <summary>
    /// Gets database statistics
    /// </summary>
    public DatabaseStatistics GetStatistics()
    {
        var fileInfo = new FileInfo(_databasePath);

        return new DatabaseStatistics
        {
            DatabasePath = _databasePath,
            FileSizeBytes = fileInfo.Exists ? fileInfo.Length : 0,
            SessionCount = Sessions.Count(),
            SnapshotCount = Snapshots.Count(),
            AnalysisResultCount = AnalysisResults.Count(),
            LogCount = Logs.Count(),
            SettingCount = Settings.Count()
        };
    }

    /// <summary>
    /// Cleans up old data based on retention policies
    /// </summary>
    public CleanupResult CleanupOldData(int retentionDays = 30)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
        var result = new CleanupResult();

        // Delete old snapshots
        result.SnapshotsDeleted = Snapshots.DeleteMany(s => s.CapturedAt < cutoffDate);

        // Delete old logs
        result.LogsDeleted = Logs.DeleteMany(l => l.Timestamp < cutoffDate);

        // Delete completed/archived sessions older than retention
        result.SessionsDeleted = Sessions.DeleteMany(s => 
            (s.Status == "Completed" || s.Status == "Archived") && 
            s.EndedAt.HasValue && 
            s.EndedAt.Value < cutoffDate);

        _logger?.LogInformation(
            "Cleanup completed: {Sessions} sessions, {Snapshots} snapshots, {Logs} logs deleted",
            result.SessionsDeleted,
            result.SnapshotsDeleted,
            result.LogsDeleted);

        return result;
    }

    #endregion

    #region Helper Methods

    private static string GetDefaultDatabasePath()
    {
        var dataFolder = AppConstants.DataFolder;
        if (!Directory.Exists(dataFolder))
        {
            Directory.CreateDirectory(dataFolder);
        }
        return Path.Combine(dataFolder, AppConstants.DatabaseFileName);
    }

    #endregion

    public void Dispose()
    {
        _database?.Dispose();
        _logger?.LogDebug("Database connection disposed");
    }
}

#region Database Models

/// <summary>
/// Database model for debug session
/// </summary>
public class DbDebugSession
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Url { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public Dictionary<string, string> Tags { get; set; } = new();
    public int TotalConsoleErrors { get; set; }
    public int TotalConsoleWarnings { get; set; }
    public int TotalNetworkRequests { get; set; }
    public int FailedNetworkRequests { get; set; }
    public double AverageResponseTimeMs { get; set; }
    public double PeakCpuUsage { get; set; }
    public long PeakMemoryUsageBytes { get; set; }
    public int SnapshotCount { get; set; }
    public int AIAnalysisCount { get; set; }
}

/// <summary>
/// Database model for telemetry snapshot
/// </summary>
public class DbTelemetrySnapshot
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public DateTime CapturedAt { get; set; }
    public string DataJson { get; set; } = string.Empty; // Serialized snapshot data
}

/// <summary>
/// Database model for AI analysis result
/// </summary>
public class DbAIAnalysisResult
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public DateTime AnalyzedAt { get; set; }
    public string Model { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ResultJson { get; set; } = string.Empty; // Serialized analysis data
    public int TokensUsed { get; set; }
    public double AnalysisDurationMs { get; set; }
}

/// <summary>
/// Database model for log entry
/// </summary>
public class DbLogEntry
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public string? Source { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
}

/// <summary>
/// Database model for application setting
/// </summary>
public class DbSetting
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Category { get; set; }
    public DateTime UpdatedAt { get; set; }
}

#endregion

#region Statistics & Results

/// <summary>
/// Database statistics
/// </summary>
public class DatabaseStatistics
{
    public string DatabasePath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public int SessionCount { get; set; }
    public int SnapshotCount { get; set; }
    public int AnalysisResultCount { get; set; }
    public int LogCount { get; set; }
    public int SettingCount { get; set; }
}

/// <summary>
/// Cleanup operation result
/// </summary>
public class CleanupResult
{
    public int SessionsDeleted { get; set; }
    public int SnapshotsDeleted { get; set; }
    public int AnalysisResultsDeleted { get; set; }
    public int LogsDeleted { get; set; }
}

#endregion
