using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AIDebugPro.Services.BackgroundTasks;

/// <summary>
/// Background service that processes queued tasks
/// </summary>
public class BackgroundTaskService : BackgroundService
{
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly ILogger<BackgroundTaskService> _logger;
    private readonly BackgroundTaskQueueOptions _options;
    private readonly SemaphoreSlim _workerSemaphore;

    public BackgroundTaskService(
        IBackgroundTaskQueue taskQueue,
        BackgroundTaskQueueOptions options,
        ILogger<BackgroundTaskService> logger)
    {
        _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options;
        _workerSemaphore = new SemaphoreSlim(_options.WorkerCount, _options.WorkerCount);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Background Task Service started with {WorkerCount} workers",
            _options.WorkerCount);

        await Task.Yield(); // Ensure async execution

        var tasks = new List<Task>();

        // Start multiple worker tasks
        for (int i = 0; i < _options.WorkerCount; i++)
        {
            var workerId = i + 1;
            tasks.Add(ProcessTasksAsync(workerId, stoppingToken));
        }

        await Task.WhenAll(tasks);

        _logger.LogInformation("Background Task Service stopped");
    }

    private async Task ProcessTasksAsync(int workerId, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker {WorkerId} started", workerId);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Wait for a task to be available
                var queuedTask = await _taskQueue.DequeueAsync(stoppingToken);

                if (queuedTask == null)
                    continue;

                await _workerSemaphore.WaitAsync(stoppingToken);

                try
                {
                    await ExecuteTaskAsync(queuedTask, workerId, stoppingToken);
                }
                finally
                {
                    _workerSemaphore.Release();
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worker {WorkerId} encountered an error", workerId);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("Worker {WorkerId} stopped", workerId);
    }

    private async Task ExecuteTaskAsync(
        QueuedTask queuedTask,
        int workerId,
        CancellationToken stoppingToken)
    {
        var startTime = DateTime.UtcNow;
        var success = false;
        Exception? exception = null;

        _logger.LogInformation(
            "Worker {WorkerId} executing task {TaskId} ({TaskName})",
            workerId,
            queuedTask.Id,
            queuedTask.Name);

        try
        {
            // Create a timeout cancellation token
            using var timeoutCts = new CancellationTokenSource(_options.TaskTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                stoppingToken,
                timeoutCts.Token);

            // Execute the task
            await queuedTask.WorkItem(linkedCts.Token);

            success = true;

            _logger.LogInformation(
                "Worker {WorkerId} completed task {TaskId} ({TaskName}) successfully",
                workerId,
                queuedTask.Id,
                queuedTask.Name);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Worker {WorkerId} task {TaskId} ({TaskName}) cancelled due to shutdown",
                workerId,
                queuedTask.Id,
                queuedTask.Name);
            
            exception = new OperationCanceledException("Task cancelled during shutdown");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "Worker {WorkerId} task {TaskId} ({TaskName}) timed out after {Timeout}",
                workerId,
                queuedTask.Id,
                queuedTask.Name,
                _options.TaskTimeout);
            
            exception = new TimeoutException($"Task timed out after {_options.TaskTimeout}");
        }
        catch (Exception ex)
        {
            exception = ex;

            _logger.LogError(
                ex,
                "Worker {WorkerId} task {TaskId} ({TaskName}) failed with exception",
                workerId,
                queuedTask.Id,
                queuedTask.Name);

            // Retry logic
            if (queuedTask.RetryCount < queuedTask.MaxRetries)
            {
                queuedTask.RetryCount++;
                
                _logger.LogInformation(
                    "Retrying task {TaskId} ({TaskName}). Attempt {Retry}/{MaxRetries}",
                    queuedTask.Id,
                    queuedTask.Name,
                    queuedTask.RetryCount,
                    queuedTask.MaxRetries);

                // Re-queue the task
                await _taskQueue.QueueBackgroundWorkItemAsync(queuedTask);
                return; // Don't record as completed yet
            }
            else
            {
                _logger.LogError(
                    "Task {TaskId} ({TaskName}) failed after {MaxRetries} retries",
                    queuedTask.Id,
                    queuedTask.Name,
                    queuedTask.MaxRetries);
            }
        }

        // Record task completion
        var result = new TaskResult
        {
            TaskId = queuedTask.Id,
            TaskName = queuedTask.Name,
            Success = success,
            Exception = exception,
            CompletedAt = DateTime.UtcNow,
            Duration = DateTime.UtcNow - startTime
        };

        if (_taskQueue is BackgroundTaskQueue queue)
        {
            queue.RecordTaskCompletion(result);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Background Task Service is stopping...");
        
        await base.StopAsync(cancellationToken);
        
        // Log final statistics
        var stats = _taskQueue.GetStatistics();
        _logger.LogInformation(
            "Final Statistics - Queued: {Queued}, Processed: {Processed}, Failed: {Failed}, Success Rate: {SuccessRate:F2}%",
            stats.TotalQueued,
            stats.TotalProcessed,
            stats.TotalFailed,
            stats.SuccessRate);
    }
}
