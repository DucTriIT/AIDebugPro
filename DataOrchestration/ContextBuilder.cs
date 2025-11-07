using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AIDebugPro.Core.Constants;
using AIDebugPro.Core.Interfaces;
using AIDebugPro.Core.Models;
using Microsoft.Extensions.Logging;

namespace AIDebugPro.DataOrchestration;

/// <summary>
/// Builds AI analysis context from telemetry data
/// </summary>
public class ContextBuilder : IContextBuilder
{
    private readonly List<ContextItem> _contextItems = new();
    private readonly PromptConfiguration _configuration;
    private readonly ILogger<ContextBuilder>? _logger;

    public ContextBuilder(PromptConfiguration? configuration = null, ILogger<ContextBuilder>? logger = null)
    {
        _configuration = configuration ?? new PromptConfiguration();
        _logger = logger;
    }

    #region IContextBuilder Implementation

    /// <summary>
    /// Builds AI prompt context from a telemetry snapshot
    /// </summary>
    public Task<string> BuildPromptContextAsync(TelemetrySnapshot snapshot, AIAnalysisOptions options)
    {
        if (snapshot == null)
            throw new ArgumentNullException(nameof(snapshot));
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        var prompt = new StringBuilder();

        // Add system instructions
        prompt.AppendLine("# AI Debugging Assistant");
        prompt.AppendLine();
        prompt.AppendLine(AIConstants.SystemPromptPrefix);
        prompt.AppendLine();

        // Add analysis instructions based on options
        prompt.AppendLine("## Analysis Instructions");
        if (options.AnalyzeErrors)
            prompt.AppendLine("- Analyze console errors and provide root cause analysis");
        if (options.AnalyzePerformance)
            prompt.AppendLine("- Evaluate performance metrics and identify bottlenecks");
        if (options.AnalyzeNetworkIssues)
            prompt.AppendLine("- Review network requests for failures and slow responses");
        if (options.ProvideFixes)
            prompt.AppendLine("- Provide actionable fixes and code suggestions");
        prompt.AppendLine();

        // Add telemetry context
        prompt.AppendLine("## Telemetry Data");
        prompt.AppendLine();

        // Console messages section
        if (snapshot.ConsoleMessages.Any())
        {
            prompt.AppendLine("### Console Messages");
            var errors = snapshot.ConsoleMessages.Where(m => m.Level == ConsoleMessageLevel.Error).ToList();
            var warnings = snapshot.ConsoleMessages.Where(m => m.Level == ConsoleMessageLevel.Warning).ToList();

            if (errors.Any())
            {
                prompt.AppendLine($"**Errors ({errors.Count}):**");
                foreach (var error in errors.Take(10)) // Limit to prevent token overflow
                {
                    prompt.AppendLine($"- [{error.Timestamp:HH:mm:ss}] {error.Message}");
                    if (!string.IsNullOrEmpty(error.Source))
                        prompt.AppendLine($"  Source: {error.Source}:{error.LineNumber}");
                    if (!string.IsNullOrEmpty(error.StackTrace))
                        prompt.AppendLine($"  Stack: {error.StackTrace.Substring(0, Math.Min(200, error.StackTrace.Length))}...");
                }
                prompt.AppendLine();
            }

            if (warnings.Any())
            {
                prompt.AppendLine($"**Warnings ({warnings.Count}):**");
                foreach (var warning in warnings.Take(5))
                {
                    prompt.AppendLine($"- [{warning.Timestamp:HH:mm:ss}] {warning.Message}");
                }
                prompt.AppendLine();
            }
        }

        // Network requests section
        if (snapshot.NetworkRequests.Any())
        {
            prompt.AppendLine("### Network Activity");
            var failed = snapshot.NetworkRequests.Where(r => r.IsFailed).ToList();
            var slow = snapshot.NetworkRequests.Where(r => r.DurationMs > PerformanceThresholds.PoorResponseTimeMs).ToList();

            prompt.AppendLine($"Total Requests: {snapshot.NetworkRequests.Count}");
            prompt.AppendLine($"Failed Requests: {failed.Count}");
            prompt.AppendLine($"Slow Requests (>{PerformanceThresholds.PoorResponseTimeMs}ms): {slow.Count}");
            prompt.AppendLine();

            if (failed.Any())
            {
                prompt.AppendLine("**Failed Requests:**");
                foreach (var req in failed.Take(5))
                {
                    prompt.AppendLine($"- {req.Method} {req.Url} - Status: {req.StatusCode} ({req.ErrorText})");
                }
                prompt.AppendLine();
            }

            if (slow.Any())
            {
                prompt.AppendLine("**Slow Requests:**");
                foreach (var req in slow.Take(5))
                {
                    prompt.AppendLine($"- {req.Method} {req.Url} - {req.DurationMs:F0}ms");
                }
                prompt.AppendLine();
            }
        }

        // Performance metrics section
        if (snapshot.PerformanceMetrics.Any())
        {
            prompt.AppendLine("### Performance Metrics");
            var latest = snapshot.PerformanceMetrics.OrderByDescending(p => p.Timestamp).First();
            
            prompt.AppendLine($"- CPU Usage: {latest.CpuUsage:F2}%");
            prompt.AppendLine($"- Memory Usage: {latest.MemoryUsageBytes / (1024 * 1024):F2} MB");
            prompt.AppendLine($"- DOM Nodes: {latest.DomNodeCount}");
            prompt.AppendLine($"- Load Time: {latest.LoadEventMs:F0}ms");
            prompt.AppendLine($"- First Contentful Paint: {latest.FirstContentfulPaintMs:F0}ms");
            prompt.AppendLine($"- Largest Contentful Paint: {latest.LargestContentfulPaintMs:F0}ms");
            prompt.AppendLine();

            // Performance assessment
            if (latest.LoadEventMs > PerformanceThresholds.PoorLoadTimeMs)
                prompt.AppendLine("⚠️ Load time exceeds recommended threshold");
            if (latest.FirstContentfulPaintMs > PerformanceThresholds.PoorFCPMs)
                prompt.AppendLine("⚠️ First Contentful Paint is slow");
            if (latest.LargestContentfulPaintMs > PerformanceThresholds.PoorLCPMs)
                prompt.AppendLine("⚠️ Largest Contentful Paint is slow");
            prompt.AppendLine();
        }

        // DOM structure section (if available and needed)
        if (snapshot.DomSnapshot != null && options.IncludeCodeSnippets)
        {
            prompt.AppendLine("### DOM Structure");
            prompt.AppendLine($"- Document Title: {snapshot.DomSnapshot.DocumentTitle}");
            prompt.AppendLine($"- Total Nodes: {snapshot.DomSnapshot.Nodes.Count}");
            prompt.AppendLine();
        }

        // Add request for specific output format
        prompt.AppendLine("## Expected Output");
        prompt.AppendLine("Please provide your analysis in the following structure:");
        prompt.AppendLine("1. **Summary**: Brief overview of main issues");
        prompt.AppendLine("2. **Issues**: List of identified problems with severity");
        prompt.AppendLine("3. **Root Causes**: Explanation of why issues occurred");
        prompt.AppendLine("4. **Recommendations**: Actionable fixes with code examples if applicable");
        prompt.AppendLine("5. **Performance Assessment**: Overall performance grade and improvements");

        var result = prompt.ToString();
        
        _logger?.LogDebug("Built AI prompt context with {Length} characters", result.Length);
        
        return Task.FromResult(result);
    }

    /// <summary>
    /// Builds a structured context object for AI analysis
    /// </summary>
    public Task<AIAnalysisContext> BuildStructuredContextAsync(TelemetrySnapshot snapshot)
    {
        if (snapshot == null)
            throw new ArgumentNullException(nameof(snapshot));

        var context = new AIAnalysisContext
        {
            Url = snapshot.DomSnapshot?.Url ?? "Unknown"
        };

        // Analyze console messages
        var errors = snapshot.ConsoleMessages.Where(m => m.Level == ConsoleMessageLevel.Error).ToList();
        var warnings = snapshot.ConsoleMessages.Where(m => m.Level == ConsoleMessageLevel.Warning).ToList();

        context.ErrorCount = errors.Count;
        context.WarningCount = warnings.Count;
        context.ErrorMessages = errors.Select(e => e.Message).Take(20).ToList();
        context.WarningMessages = warnings.Select(w => w.Message).Take(10).ToList();

        // Build network summary
        var networkRequests = snapshot.NetworkRequests;
        context.NetworkSummary = new NetworkSummary
        {
            TotalRequests = networkRequests.Count,
            FailedRequests = networkRequests.Count(r => r.IsFailed),
            AverageResponseTimeMs = networkRequests.Any() ? networkRequests.Average(r => r.DurationMs) : 0,
            FailedUrls = networkRequests.Where(r => r.IsFailed).Select(r => r.Url).Distinct().ToList(),
            SlowRequests = networkRequests
                .Where(r => r.DurationMs > PerformanceThresholds.PoorResponseTimeMs)
                .Select(r => $"{r.Url} ({r.DurationMs:F0}ms)")
                .ToList()
        };

        // Build performance summary
        if (snapshot.PerformanceMetrics.Any())
        {
            var metrics = snapshot.PerformanceMetrics;
            context.PerformanceSummary = new PerformanceSummary
            {
                AverageCpuUsage = metrics.Average(m => m.CpuUsage),
                PeakMemoryUsageBytes = metrics.Max(m => m.MemoryUsageBytes),
                LoadTimeMs = metrics.OrderByDescending(m => m.Timestamp).First().LoadEventMs,
                FirstContentfulPaintMs = metrics.OrderByDescending(m => m.Timestamp).First().FirstContentfulPaintMs,
                DomNodeCount = metrics.OrderByDescending(m => m.Timestamp).First().DomNodeCount
            };
        }

        // Add DOM structure summary
        if (snapshot.DomSnapshot != null)
        {
            context.DomStructure = $"{snapshot.DomSnapshot.DocumentTitle} ({snapshot.DomSnapshot.Nodes.Count} nodes)";
        }

        // Additional context
        context.AdditionalContext["CapturedAt"] = snapshot.CapturedAt;
        context.AdditionalContext["SessionId"] = snapshot.SessionId;

        _logger?.LogDebug(
            "Built structured context: {Errors} errors, {Warnings} warnings, {Requests} requests",
            context.ErrorCount,
            context.WarningCount,
            context.NetworkSummary.TotalRequests);

        return Task.FromResult(context);
    }

    /// <summary>
    /// Redacts sensitive data from context
    /// </summary>
    public string RedactSensitiveData(string context, RedactionOptions options)
    {
        if (string.IsNullOrEmpty(context))
            return context;

        var redacted = context;

        // Redact API keys
        if (options.RedactApiKeys)
        {
            redacted = Regex.Replace(redacted, 
                @"(api[_-]?key|apikey|access[_-]?token)[=:\s]+['""]?[\w\-]+['""]?", 
                "$1=REDACTED", 
                RegexOptions.IgnoreCase);
        }

        // Redact tokens
        if (options.RedactTokens)
        {
            redacted = Regex.Replace(redacted, 
                @"(bearer|token)[=:\s]+['""]?[\w\-\.]+['""]?", 
                "$1=REDACTED", 
                RegexOptions.IgnoreCase);
        }

        // Redact passwords
        if (options.RedactPasswords)
        {
            redacted = Regex.Replace(redacted, 
                @"(password|passwd|pwd)[=:\s]+['""]?[^'""]+['""]?", 
                "$1=REDACTED", 
                RegexOptions.IgnoreCase);
        }

        // Redact emails
        if (options.RedactEmails)
        {
            redacted = Regex.Replace(redacted, 
                @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", 
                "[EMAIL_REDACTED]");
        }

        // Redact phone numbers
        if (options.RedactPhoneNumbers)
        {
            redacted = Regex.Replace(redacted, 
                @"\b\d{3}[-.]?\d{3}[-.]?\d{4}\b", 
                "[PHONE_REDACTED]");
        }

        // Redact URLs (if requested)
        if (options.RedactUrls)
        {
            redacted = Regex.Replace(redacted, 
                @"https?://[^\s]+", 
                "[URL_REDACTED]");
        }

        // Custom patterns
        foreach (var pattern in options.CustomPatterns)
        {
            try
            {
                redacted = Regex.Replace(redacted, pattern, "[REDACTED]");
            }
            catch (ArgumentException ex)
            {
                _logger?.LogWarning(ex, "Invalid regex pattern: {Pattern}", pattern);
            }
        }

        _logger?.LogDebug("Redacted sensitive data from context");

        return redacted;
    }

    /// <summary>
    /// Summarizes telemetry data for context
    /// </summary>
    public Task<string> SummarizeTelemetryAsync(TelemetrySnapshot snapshot)
    {
        if (snapshot == null)
            throw new ArgumentNullException(nameof(snapshot));

        var summary = new StringBuilder();

        // Summary header
        summary.AppendLine($"Telemetry Summary for {snapshot.DomSnapshot?.Url ?? "Unknown URL"}");
        summary.AppendLine($"Captured at: {snapshot.CapturedAt:yyyy-MM-dd HH:mm:ss} UTC");
        summary.AppendLine();

        // Console activity
        var errorCount = snapshot.ConsoleMessages.Count(m => m.Level == ConsoleMessageLevel.Error);
        var warningCount = snapshot.ConsoleMessages.Count(m => m.Level == ConsoleMessageLevel.Warning);
        summary.AppendLine($"Console Activity:");
        summary.AppendLine($"  • Errors: {errorCount}");
        summary.AppendLine($"  • Warnings: {warningCount}");
        summary.AppendLine($"  • Total Messages: {snapshot.ConsoleMessages.Count}");
        summary.AppendLine();

        // Network activity
        var failedRequests = snapshot.NetworkRequests.Count(r => r.IsFailed);
        var avgResponseTime = snapshot.NetworkRequests.Any() 
            ? snapshot.NetworkRequests.Average(r => r.DurationMs) 
            : 0;
        summary.AppendLine($"Network Activity:");
        summary.AppendLine($"  • Total Requests: {snapshot.NetworkRequests.Count}");
        summary.AppendLine($"  • Failed: {failedRequests}");
        summary.AppendLine($"  • Avg Response Time: {avgResponseTime:F0}ms");
        summary.AppendLine();

        // Performance
        if (snapshot.PerformanceMetrics.Any())
        {
            var latest = snapshot.PerformanceMetrics.OrderByDescending(p => p.Timestamp).First();
            summary.AppendLine($"Performance:");
            summary.AppendLine($"  • CPU Usage: {latest.CpuUsage:F2}%");
            summary.AppendLine($"  • Memory: {latest.MemoryUsageBytes / (1024 * 1024):F2} MB");
            summary.AppendLine($"  • Load Time: {latest.LoadEventMs:F0}ms");
            summary.AppendLine($"  • FCP: {latest.FirstContentfulPaintMs:F0}ms");
            summary.AppendLine();
        }

        // DOM
        if (snapshot.DomSnapshot != null)
        {
            summary.AppendLine($"DOM:");
            summary.AppendLine($"  • Title: {snapshot.DomSnapshot.DocumentTitle}");
            summary.AppendLine($"  • Nodes: {snapshot.DomSnapshot.Nodes.Count}");
        }

        var result = summary.ToString();
        _logger?.LogDebug("Generated telemetry summary");

        return Task.FromResult(result);
    }

    #endregion

    #region Builder Pattern Methods (Existing Functionality)

    public ContextBuilder AddSystemContext(string content, int priority = 0)
    {
        _contextItems.Add(new ContextItem
        {
            Type = ContextType.System,
            Content = content,
            Priority = priority,
            Timestamp = DateTime.UtcNow
        });
        return this;
    }

    public ContextBuilder AddCodeContext(string filePath, string code, int priority = 5)
    {
        _contextItems.Add(new ContextItem
        {
            Type = ContextType.Code,
            Content = code,
            Metadata = new Dictionary<string, string> { ["FilePath"] = filePath },
            Priority = priority,
            Timestamp = DateTime.UtcNow
        });
        return this;
    }

    public ContextBuilder AddErrorContext(string errorMessage, string? stackTrace = null, int priority = 10)
    {
        var content = errorMessage;
        if (!string.IsNullOrEmpty(stackTrace))
        {
            content += $"\nStack Trace:\n{stackTrace}";
        }

        _contextItems.Add(new ContextItem
        {
            Type = ContextType.Error,
            Content = content,
            Priority = priority,
            Timestamp = DateTime.UtcNow
        });
        return this;
    }

    public string BuildPrompt()
    {
        var orderedItems = _contextItems
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.Timestamp)
            .ToList();

        var prompt = new StringBuilder();
        var tokenCount = 0;

        foreach (var item in orderedItems)
        {
            var section = FormatContextItem(item);
            var estimatedTokens = EstimateTokenCount(section);

            if (tokenCount + estimatedTokens > _configuration.MaxTokens)
            {
                break;
            }

            prompt.AppendLine(section);
            prompt.AppendLine();
            tokenCount += estimatedTokens;
        }

        return prompt.ToString().Trim();
    }

    private string FormatContextItem(ContextItem item)
    {
        return item.Type switch
        {
            ContextType.System => $"# System Context\n{item.Content}",
            ContextType.Code => FormatCodeContext(item),
            ContextType.Error => $"# Error Context\n{item.Content}",
            ContextType.User => $"# User Input\n{item.Content}",
            _ => item.Content
        };
    }

    private string FormatCodeContext(ContextItem item)
    {
        var filePath = item.Metadata?.GetValueOrDefault("FilePath") ?? "Unknown";
        return $"# Code Context: {filePath}\n```csharp\n{item.Content}\n```";
    }

    private int EstimateTokenCount(string text)
    {
        return (int)Math.Ceiling(text.Length / 4.0);
    }

    #endregion
}

#region Supporting Classes

internal class ContextItem
{
    public ContextType Type { get; set; }
    public string Content { get; set; } = string.Empty;
    public Dictionary<string, string>? Metadata { get; set; }
    public int Priority { get; set; }
    public DateTime Timestamp { get; set; }
}

internal enum ContextType
{
    System,
    Code,
    Error,
    User,
    Custom
}

public class PromptConfiguration
{
    public int MaxTokens { get; set; } = 4000;
    public bool IncludeTimestamps { get; set; } = false;
    public string PromptTemplate { get; set; } = string.Empty;
}

public class PromptBuilder
{
    private readonly StringBuilder _prompt = new();

    public PromptBuilder AddSection(string title, string content)
    {
        _prompt.AppendLine($"## {title}");
        _prompt.AppendLine(content);
        _prompt.AppendLine();
        return this;
    }

    public PromptBuilder AddInstructions(params string[] instructions)
    {
        _prompt.AppendLine("## Instructions");
        foreach (var instruction in instructions)
        {
            _prompt.AppendLine($"- {instruction}");
        }
        _prompt.AppendLine();
        return this;
    }

    public string Build() => _prompt.ToString().Trim();
}

public static class PromptTemplates
{
    public static string DebugAssistant => 
        "You are an AI debugging assistant. Analyze the provided code and error context to identify issues and suggest fixes.";

    public static string CodeReview => 
        "You are an AI code reviewer. Review the provided code for potential issues, best practices, and improvement opportunities.";

    public static string CodeExplanation => 
        "You are an AI code explainer. Explain the provided code in clear, concise terms.";
}

#endregion
