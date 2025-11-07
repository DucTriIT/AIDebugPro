using System.Text;
using Microsoft.Extensions.Logging;
using AIDebugPro.Core.Models;
using AIDebugPro.Core.Constants;

namespace AIDebugPro.Persistence;

/// <summary>
/// Generates reports from debug session data
/// </summary>
public class ReportGenerator
{
    private readonly ILogger<ReportGenerator>? _logger;

    public ReportGenerator(ILogger<ReportGenerator>? logger = null)
    {
        _logger = logger;
    }

    #region HTML Reports

    /// <summary>
    /// Generates an HTML report for a debug session
    /// </summary>
    public async Task<string> GenerateHtmlReportAsync(DebugSession session)
    {
        return await Task.Run(() =>
        {
            var html = new StringBuilder();

            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html lang='en'>");
            html.AppendLine("<head>");
            html.AppendLine("    <meta charset='UTF-8'>");
            html.AppendLine("    <meta name='viewport' content='width=device-width, initial-scale=1.0'>");
            html.AppendLine($"    <title>Debug Report - {session.Name}</title>");
            html.AppendLine(GetHtmlStyles());
            html.AppendLine("</head>");
            html.AppendLine("<body>");

            // Header
            html.AppendLine("    <div class='container'>");
            html.AppendLine("        <header>");
            html.AppendLine($"            <h1>Debug Session Report</h1>");
            html.AppendLine($"            <h2>{session.Name}</h2>");
            html.AppendLine("        </header>");

            // Session Info
            html.AppendLine("        <section class='session-info'>");
            html.AppendLine("            <h3>Session Information</h3>");
            html.AppendLine("            <table>");
            html.AppendLine($"                <tr><th>URL:</th><td>{session.Url}</td></tr>");
            html.AppendLine($"                <tr><th>Started:</th><td>{session.StartedAt:yyyy-MM-dd HH:mm:ss} UTC</td></tr>");
            html.AppendLine($"                <tr><th>Status:</th><td class='status-{session.Status.ToString().ToLower()}'>{session.Status}</td></tr>");
            if (session.EndedAt.HasValue)
                html.AppendLine($"                <tr><th>Ended:</th><td>{session.EndedAt:yyyy-MM-dd HH:mm:ss} UTC</td></tr>");
            if (!string.IsNullOrEmpty(session.Description))
                html.AppendLine($"                <tr><th>Description:</th><td>{session.Description}</td></tr>");
            html.AppendLine("            </table>");
            html.AppendLine("        </section>");

            // Statistics
            html.AppendLine("        <section class='statistics'>");
            html.AppendLine("            <h3>Statistics</h3>");
            html.AppendLine("            <div class='stat-grid'>");
            html.AppendLine($"                <div class='stat-card error'>");
            html.AppendLine($"                    <div class='stat-value'>{session.Statistics.TotalConsoleErrors}</div>");
            html.AppendLine($"                    <div class='stat-label'>Console Errors</div>");
            html.AppendLine("                </div>");
            html.AppendLine($"                <div class='stat-card warning'>");
            html.AppendLine($"                    <div class='stat-value'>{session.Statistics.TotalConsoleWarnings}</div>");
            html.AppendLine($"                    <div class='stat-label'>Console Warnings</div>");
            html.AppendLine("                </div>");
            html.AppendLine($"                <div class='stat-card'>");
            html.AppendLine($"                    <div class='stat-value'>{session.Statistics.TotalNetworkRequests}</div>");
            html.AppendLine($"                    <div class='stat-label'>Network Requests</div>");
            html.AppendLine("                </div>");
            html.AppendLine($"                <div class='stat-card error'>");
            html.AppendLine($"                    <div class='stat-value'>{session.Statistics.FailedNetworkRequests}</div>");
            html.AppendLine($"                    <div class='stat-label'>Failed Requests</div>");
            html.AppendLine("                </div>");
            html.AppendLine($"                <div class='stat-card'>");
            html.AppendLine($"                    <div class='stat-value'>{session.Statistics.AverageResponseTimeMs:F0}ms</div>");
            html.AppendLine($"                    <div class='stat-label'>Avg Response Time</div>");
            html.AppendLine("                </div>");
            html.AppendLine($"                <div class='stat-card'>");
            html.AppendLine($"                    <div class='stat-value'>{session.Statistics.PeakCpuUsage:F1}%</div>");
            html.AppendLine($"                    <div class='stat-label'>Peak CPU</div>");
            html.AppendLine("                </div>");
            html.AppendLine($"                <div class='stat-card'>");
            html.AppendLine($"                    <div class='stat-value'>{session.Statistics.PeakMemoryUsageBytes / (1024 * 1024):F0}MB</div>");
            html.AppendLine($"                    <div class='stat-label'>Peak Memory</div>");
            html.AppendLine("                </div>");
            html.AppendLine($"                <div class='stat-card'>");
            html.AppendLine($"                    <div class='stat-value'>{session.Statistics.SnapshotCount}</div>");
            html.AppendLine($"                    <div class='stat-label'>Snapshots</div>");
            html.AppendLine("                </div>");
            html.AppendLine("            </div>");
            html.AppendLine("        </section>");

            // AI Analysis Results
            if (session.AnalysisResults.Any())
            {
                html.AppendLine("        <section class='analysis-results'>");
                html.AppendLine("            <h3>AI Analysis Results</h3>");

                foreach (var result in session.AnalysisResults.OrderByDescending(r => r.AnalyzedAt))
                {
                    html.AppendLine("            <div class='analysis-result'>");
                    html.AppendLine($"                <h4>Analysis - {result.AnalyzedAt:yyyy-MM-dd HH:mm:ss}</h4>");
                    html.AppendLine($"                <p><strong>Model:</strong> {result.Model}</p>");
                    html.AppendLine($"                <p><strong>Status:</strong> {result.Status}</p>");

                    if (!string.IsNullOrEmpty(result.Summary))
                    {
                        html.AppendLine($"                <div class='summary'>");
                        html.AppendLine($"                    <h5>Summary</h5>");
                        html.AppendLine($"                    <p>{result.Summary}</p>");
                        html.AppendLine($"                </div>");
                    }

                    if (result.Issues.Any())
                    {
                        html.AppendLine("                <div class='issues'>");
                        html.AppendLine("                    <h5>Issues Found</h5>");
                        foreach (var issue in result.Issues)
                        {
                            html.AppendLine($"                    <div class='issue severity-{issue.Severity.ToString().ToLower()}'>");
                            html.AppendLine($"                        <strong>{issue.Title}</strong>");
                            html.AppendLine($"                        <p>{issue.Description}</p>");
                            if (issue.SuggestedFixes.Any())
                            {
                                html.AppendLine("                        <ul>");
                                foreach (var fix in issue.SuggestedFixes)
                                {
                                    html.AppendLine($"                            <li>{fix}</li>");
                                }
                                html.AppendLine("                        </ul>");
                            }
                            html.AppendLine("                    </div>");
                        }
                        html.AppendLine("                </div>");
                    }

                    if (result.Recommendations.Any())
                    {
                        html.AppendLine("                <div class='recommendations'>");
                        html.AppendLine("                    <h5>Recommendations</h5>");
                        foreach (var rec in result.Recommendations)
                        {
                            html.AppendLine($"                    <div class='recommendation priority-{rec.Priority.ToString().ToLower()}'>");
                            html.AppendLine($"                        <strong>{rec.Title}</strong>");
                            html.AppendLine($"                        <p>{rec.Description}</p>");
                            html.AppendLine("                    </div>");
                        }
                        html.AppendLine("                </div>");
                    }

                    html.AppendLine("            </div>");
                }

                html.AppendLine("        </section>");
            }

            // Footer
            html.AppendLine("        <footer>");
            html.AppendLine($"            <p>Generated by AIDebugPro v{AppConstants.ApplicationVersion} on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>");
            html.AppendLine("        </footer>");

            html.AppendLine("    </div>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");

            _logger?.LogInformation("Generated HTML report for session {SessionId}", session.Id);

            return html.ToString();
        });
    }

    private string GetHtmlStyles()
    {
        return @"
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background: #f5f5f5; padding: 20px; }
        .container { max-width: 1200px; margin: 0 auto; background: white; padding: 40px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
        header { border-bottom: 3px solid #2196F3; padding-bottom: 20px; margin-bottom: 30px; }
        h1 { color: #333; font-size: 32px; }
        h2 { color: #666; font-size: 24px; margin-top: 10px; }
        h3 { color: #2196F3; font-size: 22px; margin: 30px 0 15px; padding-bottom: 10px; border-bottom: 2px solid #e0e0e0; }
        h4 { color: #444; font-size: 18px; margin: 20px 0 10px; }
        h5 { color: #555; font-size: 16px; margin: 15px 0 10px; }
        .session-info table { width: 100%; border-collapse: collapse; }
        .session-info th { text-align: left; padding: 10px; background: #f9f9f9; width: 150px; font-weight: 600; }
        .session-info td { padding: 10px; border-bottom: 1px solid #eee; }
        .status-active { color: #4CAF50; font-weight: bold; }
        .status-completed { color: #2196F3; font-weight: bold; }
        .status-failed { color: #f44336; font-weight: bold; }
        .stat-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(150px, 1fr)); gap: 15px; margin: 20px 0; }
        .stat-card { background: #f9f9f9; padding: 20px; border-radius: 6px; text-align: center; border-left: 4px solid #2196F3; }
        .stat-card.error { border-left-color: #f44336; background: #ffebee; }
        .stat-card.warning { border-left-color: #ff9800; background: #fff3e0; }
        .stat-value { font-size: 32px; font-weight: bold; color: #333; }
        .stat-label { font-size: 12px; color: #666; margin-top: 5px; text-transform: uppercase; }
        .analysis-result { background: #f9f9f9; padding: 20px; margin: 15px 0; border-radius: 6px; border-left: 4px solid #2196F3; }
        .summary { background: white; padding: 15px; margin: 15px 0; border-radius: 4px; }
        .issue { background: white; padding: 15px; margin: 10px 0; border-radius: 4px; border-left: 4px solid #ff9800; }
        .issue.severity-critical { border-left-color: #f44336; }
        .issue.severity-high { border-left-color: #ff5722; }
        .issue.severity-medium { border-left-color: #ff9800; }
        .issue.severity-low { border-left-color: #ffc107; }
        .issue ul { margin: 10px 0 0 20px; }
        .recommendation { background: white; padding: 15px; margin: 10px 0; border-radius: 4px; border-left: 4px solid #4CAF50; }
        footer { margin-top: 40px; padding-top: 20px; border-top: 2px solid #e0e0e0; text-align: center; color: #999; font-size: 14px; }
    </style>";
    }

    #endregion

    #region Markdown Reports

    /// <summary>
    /// Generates a Markdown report for a debug session
    /// </summary>
    public async Task<string> GenerateMarkdownReportAsync(DebugSession session)
    {
        return await Task.Run(() =>
        {
            var md = new StringBuilder();

            md.AppendLine($"# Debug Session Report: {session.Name}");
            md.AppendLine();

            // Session Info
            md.AppendLine("## Session Information");
            md.AppendLine();
            md.AppendLine($"- **URL**: {session.Url}");
            md.AppendLine($"- **Started**: {session.StartedAt:yyyy-MM-dd HH:mm:ss} UTC");
            md.AppendLine($"- **Status**: {session.Status}");
            if (session.EndedAt.HasValue)
                md.AppendLine($"- **Ended**: {session.EndedAt:yyyy-MM-dd HH:mm:ss} UTC");
            if (!string.IsNullOrEmpty(session.Description))
                md.AppendLine($"- **Description**: {session.Description}");
            md.AppendLine();

            // Statistics
            md.AppendLine("## Statistics");
            md.AppendLine();
            md.AppendLine("| Metric | Value |");
            md.AppendLine("|--------|-------|");
            md.AppendLine($"| Console Errors | {session.Statistics.TotalConsoleErrors} |");
            md.AppendLine($"| Console Warnings | {session.Statistics.TotalConsoleWarnings} |");
            md.AppendLine($"| Network Requests | {session.Statistics.TotalNetworkRequests} |");
            md.AppendLine($"| Failed Requests | {session.Statistics.FailedNetworkRequests} |");
            md.AppendLine($"| Avg Response Time | {session.Statistics.AverageResponseTimeMs:F2} ms |");
            md.AppendLine($"| Peak CPU | {session.Statistics.PeakCpuUsage:F2}% |");
            md.AppendLine($"| Peak Memory | {session.Statistics.PeakMemoryUsageBytes / (1024 * 1024):F2} MB |");
            md.AppendLine($"| Snapshots | {session.Statistics.SnapshotCount} |");
            md.AppendLine($"| AI Analyses | {session.Statistics.AIAnalysisCount} |");
            md.AppendLine();

            // AI Analysis Results
            if (session.AnalysisResults.Any())
            {
                md.AppendLine("## AI Analysis Results");
                md.AppendLine();

                foreach (var result in session.AnalysisResults.OrderByDescending(r => r.AnalyzedAt))
                {
                    md.AppendLine($"### Analysis - {result.AnalyzedAt:yyyy-MM-dd HH:mm:ss}");
                    md.AppendLine();
                    md.AppendLine($"- **Model**: {result.Model}");
                    md.AppendLine($"- **Status**: {result.Status}");
                    md.AppendLine($"- **Tokens Used**: {result.TokensUsed}");
                    md.AppendLine();

                    if (!string.IsNullOrEmpty(result.Summary))
                    {
                        md.AppendLine("#### Summary");
                        md.AppendLine();
                        md.AppendLine(result.Summary);
                        md.AppendLine();
                    }

                    if (result.Issues.Any())
                    {
                        md.AppendLine("#### Issues");
                        md.AppendLine();
                        foreach (var issue in result.Issues)
                        {
                            md.AppendLine($"**{issue.Severity}**: {issue.Title}");
                            md.AppendLine();
                            md.AppendLine(issue.Description);
                            md.AppendLine();

                            if (issue.SuggestedFixes.Any())
                            {
                                md.AppendLine("**Suggested Fixes:**");
                                foreach (var fix in issue.SuggestedFixes)
                                {
                                    md.AppendLine($"- {fix}");
                                }
                                md.AppendLine();
                            }
                        }
                    }

                    if (result.Recommendations.Any())
                    {
                        md.AppendLine("#### Recommendations");
                        md.AppendLine();
                        foreach (var rec in result.Recommendations)
                        {
                            md.AppendLine($"**{rec.Priority}**: {rec.Title}");
                            md.AppendLine();
                            md.AppendLine(rec.Description);
                            md.AppendLine();
                        }
                    }
                }
            }

            // Footer
            md.AppendLine("---");
            md.AppendLine();
            md.AppendLine($"*Generated by AIDebugPro v{AppConstants.ApplicationVersion} on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC*");

            _logger?.LogInformation("Generated Markdown report for session {SessionId}", session.Id);

            return md.ToString();
        });
    }

    #endregion

    #region Save Reports

    /// <summary>
    /// Saves a report to a file
    /// </summary>
    public async Task<string> SaveReportAsync(
        DebugSession session,
        ReportFormat format,
        string? outputPath = null)
    {
        string content;
        string extension;

        switch (format)
        {
            case ReportFormat.HTML:
                content = await GenerateHtmlReportAsync(session);
                extension = ".html";
                break;

            case ReportFormat.Markdown:
                content = await GenerateMarkdownReportAsync(session);
                extension = ".md";
                break;

            case ReportFormat.JSON:
                content = Newtonsoft.Json.JsonConvert.SerializeObject(session, Newtonsoft.Json.Formatting.Indented);
                extension = ".json";
                break;

            default:
                throw new NotSupportedException($"Report format {format} is not supported");
        }

        outputPath ??= Path.Combine(
            AppConstants.ReportsFolder,
            $"report_{session.Name}_{DateTime.UtcNow:yyyyMMdd_HHmmss}{extension}");

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(outputPath, content);

        _logger?.LogInformation("Saved {Format} report to {Path}", format, outputPath);

        return outputPath;
    }

    #endregion
}
