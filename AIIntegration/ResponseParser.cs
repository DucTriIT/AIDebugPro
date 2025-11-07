using System.Text.Json;
using System.Text.RegularExpressions;
using AIDebugPro.Core.Models;

namespace AIDebugPro.AIIntegration;

/// <summary>
/// Parses AI responses into structured data
/// </summary>
public class ResponseParser
{
    /// <summary>
    /// Parses AI response text into structured result
    /// </summary>
    public AIAnalysisResult Parse(string responseText, string model, int tokensUsed)
    {
        var result = new AIAnalysisResult
        {
            Id = Guid.NewGuid(),
            AnalyzedAt = DateTime.UtcNow,
            Model = model,
            TokensUsed = tokensUsed,
            Status = AIAnalysisStatus.Completed
        };

        // Store raw response in metadata
        result.Metadata["rawResponse"] = responseText;

        try
        {
            // Try JSON parsing first
            if (TryParseAsJson(responseText, result))
            {
                return result;
            }

            // Fall back to text parsing
            ParseAsText(responseText, result);
        }
        catch (Exception)
        {
            // Use raw response as summary if parsing fails
            result.Summary = ExtractFirstParagraph(responseText);
        }

        return result;
    }

    #region JSON Parsing

    private bool TryParseAsJson(string responseText, AIAnalysisResult result)
    {
        try
        {
            // Try to extract JSON from markdown code blocks
            var jsonMatch = Regex.Match(responseText, @"```json\s*(.*?)\s*```", RegexOptions.Singleline);
            var jsonText = jsonMatch.Success ? jsonMatch.Groups[1].Value : responseText;

            var jsonDoc = JsonDocument.Parse(jsonText);
            var root = jsonDoc.RootElement;

            // Parse summary
            if (root.TryGetProperty("summary", out var summary))
            {
                result.Summary = summary.GetString() ?? "";
            }

            // Parse issues
            if (root.TryGetProperty("issues", out var issues))
            {
                foreach (var issue in issues.EnumerateArray())
                {
                    result.Issues.Add(ParseIssueFromJson(issue));
                }
            }

            // Parse recommendations
            if (root.TryGetProperty("recommendations", out var recommendations))
            {
                foreach (var rec in recommendations.EnumerateArray())
                {
                    result.Recommendations.Add(ParseRecommendationFromJson(rec));
                }
            }

            // Parse performance assessment
            if (root.TryGetProperty("performance", out var performance))
            {
                result.PerformanceAssessment = ParsePerformanceFromJson(performance);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private Issue ParseIssueFromJson(JsonElement element)
    {
        var issue = new Issue
        {
            Title = element.TryGetProperty("title", out var title) ? title.GetString() ?? "" : "",
            Description = element.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
        };

        // Parse severity
        if (element.TryGetProperty("severity", out var severity))
        {
            issue.Severity = Enum.TryParse<IssueSeverity>(severity.GetString(), true, out var sev)
                ? sev
                : IssueSeverity.Medium;
        }

        // Parse category
        if (element.TryGetProperty("category", out var category))
        {
            issue.Category = Enum.TryParse<IssueCategory>(category.GetString(), true, out var cat)
                ? cat
                : IssueCategory.Other;
        }

        // Parse fixes
        if (element.TryGetProperty("suggestedFixes", out var fixes))
        {
            foreach (var fix in fixes.EnumerateArray())
            {
                issue.SuggestedFixes.Add(fix.GetString() ?? "");
            }
        }

        // Parse code location
        if (element.TryGetProperty("location", out var location))
        {
            if (location.TryGetProperty("file", out var file))
                issue.Source = file.GetString();
            if (location.TryGetProperty("line", out var line))
                issue.LineNumber = line.GetInt32();
        }

        return issue;
    }

    private Recommendation ParseRecommendationFromJson(JsonElement element)
    {
        var rec = new Recommendation
        {
            Title = element.TryGetProperty("title", out var title) ? title.GetString() ?? "" : "",
            Description = element.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
        };

        // Parse type
        if (element.TryGetProperty("type", out var type))
        {
            rec.Type = Enum.TryParse<RecommendationType>(type.GetString(), true, out var t)
                ? t
                : RecommendationType.BestPractice;
        }

        // Parse priority
        if (element.TryGetProperty("priority", out var priority))
        {
            rec.Priority = Enum.TryParse<RecommendationPriority>(priority.GetString(), true, out var p)
                ? p
                : RecommendationPriority.Medium;
        }

        // Parse implementation steps
        if (element.TryGetProperty("steps", out var steps))
        {
            foreach (var step in steps.EnumerateArray())
            {
                rec.ImplementationSteps.Add(step.GetString() ?? "");
            }
        }

        // Store code example in Rationale if available
        if (element.TryGetProperty("codeExample", out var code))
        {
            rec.Rationale = code.GetString();
        }

        return rec;
    }

    private PerformanceAssessment ParsePerformanceFromJson(JsonElement element)
    {
        var assessment = new PerformanceAssessment();

        if (element.TryGetProperty("grade", out var grade))
        {
            assessment.Grade = Enum.TryParse<PerformanceGrade>(grade.GetString(), true, out var g)
                ? g
                : PerformanceGrade.C;
        }

        if (element.TryGetProperty("score", out var score))
        {
            assessment.OverallScore = score.GetDouble();
        }

        // Parse metric assessments
        if (element.TryGetProperty("metrics", out var metrics))
        {
            foreach (var metric in metrics.EnumerateObject())
            {
                var metricName = metric.Name;
                assessment.MetricAssessments[metricName] = new MetricAssessment
                {
                    MetricName = metricName,
                    Value = metric.Value.TryGetProperty("value", out var v) ? v.GetDouble() : 0,
                    Assessment = metric.Value.TryGetProperty("assessment", out var a) ? a.GetString() ?? "" : ""
                };
            }
        }

        return assessment;
    }

    #endregion

    #region Text Parsing

    private void ParseAsText(string responseText, AIAnalysisResult result)
    {
        // Extract summary
        result.Summary = ExtractSummary(responseText);

        // Extract issues
        result.Issues = ExtractIssues(responseText);

        // Extract recommendations
        result.Recommendations = ExtractRecommendations(responseText);

        // Extract performance assessment
        result.PerformanceAssessment = ExtractPerformanceAssessment(responseText);
    }

    private string ExtractSummary(string text)
    {
        // Look for summary section
        var summaryMatch = Regex.Match(
            text,
            @"(?:Summary|SUMMARY|Overview)[:\s]*(.*?)(?=\n\n|##|$)",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        if (summaryMatch.Success)
        {
            return summaryMatch.Groups[1].Value.Trim();
        }

        // Use first paragraph
        return ExtractFirstParagraph(text);
    }

    private List<Issue> ExtractIssues(string text)
    {
        var issues = new List<Issue>();

        // Look for issues section
        var issuesSection = Regex.Match(
            text,
            @"(?:Issues?|Problems?|Errors?)[:\s]*(.*?)(?=##|$)",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        if (!issuesSection.Success)
            return issues;

        var issuesText = issuesSection.Groups[1].Value;

        // Extract individual issues (numbered or bulleted)
        var issueMatches = Regex.Matches(
            issuesText,
            @"(?:^|\n)(?:\d+\.|[-*])\s*\*?\*?(.*?)(?=\n(?:\d+\.|[-*])|\n\n|$)",
            RegexOptions.Singleline | RegexOptions.Multiline);

        foreach (Match match in issueMatches)
        {
            var issueText = match.Groups[1].Value.Trim();
            if (string.IsNullOrWhiteSpace(issueText))
                continue;

            var lines = issueText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var title = lines[0].Trim().TrimStart('*', ' ');

            var issue = new Issue
            {
                Title = title,
                Description = issueText,
                Severity = DetectSeverity(issueText),
                Category = DetectCategory(issueText)
            };

            // Extract suggested fixes
            var fixesMatch = Regex.Match(
                issueText,
                @"(?:Fix|Solution|Recommendation)[:\s]*(.*?)$",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);

            if (fixesMatch.Success)
            {
                issue.SuggestedFixes.Add(fixesMatch.Groups[1].Value.Trim());
            }

            issues.Add(issue);
        }

        return issues;
    }

    private List<Recommendation> ExtractRecommendations(string text)
    {
        var recommendations = new List<Recommendation>();

        // Look for recommendations section
        var recSection = Regex.Match(
            text,
            @"(?:Recommendations?|Suggestions?|Fixes?)[:\s]*(.*?)(?=##|$)",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        if (!recSection.Success)
            return recommendations;

        var recText = recSection.Groups[1].Value;

        // Extract individual recommendations
        var recommendationMatches = Regex.Matches(
            recText,
            @"(?:^|\n)(?:\d+\.|[-*])\s*\*?\*?(.*?)(?=\n(?:\d+\.|[-*])|\n\n|$)",
            RegexOptions.Singleline | RegexOptions.Multiline);

        foreach (System.Text.RegularExpressions.Match match in recommendationMatches)
        {
            var recommendationText = match.Groups[1].Value.Trim();
            if (string.IsNullOrWhiteSpace(recommendationText))
                continue;

            var lines = recommendationText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var title = lines[0].Trim().TrimStart('*', ' ');

            var rec = new Recommendation
            {
                Title = title,
                Description = recommendationText,
                Type = DetectRecommendationType(recommendationText),
                Priority = DetectPriority(recommendationText)
            };

            // Extract code example
            var codeMatch = Regex.Match(recommendationText, @"```(?:\w+)?\s*(.*?)\s*```", RegexOptions.Singleline);
            if (codeMatch.Success)
            {
                rec.Rationale = codeMatch.Groups[1].Value.Trim();
            }

            recommendations.Add(rec);
        }

        return recommendations;
    }

    private PerformanceAssessment? ExtractPerformanceAssessment(string text)
    {
        // Look for performance section
        var perfMatch = Regex.Match(
            text,
            @"(?:Performance|Grade|Score)[:\s]*(.*?)(?=\n\n|##|$)",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        if (!perfMatch.Success)
            return null;

        var perfText = perfMatch.Groups[1].Value;
        var assessment = new PerformanceAssessment();

        // Extract grade
        var gradeMatch = Regex.Match(perfText, @"(?:Grade|Score)[:\s]*([A-F])", RegexOptions.IgnoreCase);
        if (gradeMatch.Success)
        {
            assessment.Grade = Enum.Parse<PerformanceGrade>(gradeMatch.Groups[1].Value, true);
        }

        // Extract numeric score
        var scoreMatch = Regex.Match(perfText, @"(?:Score)[:\s]*(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
        if (scoreMatch.Success)
        {
            assessment.OverallScore = double.Parse(scoreMatch.Groups[1].Value);
        }

        return assessment;
    }

    #endregion

    #region Helper Methods

    private string ExtractFirstParagraph(string text)
    {
        var paragraphs = text.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        return paragraphs.FirstOrDefault()?.Trim() ?? "No summary available";
    }

    private IssueSeverity DetectSeverity(string text)
    {
        text = text.ToLowerInvariant();
        
        if (text.Contains("critical") || text.Contains("severe") || text.Contains("blocking"))
            return IssueSeverity.Critical;
        
        if (text.Contains("high") || text.Contains("important") || text.Contains("major"))
            return IssueSeverity.High;
        
        if (text.Contains("low") || text.Contains("minor") || text.Contains("trivial"))
            return IssueSeverity.Low;
        
        return IssueSeverity.Medium;
    }

    private IssueCategory DetectCategory(string text)
    {
        text = text.ToLowerInvariant();
        
        if (text.Contains("javascript") || text.Contains("js error") || text.Contains("undefined"))
            return IssueCategory.JavaScriptError;
        
        if (text.Contains("network") || text.Contains("request") || text.Contains("api"))
            return IssueCategory.NetworkError;
        
        if (text.Contains("performance") || text.Contains("slow") || text.Contains("load time"))
            return IssueCategory.PerformanceIssue;
        
        if (text.Contains("security") || text.Contains("xss") || text.Contains("csrf"))
            return IssueCategory.SecurityVulnerability;
        
        if (text.Contains("ui") || text.Contains("display") || text.Contains("render"))
            return IssueCategory.Other;
        
        return IssueCategory.Other;
    }

    private RecommendationType DetectRecommendationType(string text)
    {
        text = text.ToLowerInvariant();
        
        if (text.Contains("performance") || text.Contains("optimize") || text.Contains("speed"))
            return RecommendationType.Performance;
        
        if (text.Contains("security") || text.Contains("vulnerability") || text.Contains("safe"))
            return RecommendationType.Security;
        
        if (text.Contains("refactor") || text.Contains("code quality") || text.Contains("clean"))
            return RecommendationType.CodeOptimization;
        
        if (text.Contains("architecture") || text.Contains("design pattern") || text.Contains("structure"))
            return RecommendationType.BestPractice;
        
        return RecommendationType.BestPractice;
    }

    private RecommendationPriority DetectPriority(string text)
    {
        text = text.ToLowerInvariant();
        
        if (text.Contains("critical") || text.Contains("urgent") || text.Contains("immediately"))
            return RecommendationPriority.Critical;
        
        if (text.Contains("high") || text.Contains("important") || text.Contains("soon"))
            return RecommendationPriority.High;
        
        if (text.Contains("low") || text.Contains("optional") || text.Contains("nice to have"))
            return RecommendationPriority.Low;
        
        return RecommendationPriority.Medium;
    }

    #endregion
}
