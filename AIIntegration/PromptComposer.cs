using System.Text;
using AIDebugPro.Core.Models;
using AIDebugPro.Core.Constants;

namespace AIDebugPro.AIIntegration;

/// <summary>
/// Composes AI prompts from telemetry data
/// </summary>
public class PromptComposer
{
    /// <summary>
    /// Builds a comprehensive analysis prompt
    /// </summary>
    public string BuildAnalysisPrompt(
        TelemetrySnapshot snapshot,
        PromptOptions? options = null)
    {
        options ??= new PromptOptions();

        var prompt = new StringBuilder();

        // System context
        if (!string.IsNullOrWhiteSpace(options.SystemPrompt))
        {
            prompt.AppendLine(options.SystemPrompt);
            prompt.AppendLine();
        }

        // Analysis instructions
        prompt.AppendLine("# AI Debugging Assistant");
        prompt.AppendLine();
        prompt.AppendLine("Analyze the following telemetry data and provide:");
        
        if (options.IncludeErrorAnalysis)
            prompt.AppendLine("1. Detailed error analysis with root causes");
        
        if (options.IncludePerformanceAnalysis)
            prompt.AppendLine("2. Performance bottleneck identification");
        
        if (options.IncludeNetworkAnalysis)
            prompt.AppendLine("3. Network issue diagnosis");
        
        if (options.IncludeFixSuggestions)
            prompt.AppendLine("4. Actionable fix suggestions with code examples");
        
        prompt.AppendLine();

        // Console errors section
        if (options.IncludeErrors && snapshot.ConsoleMessages.Any())
        {
            var errors = snapshot.ConsoleMessages
                .Where(m => m.Level == ConsoleMessageLevel.Error)
                .Take(options.MaxErrors)
                .ToList();

            if (errors.Any())
            {
                prompt.AppendLine("## Console Errors");
                prompt.AppendLine();
                
                foreach (var error in errors)
                {
                    prompt.AppendLine($"### Error: {error.Message}");
                    if (!string.IsNullOrWhiteSpace(error.Source))
                        prompt.AppendLine($"**Source:** {error.Source}:{error.LineNumber}");
                    
                    if (!string.IsNullOrWhiteSpace(error.StackTrace))
                    {
                        prompt.AppendLine("**Stack Trace:**");
                        prompt.AppendLine("```");
                        prompt.AppendLine(error.StackTrace);
                        prompt.AppendLine("```");
                    }
                    prompt.AppendLine();
                }
            }
        }

        // Network issues section
        if (options.IncludeNetworkIssues && snapshot.NetworkRequests.Any())
        {
            var failedRequests = snapshot.NetworkRequests
                .Where(r => r.IsFailed)
                .Take(options.MaxNetworkRequests)
                .ToList();

            var slowRequests = snapshot.NetworkRequests
                .Where(r => r.DurationMs > PerformanceThresholds.PoorResponseTimeMs)
                .OrderByDescending(r => r.DurationMs)
                .Take(5)
                .ToList();

            if (failedRequests.Any() || slowRequests.Any())
            {
                prompt.AppendLine("## Network Issues");
                prompt.AppendLine();

                if (failedRequests.Any())
                {
                    prompt.AppendLine("### Failed Requests");
                    foreach (var request in failedRequests)
                    {
                        prompt.AppendLine($"- {request.Method} {request.Url}");
                        prompt.AppendLine($"  Status: {request.StatusCode} - {request.ErrorText}");
                    }
                    prompt.AppendLine();
                }

                if (slowRequests.Any())
                {
                    prompt.AppendLine("### Slow Requests (>500ms)");
                    foreach (var request in slowRequests)
                    {
                        prompt.AppendLine($"- {request.Method} {request.Url}");
                        prompt.AppendLine($"  Duration: {request.DurationMs:F0}ms");
                    }
                    prompt.AppendLine();
                }
            }
        }

        // Performance metrics section
        if (options.IncludePerformanceMetrics && snapshot.PerformanceMetrics.Any())
        {
            var latest = snapshot.PerformanceMetrics.OrderByDescending(p => p.Timestamp).First();

            prompt.AppendLine("## Performance Metrics");
            prompt.AppendLine();
            prompt.AppendLine($"- Load Time: {latest.LoadEventMs:F0}ms");
            prompt.AppendLine($"- First Contentful Paint: {latest.FirstContentfulPaintMs:F0}ms");
            prompt.AppendLine($"- Largest Contentful Paint: {latest.LargestContentfulPaintMs:F0}ms");
            prompt.AppendLine($"- DOM Nodes: {latest.DomNodeCount}");
            prompt.AppendLine($"- Memory Usage: {latest.MemoryUsageBytes / (1024 * 1024):F2} MB");
            prompt.AppendLine();

            // Performance assessment
            var issues = new List<string>();
            
            if (latest.LoadEventMs > PerformanceThresholds.PoorLoadTimeMs)
                issues.Add($"Load time ({latest.LoadEventMs:F0}ms) exceeds threshold ({PerformanceThresholds.PoorLoadTimeMs}ms)");
            
            if (latest.FirstContentfulPaintMs > PerformanceThresholds.PoorFCPMs)
                issues.Add($"FCP ({latest.FirstContentfulPaintMs:F0}ms) exceeds threshold ({PerformanceThresholds.PoorFCPMs}ms)");
            
            if (latest.LargestContentfulPaintMs > PerformanceThresholds.PoorLCPMs)
                issues.Add($"LCP ({latest.LargestContentfulPaintMs:F0}ms) exceeds threshold ({PerformanceThresholds.PoorLCPMs}ms)");

            if (issues.Any())
            {
                prompt.AppendLine("**Performance Issues:**");
                foreach (var issue in issues)
                {
                    prompt.AppendLine($"- {issue}");
                }
                prompt.AppendLine();
            }
        }

        // Additional context
        if (!string.IsNullOrWhiteSpace(options.AdditionalContext))
        {
            prompt.AppendLine("## Additional Context");
            prompt.AppendLine();
            prompt.AppendLine(options.AdditionalContext);
            prompt.AppendLine();
        }

        // Expected output format
        prompt.AppendLine("## Expected Output Format");
        prompt.AppendLine();
        prompt.AppendLine("Provide your analysis in the following structure:");
        prompt.AppendLine();
        prompt.AppendLine("**Summary:** Brief overview of main issues");
        prompt.AppendLine();
        prompt.AppendLine("**Issues:** List each problem with:");
        prompt.AppendLine("- Severity (Critical/High/Medium/Low)");
        prompt.AppendLine("- Root cause explanation");
        prompt.AppendLine("- Impact on users");
        prompt.AppendLine();
        prompt.AppendLine("**Recommendations:** For each issue provide:");
        prompt.AppendLine("- Specific fix steps");
        prompt.AppendLine("- Code examples if applicable");
        prompt.AppendLine("- Priority (High/Medium/Low)");
        prompt.AppendLine();

        return prompt.ToString();
    }

    /// <summary>
    /// Builds a focused error analysis prompt
    /// </summary>
    public string BuildErrorAnalysisPrompt(ConsoleMessage error, string? codeContext = null)
    {
        var prompt = new StringBuilder();

        prompt.AppendLine("# JavaScript Error Analysis");
        prompt.AppendLine();
        prompt.AppendLine("Analyze the following JavaScript error and provide:");
        prompt.AppendLine("1. Root cause explanation");
        prompt.AppendLine("2. Common scenarios causing this error");
        prompt.AppendLine("3. Step-by-step fix instructions");
        prompt.AppendLine("4. Code example of the fix");
        prompt.AppendLine();

        prompt.AppendLine("## Error Details");
        prompt.AppendLine();
        prompt.AppendLine($"**Message:** {error.Message}");
        prompt.AppendLine($"**Source:** {error.Source}:{error.LineNumber}:{error.ColumnNumber}");
        prompt.AppendLine();

        if (!string.IsNullOrWhiteSpace(error.StackTrace))
        {
            prompt.AppendLine("**Stack Trace:**");
            prompt.AppendLine("```");
            prompt.AppendLine(error.StackTrace);
            prompt.AppendLine("```");
            prompt.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(codeContext))
        {
            prompt.AppendLine("**Code Context:**");
            prompt.AppendLine("```javascript");
            prompt.AppendLine(codeContext);
            prompt.AppendLine("```");
            prompt.AppendLine();
        }

        return prompt.ToString();
    }

    /// <summary>
    /// Builds a performance optimization prompt
    /// </summary>
    public string BuildPerformancePrompt(PerformanceMetrics metrics)
    {
        var prompt = new StringBuilder();

        prompt.AppendLine("# Performance Optimization Analysis");
        prompt.AppendLine();
        prompt.AppendLine("Analyze these performance metrics and provide:");
        prompt.AppendLine("1. Performance bottleneck identification");
        prompt.AppendLine("2. Optimization priorities");
        prompt.AppendLine("3. Specific optimization techniques");
        prompt.AppendLine("4. Expected improvements");
        prompt.AppendLine();

        prompt.AppendLine("## Metrics");
        prompt.AppendLine();
        prompt.AppendLine($"- Load Time: {metrics.LoadEventMs:F0}ms");
        prompt.AppendLine($"- First Contentful Paint: {metrics.FirstContentfulPaintMs:F0}ms");
        prompt.AppendLine($"- Largest Contentful Paint: {metrics.LargestContentfulPaintMs:F0}ms");
        prompt.AppendLine($"- DOM Content Loaded: {metrics.DomContentLoadedMs:F0}ms");
        prompt.AppendLine($"- DOM Nodes: {metrics.DomNodeCount}");
        prompt.AppendLine($"- Memory Usage: {metrics.MemoryUsageBytes / (1024 * 1024):F2} MB");
        prompt.AppendLine();

        // Add thresholds for context
        prompt.AppendLine("## Performance Thresholds");
        prompt.AppendLine();
        prompt.AppendLine($"- Good Load Time: < {PerformanceThresholds.GoodLoadTimeMs}ms");
        prompt.AppendLine($"- Good FCP: < {PerformanceThresholds.GoodFCPMs}ms");
        prompt.AppendLine($"- Good LCP: < {PerformanceThresholds.GoodLCPMs}ms");
        prompt.AppendLine();

        return prompt.ToString();
    }

    /// <summary>
    /// Builds a network debugging prompt
    /// </summary>
    public string BuildNetworkDebugPrompt(List<NetworkRequest> failedRequests)
    {
        var prompt = new StringBuilder();

        prompt.AppendLine("# Network Issue Debugging");
        prompt.AppendLine();
        prompt.AppendLine("Analyze these failed network requests and provide:");
        prompt.AppendLine("1. Likely causes for each failure");
        prompt.AppendLine("2. Debugging steps");
        prompt.AppendLine("3. Fix recommendations");
        prompt.AppendLine("4. Prevention strategies");
        prompt.AppendLine();

        prompt.AppendLine("## Failed Requests");
        prompt.AppendLine();

        foreach (var request in failedRequests)
        {
            prompt.AppendLine($"### {request.Method} {request.Url}");
            prompt.AppendLine($"- Status: {request.StatusCode}");
            prompt.AppendLine($"- Error: {request.ErrorText}");
            prompt.AppendLine($"- Duration: {request.DurationMs:F0}ms");
            
            if (request.RequestHeaders.Any())
            {
                prompt.AppendLine("- Headers:");
                foreach (var header in request.RequestHeaders.Take(5))
                {
                    prompt.AppendLine($"  - {header.Key}: {header.Value}");
                }
            }
            prompt.AppendLine();
        }

        return prompt.ToString();
    }
}

/// <summary>
/// Options for prompt composition
/// </summary>
public class PromptOptions
{
    public string? SystemPrompt { get; set; }
    public bool IncludeErrors { get; set; } = true;
    public bool IncludeNetworkIssues { get; set; } = true;
    public bool IncludePerformanceMetrics { get; set; } = true;
    public bool IncludeErrorAnalysis { get; set; } = true;
    public bool IncludePerformanceAnalysis { get; set; } = true;
    public bool IncludeNetworkAnalysis { get; set; } = true;
    public bool IncludeFixSuggestions { get; set; } = true;
    public int MaxErrors { get; set; } = 10;
    public int MaxNetworkRequests { get; set; } = 10;
    public string? AdditionalContext { get; set; }
}
