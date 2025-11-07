using System.Text;
using System.Text.RegularExpressions;

namespace AIDebugPro.Services.Utilities;

/// <summary>
/// Extension methods for string operations
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Truncates a string to a maximum length
    /// </summary>
    public static string Truncate(this string value, int maxLength, string suffix = "...")
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;

        return value.Substring(0, maxLength - suffix.Length) + suffix;
    }

    /// <summary>
    /// Checks if a string is null or whitespace
    /// </summary>
    public static bool IsNullOrWhiteSpace(this string? value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Converts a string to camelCase
    /// </summary>
    public static string ToCamelCase(this string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return char.ToLowerInvariant(value[0]) + value.Substring(1);
    }

    /// <summary>
    /// Converts a string to PascalCase
    /// </summary>
    public static string ToPascalCase(this string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return char.ToUpperInvariant(value[0]) + value.Substring(1);
    }

    /// <summary>
    /// Converts a string to snake_case
    /// </summary>
    public static string ToSnakeCase(this string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return Regex.Replace(value, "([a-z])([A-Z])", "$1_$2").ToLower();
    }

    /// <summary>
    /// Removes HTML tags from a string
    /// </summary>
    public static string StripHtml(this string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return Regex.Replace(value, "<.*?>", string.Empty);
    }

    /// <summary>
    /// Converts a string to a slug (URL-friendly format)
    /// </summary>
    public static string ToSlug(this string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        value = value.ToLowerInvariant();
        value = Regex.Replace(value, @"[^a-z0-9\s-]", "");
        value = Regex.Replace(value, @"\s+", "-");
        value = Regex.Replace(value, @"-+", "-");
        return value.Trim('-');
    }

    /// <summary>
    /// Converts a string to Base64
    /// </summary>
    public static string ToBase64(this string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var bytes = Encoding.UTF8.GetBytes(value);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Decodes a Base64 string
    /// </summary>
    public static string FromBase64(this string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var bytes = Convert.FromBase64String(value);
        return Encoding.UTF8.GetString(bytes);
    }

    /// <summary>
    /// Masks a string (useful for API keys, passwords)
    /// </summary>
    public static string Mask(this string value, int visibleChars = 4, char maskChar = '*')
    {
        if (string.IsNullOrEmpty(value) || value.Length <= visibleChars)
            return new string(maskChar, value?.Length ?? 0);

        var visible = value.Substring(0, visibleChars);
        var masked = new string(maskChar, value.Length - visibleChars);
        return visible + masked;
    }

    /// <summary>
    /// Counts the number of words in a string
    /// </summary>
    public static int WordCount(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;

        return value.Split(new[] { ' ', '\t', '\n', '\r' }, 
            StringSplitOptions.RemoveEmptyEntries).Length;
    }

    /// <summary>
    /// Reverses a string
    /// </summary>
    public static string Reverse(this string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var chars = value.ToCharArray();
        Array.Reverse(chars);
        return new string(chars);
    }

    /// <summary>
    /// Checks if a string contains any of the specified values
    /// </summary>
    public static bool ContainsAny(this string value, params string[] values)
    {
        return values.Any(v => value.Contains(v));
    }

    /// <summary>
    /// Checks if a string contains all of the specified values
    /// </summary>
    public static bool ContainsAll(this string value, params string[] values)
    {
        return values.All(v => value.Contains(v));
    }
}

/// <summary>
/// Extension methods for DateTime operations
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Converts DateTime to Unix timestamp (seconds)
    /// </summary>
    public static long ToUnixTimestamp(this DateTime dateTime)
    {
        return ((DateTimeOffset)dateTime).ToUnixTimeSeconds();
    }

    /// <summary>
    /// Converts DateTime to Unix timestamp (milliseconds)
    /// </summary>
    public static long ToUnixTimestampMs(this DateTime dateTime)
    {
        return ((DateTimeOffset)dateTime).ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// Gets a relative time string (e.g., "2 hours ago")
    /// </summary>
    public static string ToRelativeTime(this DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime.ToUniversalTime();

        if (timeSpan.TotalSeconds < 60)
            return $"{(int)timeSpan.TotalSeconds} seconds ago";

        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes} minutes ago";

        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours} hours ago";

        if (timeSpan.TotalDays < 7)
            return $"{(int)timeSpan.TotalDays} days ago";

        if (timeSpan.TotalDays < 30)
            return $"{(int)(timeSpan.TotalDays / 7)} weeks ago";

        if (timeSpan.TotalDays < 365)
            return $"{(int)(timeSpan.TotalDays / 30)} months ago";

        return $"{(int)(timeSpan.TotalDays / 365)} years ago";
    }

    /// <summary>
    /// Checks if a DateTime is in the past
    /// </summary>
    public static bool IsInPast(this DateTime dateTime)
    {
        return dateTime < DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if a DateTime is in the future
    /// </summary>
    public static bool IsInFuture(this DateTime dateTime)
    {
        return dateTime > DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the start of the day
    /// </summary>
    public static DateTime StartOfDay(this DateTime dateTime)
    {
        return dateTime.Date;
    }

    /// <summary>
    /// Gets the end of the day
    /// </summary>
    public static DateTime EndOfDay(this DateTime dateTime)
    {
        return dateTime.Date.AddDays(1).AddTicks(-1);
    }

    /// <summary>
    /// Gets the start of the week (Monday)
    /// </summary>
    public static DateTime StartOfWeek(this DateTime dateTime)
    {
        var diff = (7 + (dateTime.DayOfWeek - DayOfWeek.Monday)) % 7;
        return dateTime.AddDays(-diff).Date;
    }

    /// <summary>
    /// Gets the start of the month
    /// </summary>
    public static DateTime StartOfMonth(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, 1);
    }

    /// <summary>
    /// Formats DateTime as ISO 8601 string
    /// </summary>
    public static string ToIso8601(this DateTime dateTime)
    {
        return dateTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
    }
}

/// <summary>
/// Extension methods for TimeSpan operations
/// </summary>
public static class TimeSpanExtensions
{
    /// <summary>
    /// Converts TimeSpan to human-readable string
    /// </summary>
    public static string ToHumanReadable(this TimeSpan timeSpan)
    {
        if (timeSpan.TotalSeconds < 1)
            return $"{timeSpan.TotalMilliseconds:F0}ms";

        if (timeSpan.TotalMinutes < 1)
            return $"{timeSpan.TotalSeconds:F1}s";

        if (timeSpan.TotalHours < 1)
            return $"{timeSpan.TotalMinutes:F1}m";

        if (timeSpan.TotalDays < 1)
            return $"{timeSpan.TotalHours:F1}h";

        return $"{timeSpan.TotalDays:F1}d";
    }

    /// <summary>
    /// Converts TimeSpan to detailed string
    /// </summary>
    public static string ToDetailedString(this TimeSpan timeSpan)
    {
        var parts = new List<string>();

        if (timeSpan.Days > 0)
            parts.Add($"{timeSpan.Days} day{(timeSpan.Days > 1 ? "s" : "")}");

        if (timeSpan.Hours > 0)
            parts.Add($"{timeSpan.Hours} hour{(timeSpan.Hours > 1 ? "s" : "")}");

        if (timeSpan.Minutes > 0)
            parts.Add($"{timeSpan.Minutes} minute{(timeSpan.Minutes > 1 ? "s" : "")}");

        if (timeSpan.Seconds > 0 || parts.Count == 0)
            parts.Add($"{timeSpan.Seconds} second{(timeSpan.Seconds > 1 ? "s" : "")}");

        return string.Join(", ", parts);
    }
}

/// <summary>
/// Extension methods for collection operations
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Checks if a collection is null or empty
    /// </summary>
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? collection)
    {
        return collection == null || !collection.Any();
    }

    /// <summary>
    /// Performs an action on each element in a collection
    /// </summary>
    public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
    {
        foreach (var item in collection)
        {
            action(item);
        }
    }

    /// <summary>
    /// Splits a collection into batches of specified size
    /// </summary>
    public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> collection, int batchSize)
    {
        var batch = new List<T>(batchSize);

        foreach (var item in collection)
        {
            batch.Add(item);

            if (batch.Count == batchSize)
            {
                yield return batch;
                batch = new List<T>(batchSize);
            }
        }

        if (batch.Count > 0)
            yield return batch;
    }

    /// <summary>
    /// Gets distinct items by a key selector
    /// </summary>
    public static IEnumerable<T> DistinctBy<T, TKey>(
        this IEnumerable<T> collection,
        Func<T, TKey> keySelector)
    {
        var seenKeys = new HashSet<TKey>();

        foreach (var item in collection)
        {
            var key = keySelector(item);
            if (seenKeys.Add(key))
            {
                yield return item;
            }
        }
    }

    /// <summary>
    /// Converts a collection to a dictionary safely
    /// </summary>
    public static Dictionary<TKey, TValue> SafeToDictionary<TSource, TKey, TValue>(
        this IEnumerable<TSource> collection,
        Func<TSource, TKey> keySelector,
        Func<TSource, TValue> valueSelector) where TKey : notnull
    {
        var dictionary = new Dictionary<TKey, TValue>();

        foreach (var item in collection)
        {
            var key = keySelector(item);
            if (!dictionary.ContainsKey(key))
            {
                dictionary[key] = valueSelector(item);
            }
        }

        return dictionary;
    }

    /// <summary>
    /// Shuffles a collection randomly
    /// </summary>
    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> collection)
    {
        var list = collection.ToList();
        var rng = new Random();

        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }

        return list;
    }
}

/// <summary>
/// Extension methods for numeric operations
/// </summary>
public static class NumericExtensions
{
    /// <summary>
    /// Clamps a value between min and max
    /// </summary>
    public static T Clamp<T>(this T value, T min, T max) where T : IComparable<T>
    {
        if (value.CompareTo(min) < 0) return min;
        if (value.CompareTo(max) > 0) return max;
        return value;
    }

    /// <summary>
    /// Converts bytes to human-readable size
    /// </summary>
    public static string ToFileSize(this long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    /// <summary>
    /// Converts percentage to string with % symbol
    /// </summary>
    public static string ToPercentage(this double value, int decimals = 2)
    {
        return $"{value.ToString($"F{decimals}")}%";
    }

    /// <summary>
    /// Checks if a number is between two values (inclusive)
    /// </summary>
    public static bool IsBetween<T>(this T value, T min, T max) where T : IComparable<T>
    {
        return value.CompareTo(min) >= 0 && value.CompareTo(max) <= 0;
    }

    /// <summary>
    /// Rounds to the nearest multiple
    /// </summary>
    public static int RoundToNearest(this int value, int multiple)
    {
        return (int)Math.Round((double)value / multiple) * multiple;
    }
}

/// <summary>
/// Extension methods for Task operations
/// </summary>
public static class TaskExtensions
{
    /// <summary>
    /// Executes a task with timeout
    /// </summary>
    public static async Task<T> WithTimeout<T>(this Task<T> task, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource();
        var delayTask = Task.Delay(timeout, cts.Token);
        var completedTask = await Task.WhenAny(task, delayTask);

        if (completedTask == delayTask)
        {
            throw new TimeoutException($"Operation timed out after {timeout.TotalSeconds} seconds");
        }

        cts.Cancel();
        return await task;
    }

    /// <summary>
    /// Executes a task with retry logic
    /// </summary>
    public static async Task<T> WithRetry<T>(
        this Func<Task<T>> taskFactory,
        int maxRetries = 3,
        TimeSpan? delay = null)
    {
        delay ??= TimeSpan.FromSeconds(1);
        var retries = 0;

        while (true)
        {
            try
            {
                return await taskFactory();
            }
            catch when (retries < maxRetries)
            {
                retries++;
                await Task.Delay(delay.Value * retries);
            }
        }
    }

    /// <summary>
    /// Safely ignores exceptions from a task
    /// </summary>
    public static async Task IgnoreExceptions(this Task task)
    {
        try
        {
            await task;
        }
        catch
        {
            // Intentionally ignored
        }
    }
}

/// <summary>
/// Extension methods for Exception operations
/// </summary>
public static class ExceptionExtensions
{
    /// <summary>
    /// Gets the full exception message including inner exceptions
    /// </summary>
    public static string GetFullMessage(this Exception exception)
    {
        var messages = new List<string>();
        var current = exception;

        while (current != null)
        {
            messages.Add(current.Message);
            current = current.InnerException;
        }

        return string.Join(" --> ", messages);
    }

    /// <summary>
    /// Gets all inner exceptions
    /// </summary>
    public static IEnumerable<Exception> GetAllInnerExceptions(this Exception exception)
    {
        var current = exception;

        while (current != null)
        {
            yield return current;
            current = current.InnerException;
        }
    }
}
