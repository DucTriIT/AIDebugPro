using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using AIDebugPro.Core.Constants;

namespace AIDebugPro.AIIntegration;

/// <summary>
/// Manages token counting and response caching
/// </summary>
public class TokenManager
{
    private readonly ILogger<TokenManager>? _logger;
    private readonly ConcurrentDictionary<string, CachedResponse> _cache;
    private readonly TokenManagerOptions _options;

    public TokenManager(TokenManagerOptions? options = null, ILogger<TokenManager>? logger = null)
    {
        _options = options ?? new TokenManagerOptions();
        _logger = logger;
        _cache = new ConcurrentDictionary<string, CachedResponse>();
    }

    #region Token Counting

    /// <summary>
    /// Estimates token count for text (GPT tokenization approximation)
    /// </summary>
    public int EstimateTokenCount(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        // Rough approximation: ~4 characters per token
        // More accurate: ~0.75 words per token
        var charCount = text.Length;
        var wordCount = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;

        // Use average of both methods
        var charEstimate = (int)Math.Ceiling(charCount / 4.0);
        var wordEstimate = (int)Math.Ceiling(wordCount / 0.75);

        return (charEstimate + wordEstimate) / 2;
    }

    /// <summary>
    /// Checks if text fits within token limit
    /// </summary>
    public bool FitsWithinLimit(string text, int maxTokens)
    {
        return EstimateTokenCount(text) <= maxTokens;
    }

    /// <summary>
    /// Truncates text to fit token limit
    /// </summary>
    public string TruncateToTokenLimit(string text, int maxTokens, string suffix = "...")
    {
        var estimatedTokens = EstimateTokenCount(text);
        
        if (estimatedTokens <= maxTokens)
            return text;

        // Calculate approximate character limit
        var ratio = (double)maxTokens / estimatedTokens;
        var targetLength = (int)(text.Length * ratio);
        
        // Account for suffix
        targetLength -= suffix.Length * 4; // Approximate token count for suffix

        if (targetLength <= 0)
            return suffix;

        return text.Substring(0, targetLength) + suffix;
    }

    /// <summary>
    /// Gets token budget breakdown for different models
    /// </summary>
    public TokenBudget GetTokenBudget(string model)
    {
        var maxTokens = model switch
        {
            "gpt-4" => AIConstants.MaxTokensGPT4,
            "gpt-4-32k" => 32768,
            "gpt-4-turbo-preview" => AIConstants.MaxTokensGPT4Turbo,
            "gpt-3.5-turbo" => 4096,
            "gpt-3.5-turbo-16k" => AIConstants.MaxTokensGPT35Turbo,
            _ => 4096
        };

        return new TokenBudget
        {
            Model = model,
            MaxTotalTokens = maxTokens,
            MaxPromptTokens = (int)(maxTokens * 0.75), // Reserve 25% for response
            MaxResponseTokens = (int)(maxTokens * 0.25),
            RecommendedPromptTokens = (int)(maxTokens * 0.60) // Leave buffer
        };
    }

    #endregion

    #region Caching

    /// <summary>
    /// Gets cached response if available
    /// </summary>
    public string? GetCachedResponse(string promptHash)
    {
        if (!_options.EnableCaching)
            return null;

        if (_cache.TryGetValue(promptHash, out var cached))
        {
            if (DateTime.UtcNow - cached.Timestamp < _options.CacheDuration)
            {
                cached.HitCount++;
                _logger?.LogDebug("Cache hit for prompt hash {Hash}", promptHash);
                return cached.Response;
            }

            // Remove expired entry
            _cache.TryRemove(promptHash, out _);
        }

        return null;
    }

    /// <summary>
    /// Caches a response
    /// </summary>
    public void CacheResponse(string promptHash, string response)
    {
        if (!_options.EnableCaching)
            return;

        var cached = new CachedResponse
        {
            PromptHash = promptHash,
            Response = response,
            Timestamp = DateTime.UtcNow,
            HitCount = 0
        };

        _cache[promptHash] = cached;

        // Cleanup if cache is too large
        if (_cache.Count > _options.MaxCacheSize)
        {
            CleanupCache();
        }

        _logger?.LogDebug("Cached response for prompt hash {Hash}", promptHash);
    }

    /// <summary>
    /// Generates hash for prompt
    /// </summary>
    public string GeneratePromptHash(string prompt)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(prompt);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Clears the cache
    /// </summary>
    public void ClearCache()
    {
        _cache.Clear();
        _logger?.LogInformation("Cache cleared");
    }

    /// <summary>
    /// Gets cache statistics
    /// </summary>
    public CacheStatistics GetCacheStatistics()
    {
        var now = DateTime.UtcNow;
        var entries = _cache.Values.ToList();

        return new CacheStatistics
        {
            TotalEntries = entries.Count,
            TotalHits = entries.Sum(e => e.HitCount),
            OldestEntry = entries.Any() ? entries.Min(e => e.Timestamp) : (DateTime?)null,
            NewestEntry = entries.Any() ? entries.Max(e => e.Timestamp) : (DateTime?)null,
            ExpiredEntries = entries.Count(e => now - e.Timestamp >= _options.CacheDuration),
            AverageHitsPerEntry = entries.Any() ? entries.Average(e => e.HitCount) : 0
        };
    }

    #endregion

    #region Private Methods

    private void CleanupCache()
    {
        var now = DateTime.UtcNow;
        var toRemove = new List<string>();

        // Remove expired entries
        foreach (var kvp in _cache)
        {
            if (now - kvp.Value.Timestamp >= _options.CacheDuration)
            {
                toRemove.Add(kvp.Key);
            }
        }

        // If still too many, remove least used
        if (_cache.Count - toRemove.Count > _options.MaxCacheSize)
        {
            var leastUsed = _cache
                .Where(kvp => !toRemove.Contains(kvp.Key))
                .OrderBy(kvp => kvp.Value.HitCount)
                .ThenBy(kvp => kvp.Value.Timestamp)
                .Take(_cache.Count - _options.MaxCacheSize)
                .Select(kvp => kvp.Key);

            toRemove.AddRange(leastUsed);
        }

        foreach (var key in toRemove)
        {
            _cache.TryRemove(key, out _);
        }

        _logger?.LogDebug("Cleaned up {Count} cache entries", toRemove.Count);
    }

    #endregion
}

#region Supporting Classes

/// <summary>
/// Cached AI response
/// </summary>
internal class CachedResponse
{
    public string PromptHash { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int HitCount { get; set; }
}

/// <summary>
/// Token budget for a model
/// </summary>
public class TokenBudget
{
    public string Model { get; set; } = string.Empty;
    public int MaxTotalTokens { get; set; }
    public int MaxPromptTokens { get; set; }
    public int MaxResponseTokens { get; set; }
    public int RecommendedPromptTokens { get; set; }
}

/// <summary>
/// Cache statistics
/// </summary>
public class CacheStatistics
{
    public int TotalEntries { get; set; }
    public int TotalHits { get; set; }
    public DateTime? OldestEntry { get; set; }
    public DateTime? NewestEntry { get; set; }
    public int ExpiredEntries { get; set; }
    public double AverageHitsPerEntry { get; set; }
}

/// <summary>
/// Options for token manager
/// </summary>
public class TokenManagerOptions
{
    public bool EnableCaching { get; set; } = true;
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(30);
    public int MaxCacheSize { get; set; } = 100;
}

#endregion
