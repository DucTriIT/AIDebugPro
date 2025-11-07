using Microsoft.Extensions.Logging;
using AIDebugPro.Persistence.Database;

namespace AIDebugPro.Persistence.Repositories;

/// <summary>
/// Repository for application settings
/// </summary>
public class SettingsRepository
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<SettingsRepository>? _logger;

    public SettingsRepository(AppDbContext dbContext, ILogger<SettingsRepository>? logger = null)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger;
    }

    #region Get Settings

    /// <summary>
    /// Gets a setting value by key
    /// </summary>
    public async Task<string?> GetSettingAsync(string key)
    {
        return await Task.Run(() =>
        {
            var setting = _dbContext.Settings.FindOne(s => s.Key == key);
            return setting?.Value;
        });
    }

    /// <summary>
    /// Gets a setting with a default value
    /// </summary>
    public async Task<string> GetSettingAsync(string key, string defaultValue)
    {
        var value = await GetSettingAsync(key);
        return value ?? defaultValue;
    }

    /// <summary>
    /// Gets a strongly-typed setting
    /// </summary>
    public async Task<T?> GetSettingAsync<T>(string key) where T : struct
    {
        var value = await GetSettingAsync(key);
        if (string.IsNullOrEmpty(value))
            return null;

        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets all settings in a category
    /// </summary>
    public async Task<Dictionary<string, string>> GetSettingsByCategoryAsync(string category)
    {
        return await Task.Run(() =>
        {
            var settings = _dbContext.Settings
                .Find(s => s.Category == category)
                .ToDictionary(s => s.Key, s => s.Value);

            _logger?.LogDebug("Retrieved {Count} settings in category {Category}",
                settings.Count, category);

            return settings;
        });
    }

    /// <summary>
    /// Gets all settings
    /// </summary>
    public async Task<Dictionary<string, string>> GetAllSettingsAsync()
    {
        return await Task.Run(() =>
        {
            return _dbContext.Settings
                .FindAll()
                .ToDictionary(s => s.Key, s => s.Value);
        });
    }

    #endregion

    #region Set Settings

    /// <summary>
    /// Sets a setting value
    /// </summary>
    public async Task SetSettingAsync(string key, string value, string? category = null)
    {
        await Task.Run(() =>
        {
            var setting = _dbContext.Settings.FindOne(s => s.Key == key);

            if (setting == null)
            {
                setting = new DbSetting
                {
                    Key = key,
                    Value = value,
                    Category = category,
                    UpdatedAt = DateTime.UtcNow
                };
            }
            else
            {
                setting.Value = value;
                if (category != null)
                    setting.Category = category;
                setting.UpdatedAt = DateTime.UtcNow;
            }

            _dbContext.Settings.Upsert(setting);

            _logger?.LogDebug("Set setting {Key} = {Value}", key, value);
        });
    }

    /// <summary>
    /// Sets multiple settings
    /// </summary>
    public async Task SetSettingsAsync(Dictionary<string, string> settings, string? category = null)
    {
        foreach (var kvp in settings)
        {
            await SetSettingAsync(kvp.Key, kvp.Value, category);
        }
    }

    #endregion

    #region Delete Settings

    /// <summary>
    /// Deletes a setting
    /// </summary>
    public async Task<bool> DeleteSettingAsync(string key)
    {
        return await Task.Run(() =>
        {
            var count = _dbContext.Settings.DeleteMany(s => s.Key == key);
            
            if (count > 0)
            {
                _logger?.LogDebug("Deleted setting {Key}", key);
            }

            return count > 0;
        });
    }

    /// <summary>
    /// Deletes all settings in a category
    /// </summary>
    public async Task<int> DeleteCategoryAsync(string category)
    {
        return await Task.Run(() =>
        {
            var count = _dbContext.Settings.DeleteMany(s => s.Category == category);

            _logger?.LogInformation("Deleted {Count} settings in category {Category}",
                count, category);

            return count;
        });
    }

    #endregion

    #region Predefined Settings

    /// <summary>
    /// Gets OpenAI API key
    /// </summary>
    public Task<string?> GetOpenAIApiKeyAsync() =>
        GetSettingAsync("OpenAI.ApiKey");

    /// <summary>
    /// Sets OpenAI API key
    /// </summary>
    public Task SetOpenAIApiKeyAsync(string apiKey) =>
        SetSettingAsync("OpenAI.ApiKey", apiKey, "OpenAI");

    /// <summary>
    /// Gets OpenAI model
    /// </summary>
    public Task<string> GetOpenAIModelAsync() =>
        GetSettingAsync("OpenAI.Model", "gpt-4");

    /// <summary>
    /// Sets OpenAI model
    /// </summary>
    public Task SetOpenAIModelAsync(string model) =>
        SetSettingAsync("OpenAI.Model", model, "OpenAI");

    /// <summary>
    /// Gets telemetry settings
    /// </summary>
    public async Task<TelemetrySettings> GetTelemetrySettingsAsync()
    {
        return new TelemetrySettings
        {
            MaxConsoleMessages = await GetSettingAsync<int>("Telemetry.MaxConsoleMessages") ?? 1000,
            MaxNetworkRequests = await GetSettingAsync<int>("Telemetry.MaxNetworkRequests") ?? 500,
            AutoCaptureEnabled = await GetSettingAsync<bool>("Telemetry.AutoCaptureEnabled") ?? true,
            AutoCaptureIntervalSeconds = await GetSettingAsync<int>("Telemetry.AutoCaptureIntervalSeconds") ?? 30
        };
    }

    /// <summary>
    /// Sets telemetry settings
    /// </summary>
    public async Task SetTelemetrySettingsAsync(TelemetrySettings settings)
    {
        await SetSettingAsync("Telemetry.MaxConsoleMessages", settings.MaxConsoleMessages.ToString(), "Telemetry");
        await SetSettingAsync("Telemetry.MaxNetworkRequests", settings.MaxNetworkRequests.ToString(), "Telemetry");
        await SetSettingAsync("Telemetry.AutoCaptureEnabled", settings.AutoCaptureEnabled.ToString(), "Telemetry");
        await SetSettingAsync("Telemetry.AutoCaptureIntervalSeconds", settings.AutoCaptureIntervalSeconds.ToString(), "Telemetry");
    }

    #endregion
}

#region Settings Models

/// <summary>
/// Telemetry settings model
/// </summary>
public class TelemetrySettings
{
    public int MaxConsoleMessages { get; set; }
    public int MaxNetworkRequests { get; set; }
    public bool AutoCaptureEnabled { get; set; }
    public int AutoCaptureIntervalSeconds { get; set; }
}

#endregion
