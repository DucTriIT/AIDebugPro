using Microsoft.Extensions.Logging;
using AIDebugPro.Persistence.Database;

namespace AIDebugPro.Persistence.Repositories;

/// <summary>
/// Repository for application logs
/// </summary>
public class LogRepository
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<LogRepository>? _logger;

    public LogRepository(AppDbContext dbContext, ILogger<LogRepository>? logger = null)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger;
    }

    #region Create

    /// <summary>
    /// Adds a log entry
    /// </summary>
    public async Task AddLogAsync(
        string level,
        string message,
        string? exception = null,
        string? source = null,
        Dictionary<string, string>? properties = null)
    {
        await Task.Run(() =>
        {
            var logEntry = new DbLogEntry
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                Level = level,
                Message = message,
                Exception = exception,
                Source = source,
                Properties = properties ?? new Dictionary<string, string>()
            };

            _dbContext.Logs.Insert(logEntry);
        });
    }

    #endregion

    #region Read

    /// <summary>
    /// Gets recent logs
    /// </summary>
    public async Task<List<DbLogEntry>> GetRecentLogsAsync(int count = 100)
    {
        return await Task.Run(() =>
        {
            return _dbContext.Logs
                .FindAll()
                .OrderByDescending(l => l.Timestamp)
                .Take(count)
                .ToList();
        });
    }

    /// <summary>
    /// Gets logs by level
    /// </summary>
    public async Task<List<DbLogEntry>> GetLogsByLevelAsync(string level, int count = 100)
    {
        return await Task.Run(() =>
        {
            return _dbContext.Logs
                .Find(l => l.Level == level)
                .OrderByDescending(l => l.Timestamp)
                .Take(count)
                .ToList();
        });
    }

    /// <summary>
    /// Gets logs within a time range
    /// </summary>
    public async Task<List<DbLogEntry>> GetLogsByTimeRangeAsync(
        DateTime startTime,
        DateTime endTime)
    {
        return await Task.Run(() =>
        {
            return _dbContext.Logs
                .Find(l => l.Timestamp >= startTime && l.Timestamp <= endTime)
                .OrderByDescending(l => l.Timestamp)
                .ToList();
        });
    }

    /// <summary>
    /// Searches logs by message content
    /// </summary>
    public async Task<List<DbLogEntry>> SearchLogsAsync(string searchTerm)
    {
        return await Task.Run(() =>
        {
            return _dbContext.Logs
                .Find(l => l.Message.Contains(searchTerm) ||
                          (l.Exception != null && l.Exception.Contains(searchTerm)))
                .OrderByDescending(l => l.Timestamp)
                .ToList();
        });
    }

    /// <summary>
    /// Gets error logs
    /// </summary>
    public Task<List<DbLogEntry>> GetErrorLogsAsync(int count = 100) =>
        GetLogsByLevelAsync("Error", count);

    /// <summary>
    /// Gets warning logs
    /// </summary>
    public Task<List<DbLogEntry>> GetWarningLogsAsync(int count = 100) =>
        GetLogsByLevelAsync("Warning", count);

    #endregion

    #region Delete

    /// <summary>
    /// Deletes old logs
    /// </summary>
    public async Task<int> DeleteOldLogsAsync(int retentionDays = 30)
    {
        return await Task.Run(() =>
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
            var count = _dbContext.Logs.DeleteMany(l => l.Timestamp < cutoffDate);

            _logger?.LogInformation("Deleted {Count} old log entries", count);

            return count;
        });
    }

    /// <summary>
    /// Deletes all logs
    /// </summary>
    public async Task<int> ClearAllLogsAsync()
    {
        return await Task.Run(() =>
        {
            var count = _dbContext.Logs.DeleteAll();

            _logger?.LogInformation("Cleared all {Count} log entries", count);

            return count;
        });
    }

    #endregion

    #region Statistics

    /// <summary>
    /// Gets log statistics
    /// </summary>
    public async Task<LogStatistics> GetLogStatisticsAsync()
    {
        return await Task.Run(() =>
        {
            var all = _dbContext.Logs.FindAll().ToList();

            return new LogStatistics
            {
                TotalLogs = all.Count,
                ErrorCount = all.Count(l => l.Level == "Error"),
                WarningCount = all.Count(l => l.Level == "Warning"),
                InfoCount = all.Count(l => l.Level == "Information"),
                DebugCount = all.Count(l => l.Level == "Debug"),
                OldestLog = all.Any() ? all.Min(l => l.Timestamp) : (DateTime?)null,
                NewestLog = all.Any() ? all.Max(l => l.Timestamp) : (DateTime?)null
            };
        });
    }

    #endregion
}

/// <summary>
/// Log statistics
/// </summary>
public class LogStatistics
{
    public int TotalLogs { get; set; }
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
    public int InfoCount { get; set; }
    public int DebugCount { get; set; }
    public DateTime? OldestLog { get; set; }
    public DateTime? NewestLog { get; set; }
}
