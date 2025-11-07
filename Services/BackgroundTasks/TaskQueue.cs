using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace AIDebugPro.Services.BackgroundTasks;

/// <summary>
/// Represents a background task to be executed
/// </summary>
public class QueuedTask
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public Func<CancellationToken, Task> WorkItem { get; set; } = null!;
    public DateTime QueuedAt { get; set; } = DateTime.UtcNow;
    public TaskPriority Priority { get; set; } = TaskPriority.Normal;
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; } = 3;
}

/// <summary>
/// Priority levels for background tasks
/// </summary>
public enum TaskPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}

/// <summary>
/// Result of a task execution
/// </summary>
public class TaskResult
{
    public Guid TaskId { get; set; }
    public string TaskName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public Exception? Exception { get; set; }
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Interface for background task queue
/// </summary>
public interface IBackgroundTaskQueue
{
    /// <summary>
    /// Queues a background task for execution
    /// </summary>
    Task QueueBackgroundWorkItemAsync(QueuedTask task);

    /// <summary>
    /// Dequeues the next task to execute
    /// </summary>
    Task<QueuedTask?> DequeueAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets the number of queued tasks
    /// </summary>
    int GetQueuedCount();

    /// <summary>
    /// Gets task statistics
    /// </summary>
    TaskQueueStatistics GetStatistics();
}

/// <summary>
/// Thread-safe background task queue with priority support
/// </summary>
public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<QueuedTask> _queue;
    private readonly ILogger<BackgroundTaskQueue>? _logger;
    private readonly ConcurrentDictionary<TaskPriority, int> _priorityCounters;
    private readonly ConcurrentBag<TaskResult> _completedTasks;
    private readonly BackgroundTaskQueueOptions _options;
    private int _totalQueued;
    private int _totalProcessed;
    private int _totalFailed;

    public BackgroundTaskQueue(
        BackgroundTaskQueueOptions? options = null,
        ILogger<BackgroundTaskQueue>? logger = null)
    {
        _options = options ?? new BackgroundTaskQueueOptions();
        _logger = logger;
        
        _queue = Channel.CreateUnbounded<QueuedTask>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false
        });

        _priorityCounters = new ConcurrentDictionary<TaskPriority, int>();
        _completedTasks = new ConcurrentBag<TaskResult>();

        // Initialize priority counters
        foreach (TaskPriority priority in Enum.GetValues(typeof(TaskPriority)))
        {
            _priorityCounters[priority] = 0;
        }
    }

    /// <summary>
    /// Queues a background task for execution
    /// </summary>
    public async Task QueueBackgroundWorkItemAsync(QueuedTask task)
    {
        if (task == null)
            throw new ArgumentNullException(nameof(task));

        if (task.WorkItem == null)
            throw new ArgumentException("WorkItem cannot be null", nameof(task));

        // Check queue capacity
        if (_options.MaxQueueSize > 0 && GetQueuedCount() >= _options.MaxQueueSize)
        {
            _logger?.LogWarning(
                "Task queue is full ({Count}/{Max}). Rejecting task: {TaskName}",
                GetQueuedCount(),
                _options.MaxQueueSize,
                task.Name);
            
            throw new InvalidOperationException("Task queue is at maximum capacity");
        }

        await _queue.Writer.WriteAsync(task);
        
        Interlocked.Increment(ref _totalQueued);
        _priorityCounters.AddOrUpdate(task.Priority, 1, (_, count) => count + 1);

        _logger?.LogInformation(
            "Queued task {TaskId} ({TaskName}) with priority {Priority}. Queue size: {Count}",
            task.Id,
            task.Name,
            task.Priority,
            GetQueuedCount());
    }

    /// <summary>
    /// Dequeues the next task to execute (with priority consideration)
    /// </summary>
    public async Task<QueuedTask?> DequeueAsync(CancellationToken cancellationToken)
    {
        // Try to read from the queue
        var task = await _queue.Reader.ReadAsync(cancellationToken);

        if (task != null)
        {
            _priorityCounters.AddOrUpdate(task.Priority, 0, (_, count) => Math.Max(0, count - 1));
            
            _logger?.LogDebug(
                "Dequeued task {TaskId} ({TaskName}) with priority {Priority}",
                task.Id,
                task.Name,
                task.Priority);
        }

        return task;
    }

    /// <summary>
    /// Gets the number of queued tasks
    /// </summary>
    public int GetQueuedCount()
    {
        return _queue.Reader.Count;
    }

    /// <summary>
    /// Records task completion
    /// </summary>
    public void RecordTaskCompletion(TaskResult result)
    {
        if (result.Success)
        {
            Interlocked.Increment(ref _totalProcessed);
        }
        else
        {
            Interlocked.Increment(ref _totalFailed);
        }

        // Keep only recent task results to prevent memory growth
        _completedTasks.Add(result);
        
        if (_completedTasks.Count > _options.MaxCompletedTaskHistory)
        {
            // Remove oldest results (approximate - ConcurrentBag doesn't guarantee order)
            var toRemove = _completedTasks.Count - _options.MaxCompletedTaskHistory;
            for (int i = 0; i < toRemove; i++)
            {
                _completedTasks.TryTake(out _);
            }
        }

        _logger?.LogInformation(
            "Task {TaskId} ({TaskName}) completed: {Status} in {Duration}ms",
            result.TaskId,
            result.TaskName,
            result.Success ? "Success" : "Failed",
            result.Duration.TotalMilliseconds);
    }

    /// <summary>
    /// Gets task statistics
    /// </summary>
    public TaskQueueStatistics GetStatistics()
    {
        var recentTasks = _completedTasks.ToArray();
        var last100 = recentTasks.OrderByDescending(t => t.CompletedAt).Take(100).ToList();

        return new TaskQueueStatistics
        {
            QueuedCount = GetQueuedCount(),
            TotalQueued = _totalQueued,
            TotalProcessed = _totalProcessed,
            TotalFailed = _totalFailed,
            SuccessRate = _totalProcessed > 0 
                ? (double)(_totalProcessed - _totalFailed) / _totalProcessed * 100 
                : 0,
            PriorityBreakdown = new Dictionary<TaskPriority, int>(_priorityCounters),
            AverageExecutionTimeMs = last100.Any() 
                ? last100.Average(t => t.Duration.TotalMilliseconds) 
                : 0,
            RecentFailures = last100.Where(t => !t.Success).Take(10).ToList()
        };
    }
}

/// <summary>
/// Options for background task queue
/// </summary>
public class BackgroundTaskQueueOptions
{
    public int MaxQueueSize { get; set; } = 1000;
    public int MaxCompletedTaskHistory { get; set; } = 500;
    public int WorkerCount { get; set; } = 3;
    public bool EnablePriorityProcessing { get; set; } = true;
    public TimeSpan TaskTimeout { get; set; } = TimeSpan.FromMinutes(5);
}

/// <summary>
/// Statistics about the task queue
/// </summary>
public class TaskQueueStatistics
{
    public int QueuedCount { get; set; }
    public int TotalQueued { get; set; }
    public int TotalProcessed { get; set; }
    public int TotalFailed { get; set; }
    public double SuccessRate { get; set; }
    public Dictionary<TaskPriority, int> PriorityBreakdown { get; set; } = new();
    public double AverageExecutionTimeMs { get; set; }
    public List<TaskResult> RecentFailures { get; set; } = new();
}
