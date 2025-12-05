using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using AIDebugPro.Core.Models;
using AIDebugPro.Core.Interfaces;
using AIDebugPro.Core.Constants;
using AIDebugPro.Core.Exceptions;
using AIDebugPro.AIIntegration.Interfaces;

namespace AIDebugPro.AIIntegration.Clients;

/// <summary>
/// OpenAI API client for GPT-4/5 integration
/// </summary>
public class OpenAIClient : IAIClient
{
    private readonly HttpClient _httpClient;
    private readonly IContextBuilder _contextBuilder;
    private readonly ILogger<OpenAIClient>? _logger;
    private readonly string? _apiKey;
    private readonly string _baseUrl;

    public string ProviderName => "OpenAI";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    public OpenAIClient(
        string? apiKey,
        IContextBuilder contextBuilder,
        ILogger<OpenAIClient>? logger = null,
        string? baseUrl = null)
    {
        _apiKey = apiKey;
        _contextBuilder = contextBuilder ?? throw new ArgumentNullException(nameof(contextBuilder));
        _logger = logger;
        _baseUrl = baseUrl ?? "https://api.openai.com/v1";

        _httpClient = new HttpClient();
        if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }
        _httpClient.Timeout = TimeSpan.FromMinutes(2);
    }

    #region Analyze Methods

    /// <summary>
    /// Analyzes telemetry with custom prompt
    /// </summary>
    public async Task<AIAnalysisResult> AnalyzeAsync(
        string prompt,
        AIAnalysisOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            throw new AIProviderConfigurationException("OpenAI API key not configured");

        options ??= new AIAnalysisOptions();

        var startTime = DateTime.UtcNow;
        var result = new AIAnalysisResult
        {
            Id = Guid.NewGuid(),
            AnalyzedAt = startTime,
            Model = options.Model,
            Status = AIAnalysisStatus.Pending
        };

        try
        {
            _logger?.LogInformation("Starting AI analysis with model {Model}", options.Model);

            // Build request
            var request = BuildChatCompletionRequest(prompt, options);
            var requestJson = JsonSerializer.Serialize(request);
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            // Call OpenAI API
            var response = await _httpClient.PostAsync(
                $"{_baseUrl}/chat/completions",
                content,
                cancellationToken);

            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger?.LogError("OpenAI API error: {StatusCode} - {Response}",
                    response.StatusCode, responseText);
                
                // Parse error details
                string errorMessage = $"OpenAI API error: {response.StatusCode}";
                try
                {
                    var errorResponse = JsonSerializer.Deserialize<JsonDocument>(responseText);
                    if (errorResponse != null && errorResponse.RootElement.TryGetProperty("error", out var error))
                    {
                        if (error.TryGetProperty("message", out var message))
                        {
                            errorMessage = $"OpenAI API error: {message.GetString()}";
                        }
                    }
                }
                catch { }
                
                // Provide helpful messages for common errors
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    errorMessage += "\n\n?? This usually means:\n" +
                        "- The model 'gpt-4' doesn't exist or you don't have access to it\n" +
                        "- Try changing the model to 'gpt-3.5-turbo' in appsettings.json\n" +
                        "- Or check if your OpenAI account has GPT-4 API access";
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    errorMessage += "\n\n?? Your API key is invalid or expired";
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    errorMessage += "\n\n?? Rate limit exceeded. Wait a moment and try again";
                }
                
                throw new AIAnalysisException(
                    errorMessage,
                    options.Model,
                    0);
            }

            // Parse response
            var apiResponse = JsonSerializer.Deserialize<OpenAIResponse>(responseText);
            if (apiResponse == null || apiResponse.Choices == null || !apiResponse.Choices.Any())
            {
                throw new AIAnalysisException("Invalid response from OpenAI", options.Model, 0);
            }

            var choice = apiResponse.Choices.First();
            var analysisText = choice.Message?.Content ?? "";

            // Parse analysis result
            result = ParseAnalysisResult(analysisText, apiResponse, options.Model);
            result.AnalysisDurationMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
            result.Status = AIAnalysisStatus.Completed;

            _logger?.LogInformation(
                "AI analysis completed in {Duration}ms, {Tokens} tokens used",
                result.AnalysisDurationMs,
                result.TokensUsed);

            return result;
        }
        catch (Exception ex) when (ex is not AIAnalysisException)
        {
            _logger?.LogError(ex, "AI analysis failed");
            result.Status = AIAnalysisStatus.Failed;
            throw new AIAnalysisException("AI analysis failed", options.Model, 0);
        }
    }

    /// <summary>
    /// Analyzes telemetry snapshot
    /// </summary>
    public async Task<AIAnalysisResult> AnalyzeSnapshotAsync(
        TelemetrySnapshot snapshot,
        AIAnalysisOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new AIAnalysisOptions();

        // Build prompt from snapshot
        var prompt = await _contextBuilder.BuildPromptContextAsync(snapshot, options);

        // Analyze
        var result = await AnalyzeAsync(prompt, options, cancellationToken);
        result.SessionId = snapshot.SessionId;

        return result;
    }

    #endregion

    #region Chat Methods

    /// <summary>
    /// Sends a chat message
    /// </summary>
    public async Task<string> ChatAsync(
        string message,
        string? conversationId = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            throw new AIProviderConfigurationException("OpenAI API key not configured");

        try
        {
            var request = new
            {
                model = "gpt-4-turbo-preview",
                messages = new[]
                {
                    new { role = "system", content = AIConstants.SystemPromptPrefix },
                    new { role = "user", content = message }
                },
                temperature = 0.7,
                max_tokens = 1000
            };

            var requestJson = JsonSerializer.Serialize(request);
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"{_baseUrl}/chat/completions",
                content,
                cancellationToken);

            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new AIAnalysisException($"OpenAI API error: {response.StatusCode}", "gpt-4-turbo-preview", 0);
            }

            var apiResponse = JsonSerializer.Deserialize<OpenAIResponse>(responseText);
            return apiResponse?.Choices?.FirstOrDefault()?.Message?.Content ?? "";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Chat request failed");
            throw;
        }
    }

    #endregion

    #region Configuration Methods

    /// <summary>
    /// Gets available models
    /// </summary>
    public async Task<List<string>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return new List<string>();

        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/models", cancellationToken);
            
            if (!response.IsSuccessStatusCode)
                return GetDefaultModels();

            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
            var modelsResponse = JsonSerializer.Deserialize<ModelsResponse>(responseText);

            var models = modelsResponse?.Data?
                .Where(m => m.Id.Contains("gpt"))
                .Select(m => m.Id)
                .OrderByDescending(m => m)
                .ToList() ?? new List<string>();

            return models.Any() ? models : GetDefaultModels();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to fetch models, using defaults");
            return GetDefaultModels();
        }
    }

    /// <summary>
    /// Validates API connection
    /// </summary>
    public async Task<bool> ValidateConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return false;

        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/models", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Estimates token count (rough approximation)
    /// </summary>
    public int EstimateTokenCount(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        // Rough estimation: ~4 characters per token
        return (int)Math.Ceiling(text.Length / 4.0);
    }

    #endregion

    #region Helper Methods

    private object BuildChatCompletionRequest(string prompt, AIAnalysisOptions options)
    {
        var messages = new List<object>
        {
            new
            {
                role = "system",
                content = AIConstants.SystemPromptPrefix
            },
            new
            {
                role = "user",
                content = prompt
            }
        };

        return new
        {
            model = options.Model,
            messages = messages,
            temperature = options.Temperature,
            max_tokens = options.MaxTokens,
            top_p = 1.0,
            frequency_penalty = 0.0,
            presence_penalty = 0.0
        };
    }

    private AIAnalysisResult ParseAnalysisResult(string analysisText, OpenAIResponse apiResponse, string model)
    {
        var result = new AIAnalysisResult
        {
            Id = Guid.NewGuid(),
            AnalyzedAt = DateTime.UtcNow,
            Model = model,
            TokensUsed = apiResponse.Usage?.TotalTokens ?? 0,
            Status = AIAnalysisStatus.Completed
        };

        // Store raw response in metadata
        result.Metadata["rawResponse"] = analysisText;

        try
        {
            // Try to parse structured response
            // Format expected: JSON or markdown sections
            
            // Extract summary
            var summaryMatch = System.Text.RegularExpressions.Regex.Match(
                analysisText,
                @"(?:Summary|SUMMARY)[:\s]*(.*?)(?=\n\n|\n#|$)",
                System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            if (summaryMatch.Success)
            {
                result.Summary = summaryMatch.Groups[1].Value.Trim();
            }
            else
            {
                // Use first paragraph as summary
                var lines = analysisText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                result.Summary = lines.FirstOrDefault()?.Trim() ?? "AI analysis completed";
            }

            // Extract issues (simplified parsing)
            var issueMatches = System.Text.RegularExpressions.Regex.Matches(
                analysisText,
                @"(?:Issue|ERROR|Problem)[:\s]*(.*?)(?=\n(?:Issue|ERROR|Problem|Recommendation|$))",
                System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            foreach (System.Text.RegularExpressions.Match match in issueMatches)
            {
                var issueText = match.Groups[1].Value.Trim();
                if (!string.IsNullOrWhiteSpace(issueText))
                {
                    result.Issues.Add(new Issue
                    {
                        Title = issueText.Split('\n')[0].Trim(),
                        Description = issueText,
                        Severity = IssueSeverity.Medium,
                        Category = IssueCategory.Other
                    });
                }
            }

            // Extract recommendations
            var recommendationMatches = System.Text.RegularExpressions.Regex.Matches(
                analysisText,
                @"(?:Recommendation|Fix|Solution)[:\s]*(.*?)(?=\n(?:Recommendation|Fix|Solution|$))",
                System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            foreach (System.Text.RegularExpressions.Match match in recommendationMatches)
            {
                var recText = match.Groups[1].Value.Trim();
                if (!string.IsNullOrWhiteSpace(recText))
                {
                    result.Recommendations.Add(new Recommendation
                    {
                        Title = recText.Split('\n')[0].Trim(),
                        Description = recText,
                        Type = RecommendationType.BestPractice,
                        Priority = RecommendationPriority.Medium
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to parse structured analysis, using raw response");
        }

        return result;
    }

    private List<string> GetDefaultModels()
    {
        return new List<string>
        {
            "gpt-4-turbo-preview",
            "gpt-4",
            "gpt-4-32k",
            "gpt-3.5-turbo",
            "gpt-3.5-turbo-16k"
        };
    }

    #endregion
}

#region OpenAI API Models

internal class OpenAIResponse
{
    public string? Id { get; set; }
    public string? Object { get; set; }
    public long Created { get; set; }
    public string? Model { get; set; }
    public List<Choice>? Choices { get; set; }
    public Usage? Usage { get; set; }
}

internal class Choice
{
    public int Index { get; set; }
    public Message? Message { get; set; }
    public string? FinishReason { get; set; }
}

internal class Message
{
    public string? Role { get; set; }
    public string? Content { get; set; }
}

internal class Usage
{
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
}

internal class ModelsResponse
{
    public string? Object { get; set; }
    public List<ModelInfo>? Data { get; set; }
}

internal class ModelInfo
{
    public string Id { get; set; } = string.Empty;
    public string? Object { get; set; }
    public long Created { get; set; }
    public string? OwnedBy { get; set; }
}

#endregion
