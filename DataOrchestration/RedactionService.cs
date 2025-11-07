using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using AIDebugPro.Core.Interfaces;

namespace AIDebugPro.DataOrchestration;

/// <summary>
/// Provides advanced redaction of sensitive data from text content
/// </summary>
public class RedactionService
{
    private readonly ILogger<RedactionService>? _logger;
    private readonly Dictionary<string, Regex> _compiledPatterns;
    private readonly RedactionOptions _defaultOptions;

    public RedactionService(ILogger<RedactionService>? logger = null)
    {
        _logger = logger;
        _compiledPatterns = new Dictionary<string, Regex>();
        _defaultOptions = new RedactionOptions
        {
            RedactApiKeys = true,
            RedactTokens = true,
            RedactPasswords = true,
            RedactEmails = true,
            RedactPhoneNumbers = true,
            RedactUrls = false
        };

        InitializeCompiledPatterns();
    }

    /// <summary>
    /// Redacts sensitive data using default options
    /// </summary>
    public string Redact(string content)
    {
        return Redact(content, _defaultOptions);
    }

    /// <summary>
    /// Redacts sensitive data using specified options
    /// </summary>
    public string Redact(string content, RedactionOptions options)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        var redacted = content;
        var redactionCount = 0;

        // API Keys and Access Tokens
        if (options.RedactApiKeys)
        {
            redacted = ApplyPattern(redacted, "ApiKey", ref redactionCount);
            redacted = ApplyPattern(redacted, "AccessToken", ref redactionCount);
            redacted = ApplyPattern(redacted, "SecretKey", ref redactionCount);
        }

        // Bearer Tokens and Auth Headers
        if (options.RedactTokens)
        {
            redacted = ApplyPattern(redacted, "BearerToken", ref redactionCount);
            redacted = ApplyPattern(redacted, "JwtToken", ref redactionCount);
            redacted = ApplyPattern(redacted, "AuthToken", ref redactionCount);
        }

        // Passwords
        if (options.RedactPasswords)
        {
            redacted = ApplyPattern(redacted, "Password", ref redactionCount);
        }

        // Email Addresses
        if (options.RedactEmails)
        {
            redacted = ApplyPattern(redacted, "Email", ref redactionCount);
        }

        // Phone Numbers
        if (options.RedactPhoneNumbers)
        {
            redacted = ApplyPattern(redacted, "PhoneUS", ref redactionCount);
            redacted = ApplyPattern(redacted, "PhoneInternational", ref redactionCount);
        }

        // URLs
        if (options.RedactUrls)
        {
            redacted = ApplyPattern(redacted, "Url", ref redactionCount);
        }

        // Credit Card Numbers
        redacted = ApplyPattern(redacted, "CreditCard", ref redactionCount);

        // Social Security Numbers
        redacted = ApplyPattern(redacted, "SSN", ref redactionCount);

        // IPv4 Addresses
        redacted = ApplyPattern(redacted, "IPv4", ref redactionCount);

        // Custom Patterns
        foreach (var pattern in options.CustomPatterns)
        {
            try
            {
                redacted = Regex.Replace(
                    redacted,
                    pattern,
                    "[REDACTED]",
                    RegexOptions.IgnoreCase | RegexOptions.Multiline,
                    TimeSpan.FromMilliseconds(100));
                redactionCount++;
            }
            catch (RegexMatchTimeoutException)
            {
                _logger?.LogWarning("Regex timeout for custom pattern: {Pattern}", pattern);
            }
            catch (ArgumentException ex)
            {
                _logger?.LogWarning(ex, "Invalid custom regex pattern: {Pattern}", pattern);
            }
        }

        if (redactionCount > 0)
        {
            _logger?.LogInformation("Redacted {Count} sensitive data patterns", redactionCount);
        }

        return redacted;
    }

    /// <summary>
    /// Redacts sensitive data from multiple strings
    /// </summary>
    public List<string> RedactMultiple(IEnumerable<string> contents, RedactionOptions? options = null)
    {
        var opts = options ?? _defaultOptions;
        return contents.Select(c => Redact(c, opts)).ToList();
    }

    /// <summary>
    /// Redacts sensitive data from a dictionary of values
    /// </summary>
    public Dictionary<string, string> RedactDictionary(
        Dictionary<string, string> data,
        RedactionOptions? options = null)
    {
        var opts = options ?? _defaultOptions;
        var redacted = new Dictionary<string, string>();

        foreach (var kvp in data)
        {
            redacted[kvp.Key] = Redact(kvp.Value, opts);
        }

        return redacted;
    }

    /// <summary>
    /// Analyzes content and returns information about detected sensitive data
    /// </summary>
    public SensitiveDataReport AnalyzeContent(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return new SensitiveDataReport();
        }

        var report = new SensitiveDataReport
        {
            ContentLength = content.Length
        };

        // Check each pattern type
        foreach (var patternName in _compiledPatterns.Keys)
        {
            var matches = _compiledPatterns[patternName].Matches(content);
            if (matches.Count > 0)
            {
                report.DetectedPatterns.Add(new SensitiveDataDetection
                {
                    PatternType = patternName,
                    MatchCount = matches.Count,
                    Severity = GetSeverityForPattern(patternName),
                    SampleMatches = matches.Take(3).Select(m => MaskSample(m.Value)).ToList()
                });
            }
        }

        report.TotalMatches = report.DetectedPatterns.Sum(p => p.MatchCount);
        report.RiskLevel = CalculateRiskLevel(report);

        _logger?.LogDebug(
            "Analyzed content: {Length} chars, {Matches} sensitive data matches, {Risk} risk",
            content.Length,
            report.TotalMatches,
            report.RiskLevel);

        return report;
    }

    /// <summary>
    /// Creates a redaction summary showing what was redacted
    /// </summary>
    public string CreateRedactionSummary(string original, string redacted)
    {
        var summary = new StringBuilder();
        summary.AppendLine("=== Redaction Summary ===");
        summary.AppendLine($"Original Length: {original.Length} characters");
        summary.AppendLine($"Redacted Length: {redacted.Length} characters");

        var report = AnalyzeContent(original);
        summary.AppendLine($"Total Sensitive Patterns: {report.TotalMatches}");
        summary.AppendLine($"Risk Level: {report.RiskLevel}");
        summary.AppendLine();

        if (report.DetectedPatterns.Any())
        {
            summary.AppendLine("Detected Patterns:");
            foreach (var pattern in report.DetectedPatterns.OrderByDescending(p => p.Severity))
            {
                summary.AppendLine($"  • {pattern.PatternType}: {pattern.MatchCount} matches (Severity: {pattern.Severity})");
                if (pattern.SampleMatches.Any())
                {
                    summary.AppendLine($"    Samples: {string.Join(", ", pattern.SampleMatches)}");
                }
            }
        }

        return summary.ToString();
    }

    #region Private Helper Methods

    /// <summary>
    /// Initializes compiled regex patterns for better performance
    /// </summary>
    private void InitializeCompiledPatterns()
    {
        // API Keys and Secrets
        AddPattern("ApiKey", 
            @"(?i)(api[_-]?key|apikey|api[_-]?secret)[=:\s]+['""]?([\w\-]{20,})['""]?");
        
        AddPattern("AccessToken", 
            @"(?i)(access[_-]?token|access[_-]?key)[=:\s]+['""]?([\w\-\.]{20,})['""]?");
        
        AddPattern("SecretKey", 
            @"(?i)(secret[_-]?key|client[_-]?secret)[=:\s]+['""]?([\w\-]{20,})['""]?");

        // Tokens
        AddPattern("BearerToken", 
            @"(?i)bearer\s+([A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+)");
        
        AddPattern("JwtToken", 
            @"eyJ[A-Za-z0-9\-_]+\.eyJ[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+");
        
        AddPattern("AuthToken", 
            @"(?i)(auth[_-]?token|x-auth-token)[=:\s]+['""]?([\w\-]{20,})['""]?");

        // Passwords
        AddPattern("Password", 
            @"(?i)(password|passwd|pwd)[=:\s]+['""]?([^'"";\s]{6,})['""]?");

        // Email
        AddPattern("Email", 
            @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b");

        // Phone Numbers
        AddPattern("PhoneUS", 
            @"\b(\+?1[-.\s]?)?\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}\b");
        
        AddPattern("PhoneInternational", 
            @"\+\d{1,3}[-.\s]?\d{1,4}[-.\s]?\d{1,4}[-.\s]?\d{1,9}");

        // URLs
        AddPattern("Url", 
            @"https?://[^\s<>""]+");

        // Credit Cards
        AddPattern("CreditCard", 
            @"\b\d{4}[-\s]?\d{4}[-\s]?\d{4}[-\s]?\d{4}\b");

        // SSN
        AddPattern("SSN", 
            @"\b\d{3}-\d{2}-\d{4}\b");

        // IPv4
        AddPattern("IPv4", 
            @"\b(?:\d{1,3}\.){3}\d{1,3}\b");

        // AWS Keys
        AddPattern("AWSKey", 
            @"(?i)AKIA[0-9A-Z]{16}");

        // Private Keys
        AddPattern("PrivateKey", 
            @"-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----[\s\S]*?-----END (?:RSA |EC |OPENSSH )?PRIVATE KEY-----");

        _logger?.LogDebug("Initialized {Count} redaction patterns", _compiledPatterns.Count);
    }

    /// <summary>
    /// Adds a compiled regex pattern
    /// </summary>
    private void AddPattern(string name, string pattern)
    {
        try
        {
            _compiledPatterns[name] = new Regex(
                pattern,
                RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline,
                TimeSpan.FromMilliseconds(100));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to compile pattern {Name}: {Pattern}", name, pattern);
        }
    }

    /// <summary>
    /// Applies a named pattern and returns redacted content
    /// </summary>
    private string ApplyPattern(string content, string patternName, ref int redactionCount)
    {
        if (!_compiledPatterns.ContainsKey(patternName))
            return content;

        try
        {
            var pattern = _compiledPatterns[patternName];
            var matches = pattern.Matches(content);
            
            if (matches.Count > 0)
            {
                redactionCount += matches.Count;
                content = pattern.Replace(content, GetReplacementText(patternName));
            }
        }
        catch (RegexMatchTimeoutException)
        {
            _logger?.LogWarning("Regex timeout for pattern: {Pattern}", patternName);
        }

        return content;
    }

    /// <summary>
    /// Gets appropriate replacement text for a pattern type
    /// </summary>
    private string GetReplacementText(string patternName)
    {
        return patternName switch
        {
            "ApiKey" => "[API_KEY_REDACTED]",
            "AccessToken" => "[ACCESS_TOKEN_REDACTED]",
            "SecretKey" => "[SECRET_KEY_REDACTED]",
            "BearerToken" => "[BEARER_TOKEN_REDACTED]",
            "JwtToken" => "[JWT_REDACTED]",
            "AuthToken" => "[AUTH_TOKEN_REDACTED]",
            "Password" => "[PASSWORD_REDACTED]",
            "Email" => "[EMAIL_REDACTED]",
            "PhoneUS" => "[PHONE_REDACTED]",
            "PhoneInternational" => "[PHONE_REDACTED]",
            "Url" => "[URL_REDACTED]",
            "CreditCard" => "[CARD_REDACTED]",
            "SSN" => "[SSN_REDACTED]",
            "IPv4" => "[IP_REDACTED]",
            "AWSKey" => "[AWS_KEY_REDACTED]",
            "PrivateKey" => "[PRIVATE_KEY_REDACTED]",
            _ => "[REDACTED]"
        };
    }

    /// <summary>
    /// Gets severity level for a pattern type
    /// </summary>
    private SensitiveDataSeverity GetSeverityForPattern(string patternName)
    {
        return patternName switch
        {
            "ApiKey" => SensitiveDataSeverity.Critical,
            "SecretKey" => SensitiveDataSeverity.Critical,
            "PrivateKey" => SensitiveDataSeverity.Critical,
            "AWSKey" => SensitiveDataSeverity.Critical,
            "Password" => SensitiveDataSeverity.High,
            "BearerToken" => SensitiveDataSeverity.High,
            "JwtToken" => SensitiveDataSeverity.High,
            "CreditCard" => SensitiveDataSeverity.High,
            "SSN" => SensitiveDataSeverity.High,
            "Email" => SensitiveDataSeverity.Medium,
            "PhoneUS" => SensitiveDataSeverity.Medium,
            "PhoneInternational" => SensitiveDataSeverity.Medium,
            "AccessToken" => SensitiveDataSeverity.Medium,
            "Url" => SensitiveDataSeverity.Low,
            "IPv4" => SensitiveDataSeverity.Low,
            _ => SensitiveDataSeverity.Low
        };
    }

    /// <summary>
    /// Masks a sample for display
    /// </summary>
    private string MaskSample(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.Length <= 8)
            return new string('*', value.Length);

        var visibleChars = Math.Min(4, value.Length / 4);
        var start = value.Substring(0, visibleChars);
        var end = value.Substring(value.Length - visibleChars);
        var masked = new string('*', value.Length - (visibleChars * 2));
        
        return $"{start}{masked}{end}";
    }

    /// <summary>
    /// Calculates overall risk level based on detected patterns
    /// </summary>
    private RiskLevel CalculateRiskLevel(SensitiveDataReport report)
    {
        if (report.DetectedPatterns.Any(p => p.Severity == SensitiveDataSeverity.Critical))
            return RiskLevel.Critical;

        if (report.DetectedPatterns.Any(p => p.Severity == SensitiveDataSeverity.High))
            return RiskLevel.High;

        if (report.DetectedPatterns.Any(p => p.Severity == SensitiveDataSeverity.Medium))
            return RiskLevel.Medium;

        if (report.DetectedPatterns.Any())
            return RiskLevel.Low;

        return RiskLevel.None;
    }

    #endregion
}

#region Supporting Classes

/// <summary>
/// Report of sensitive data detected in content
/// </summary>
public class SensitiveDataReport
{
    public int ContentLength { get; set; }
    public int TotalMatches { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public List<SensitiveDataDetection> DetectedPatterns { get; set; } = new();
}

/// <summary>
/// Details of a specific sensitive data detection
/// </summary>
public class SensitiveDataDetection
{
    public string PatternType { get; set; } = string.Empty;
    public int MatchCount { get; set; }
    public SensitiveDataSeverity Severity { get; set; }
    public List<string> SampleMatches { get; set; } = new();
}

/// <summary>
/// Severity level for sensitive data
/// </summary>
public enum SensitiveDataSeverity
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Overall risk level
/// </summary>
public enum RiskLevel
{
    None,
    Low,
    Medium,
    High,
    Critical
}

#endregion
