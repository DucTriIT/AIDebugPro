namespace AIDebugPro.Services.Utilities;

/// <summary>
/// Provides system date/time for testability
/// </summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
    DateTime Now { get; }
    DateTimeOffset UtcNowOffset { get; }
}

/// <summary>
/// Default implementation of IDateTimeProvider
/// </summary>
public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateTime Now => DateTime.Now;
    public DateTimeOffset UtcNowOffset => DateTimeOffset.UtcNow;
}

/// <summary>
/// Retry policy helper
/// </summary>
public class RetryPolicy
{
    private readonly int _maxRetries;
    private readonly TimeSpan _delay;
    private readonly TimeSpan _maxDelay;
    private readonly bool _exponentialBackoff;

    public RetryPolicy(
        int maxRetries = 3,
        TimeSpan? delay = null,
        TimeSpan? maxDelay = null,
        bool exponentialBackoff = true)
    {
        _maxRetries = maxRetries;
        _delay = delay ?? TimeSpan.FromSeconds(1);
        _maxDelay = maxDelay ?? TimeSpan.FromMinutes(1);
        _exponentialBackoff = exponentialBackoff;
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
    {
        var retries = 0;

        while (true)
        {
            try
            {
                return await action();
            }
            catch when (retries < _maxRetries)
            {
                retries++;
                var delayTime = CalculateDelay(retries);
                await Task.Delay(delayTime);
            }
        }
    }

    public async Task ExecuteAsync(Func<Task> action)
    {
        var retries = 0;

        while (true)
        {
            try
            {
                await action();
                return;
            }
            catch when (retries < _maxRetries)
            {
                retries++;
                var delayTime = CalculateDelay(retries);
                await Task.Delay(delayTime);
            }
        }
    }

    private TimeSpan CalculateDelay(int retryCount)
    {
        if (!_exponentialBackoff)
            return _delay;

        var delay = TimeSpan.FromMilliseconds(
            _delay.TotalMilliseconds * Math.Pow(2, retryCount - 1));

        return delay > _maxDelay ? _maxDelay : delay;
    }
}

/// <summary>
/// Stopwatch wrapper for easier timing
/// </summary>
public class TimingHelper : IDisposable
{
    private readonly System.Diagnostics.Stopwatch _stopwatch;
    private readonly Action<TimeSpan>? _onComplete;

    private TimingHelper(Action<TimeSpan>? onComplete = null)
    {
        _stopwatch = System.Diagnostics.Stopwatch.StartNew();
        _onComplete = onComplete;
    }

    public static TimingHelper Start(Action<TimeSpan>? onComplete = null)
    {
        return new TimingHelper(onComplete);
    }

    public TimeSpan Elapsed => _stopwatch.Elapsed;

    public void Stop()
    {
        _stopwatch.Stop();
        _onComplete?.Invoke(_stopwatch.Elapsed);
    }

    public void Dispose()
    {
        Stop();
    }
}

/// <summary>
/// Batch processor for processing items in batches
/// </summary>
public class BatchProcessor<T>
{
    private readonly int _batchSize;
    private readonly Func<IEnumerable<T>, Task> _processBatch;

    public BatchProcessor(int batchSize, Func<IEnumerable<T>, Task> processBatch)
    {
        _batchSize = batchSize;
        _processBatch = processBatch ?? throw new ArgumentNullException(nameof(processBatch));
    }

    public async Task ProcessAsync(IEnumerable<T> items)
    {
        var batch = new List<T>(_batchSize);

        foreach (var item in items)
        {
            batch.Add(item);

            if (batch.Count >= _batchSize)
            {
                await _processBatch(batch);
                batch.Clear();
            }
        }

        // Process remaining items
        if (batch.Count > 0)
        {
            await _processBatch(batch);
        }
    }
}

/// <summary>
/// Throttle helper to limit operation frequency
/// </summary>
public class Throttle
{
    private readonly TimeSpan _interval;
    private DateTime _lastExecution = DateTime.MinValue;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public Throttle(TimeSpan interval)
    {
        _interval = interval;
    }

    public async Task<bool> TryExecuteAsync(Func<Task> action)
    {
        await _semaphore.WaitAsync();
        try
        {
            var now = DateTime.UtcNow;
            var timeSinceLastExecution = now - _lastExecution;

            if (timeSinceLastExecution < _interval)
            {
                return false; // Too soon
            }

            await action();
            _lastExecution = now;
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task ExecuteAsync(Func<Task> action)
    {
        await _semaphore.WaitAsync();
        try
        {
            var now = DateTime.UtcNow;
            var timeSinceLastExecution = now - _lastExecution;

            if (timeSinceLastExecution < _interval)
            {
                var delay = _interval - timeSinceLastExecution;
                await Task.Delay(delay);
            }

            await action();
            _lastExecution = DateTime.UtcNow;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

/// <summary>
/// Debounce helper to delay execution until quiet period
/// </summary>
public class Debouncer
{
    private readonly TimeSpan _delay;
    private CancellationTokenSource? _cts;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public Debouncer(TimeSpan delay)
    {
        _delay = delay;
    }

    public async Task DebounceAsync(Func<Task> action)
    {
        await _semaphore.WaitAsync();
        try
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            await Task.Delay(_delay, token);

            if (!token.IsCancellationRequested)
            {
                await action();
            }
        }
        catch (TaskCanceledException)
        {
            // Expected when debounced
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

/// <summary>
/// Circuit breaker pattern implementation
/// </summary>
public class CircuitBreaker
{
    private readonly int _failureThreshold;
    private readonly TimeSpan _timeout;
    private int _failureCount;
    private DateTime _lastFailureTime;
    private CircuitState _state = CircuitState.Closed;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public CircuitBreaker(int failureThreshold = 5, TimeSpan? timeout = null)
    {
        _failureThreshold = failureThreshold;
        _timeout = timeout ?? TimeSpan.FromMinutes(1);
    }

    public CircuitState State => _state;

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_state == CircuitState.Open)
            {
                if (DateTime.UtcNow - _lastFailureTime > _timeout)
                {
                    _state = CircuitState.HalfOpen;
                }
                else
                {
                    throw new InvalidOperationException("Circuit breaker is open");
                }
            }

            try
            {
                var result = await action();

                if (_state == CircuitState.HalfOpen)
                {
                    _state = CircuitState.Closed;
                    _failureCount = 0;
                }

                return result;
            }
            catch
            {
                _failureCount++;
                _lastFailureTime = DateTime.UtcNow;

                if (_failureCount >= _failureThreshold)
                {
                    _state = CircuitState.Open;
                }

                throw;
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

public enum CircuitState
{
    Closed,   // Normal operation
    Open,     // Failing, blocking requests
    HalfOpen  // Testing if service recovered
}

/// <summary>
/// Memory cache with expiration
/// </summary>
public class MemoryCache<TKey, TValue> where TKey : notnull
{
    private readonly Dictionary<TKey, CacheEntry<TValue>> _cache = new();
    private readonly TimeSpan _defaultExpiration;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public MemoryCache(TimeSpan? defaultExpiration = null)
    {
        _defaultExpiration = defaultExpiration ?? TimeSpan.FromMinutes(5);
    }

    public async Task<TValue?> GetAsync(TKey key)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_cache.TryGetValue(key, out var entry))
            {
                if (entry.ExpiresAt > DateTime.UtcNow)
                {
                    return entry.Value;
                }

                _cache.Remove(key);
            }

            return default;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task SetAsync(TKey key, TValue value, TimeSpan? expiration = null)
    {
        await _semaphore.WaitAsync();
        try
        {
            var entry = new CacheEntry<TValue>
            {
                Value = value,
                ExpiresAt = DateTime.UtcNow + (expiration ?? _defaultExpiration)
            };

            _cache[key] = entry;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> RemoveAsync(TKey key)
    {
        await _semaphore.WaitAsync();
        try
        {
            return _cache.Remove(key);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task ClearAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            _cache.Clear();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task CleanupExpiredAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            var now = DateTime.UtcNow;
            var expiredKeys = _cache
                .Where(kvp => kvp.Value.ExpiresAt <= now)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _cache.Remove(key);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private class CacheEntry<T>
    {
        public T Value { get; set; } = default!;
        public DateTime ExpiresAt { get; set; }
    }
}

/// <summary>
/// Event aggregator for loosely coupled event handling
/// </summary>
public class EventAggregator
{
    private readonly Dictionary<Type, List<Delegate>> _subscriptions = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task PublishAsync<TEvent>(TEvent eventData)
    {
        await _semaphore.WaitAsync();
        List<Delegate> handlers;
        
        try
        {
            if (!_subscriptions.TryGetValue(typeof(TEvent), out var list))
                return;

            handlers = list.ToList();
        }
        finally
        {
            _semaphore.Release();
        }

        foreach (var handler in handlers)
        {
            if (handler is Func<TEvent, Task> asyncHandler)
            {
                await asyncHandler(eventData);
            }
            else if (handler is Action<TEvent> syncHandler)
            {
                syncHandler(eventData);
            }
        }
    }

    public async Task SubscribeAsync<TEvent>(Func<TEvent, Task> handler)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (!_subscriptions.TryGetValue(typeof(TEvent), out var handlers))
            {
                handlers = new List<Delegate>();
                _subscriptions[typeof(TEvent)] = handlers;
            }

            handlers.Add(handler);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task SubscribeAsync<TEvent>(Action<TEvent> handler)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (!_subscriptions.TryGetValue(typeof(TEvent), out var handlers))
            {
                handlers = new List<Delegate>();
                _subscriptions[typeof(TEvent)] = handlers;
            }

            handlers.Add(handler);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
