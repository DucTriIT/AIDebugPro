namespace AIDebugPro.Services.BackgroundTasks;

/// <summary>
/// Extension methods for background task queue
/// </summary>
public static class BackgroundTaskQueueExtensions
{
    /// <summary>
    /// Queues a simple background work item
    /// </summary>
    public static async Task QueueWorkAsync(
        this IBackgroundTaskQueue queue,
        string taskName,
        Func<CancellationToken, Task> workItem,
        TaskPriority priority = TaskPriority.Normal)
    {
        var task = new QueuedTask
        {
            Name = taskName,
            WorkItem = workItem,
            Priority = priority
        };

        await queue.QueueBackgroundWorkItemAsync(task);
    }

    /// <summary>
    /// Queues a synchronous work item as background task
    /// </summary>
    public static async Task QueueWorkAsync(
        this IBackgroundTaskQueue queue,
        string taskName,
        Action work,
        TaskPriority priority = TaskPriority.Normal)
    {
        var task = new QueuedTask
        {
            Name = taskName,
            WorkItem = cancellationToken =>
            {
                work();
                return Task.CompletedTask;
            },
            Priority = priority
        };

        await queue.QueueBackgroundWorkItemAsync(task);
    }

    /// <summary>
    /// Queues a background work item with retry configuration
    /// </summary>
    public static async Task QueueWorkWithRetryAsync(
        this IBackgroundTaskQueue queue,
        string taskName,
        Func<CancellationToken, Task> workItem,
        int maxRetries = 3,
        TaskPriority priority = TaskPriority.Normal)
    {
        var task = new QueuedTask
        {
            Name = taskName,
            WorkItem = workItem,
            Priority = priority,
            MaxRetries = maxRetries
        };

        await queue.QueueBackgroundWorkItemAsync(task);
    }

    /// <summary>
    /// Queues multiple related tasks
    /// </summary>
    public static async Task QueueBatchAsync(
        this IBackgroundTaskQueue queue,
        string batchName,
        IEnumerable<Func<CancellationToken, Task>> workItems,
        TaskPriority priority = TaskPriority.Normal)
    {
        var tasks = workItems.Select((workItem, index) => new QueuedTask
        {
            Name = $"{batchName} [{index + 1}]",
            WorkItem = workItem,
            Priority = priority
        });

        foreach (var task in tasks)
        {
            await queue.QueueBackgroundWorkItemAsync(task);
        }
    }

    /// <summary>
    /// Queues a delayed task
    /// </summary>
    public static async Task QueueDelayedWorkAsync(
        this IBackgroundTaskQueue queue,
        string taskName,
        Func<CancellationToken, Task> workItem,
        TimeSpan delay,
        TaskPriority priority = TaskPriority.Normal)
    {
        var task = new QueuedTask
        {
            Name = taskName,
            WorkItem = async cancellationToken =>
            {
                await Task.Delay(delay, cancellationToken);
                await workItem(cancellationToken);
            },
            Priority = priority
        };

        await queue.QueueBackgroundWorkItemAsync(task);
    }

    /// <summary>
    /// Queues a periodic task
    /// </summary>
    public static async Task QueuePeriodicWorkAsync(
        this IBackgroundTaskQueue queue,
        string taskName,
        Func<CancellationToken, Task> workItem,
        TimeSpan interval,
        int maxExecutions = int.MaxValue,
        TaskPriority priority = TaskPriority.Normal)
    {
        var executionCount = 0;

        var task = new QueuedTask
        {
            Name = taskName,
            WorkItem = async cancellationToken =>
            {
                while (executionCount < maxExecutions && !cancellationToken.IsCancellationRequested)
                {
                    await workItem(cancellationToken);
                    executionCount++;

                    if (executionCount < maxExecutions)
                    {
                        await Task.Delay(interval, cancellationToken);
                    }
                }
            },
            Priority = priority
        };

        await queue.QueueBackgroundWorkItemAsync(task);
    }
}
