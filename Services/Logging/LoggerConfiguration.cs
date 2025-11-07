using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using AIDebugPro.Core.Constants;

namespace AIDebugPro.Services.Logging;

/// <summary>
/// Configures Serilog logging for the application
/// </summary>
public static class LoggerConfiguration
{
    /// <summary>
    /// Configures Serilog with file and console sinks
    /// </summary>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Configured logger</returns>
    public static Serilog.ILogger ConfigureSerilog(IConfiguration configuration)
    {
        var logPath = configuration["Logging:Path"] ?? AppConstants.LogsFolder;
        var logLevel = configuration["Logging:LogLevel"] ?? "Information";
        var minimumLevel = ParseLogLevel(logLevel);

        var logger = new Serilog.LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithProcessId()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: Path.Combine(logPath, AppConstants.LogFileName),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                fileSizeLimitBytes: 100_000_000, // 100 MB
                rollOnFileSizeLimit: true)
            .CreateLogger();

        return logger;
    }

    /// <summary>
    /// Configures Serilog for development environment with verbose logging
    /// </summary>
    public static Serilog.ILogger ConfigureForDevelopment()
    {
        return new Serilog.LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: Path.Combine(AppConstants.LogsFolder, "debug-" + AppConstants.LogFileName),
                rollingInterval: RollingInterval.Hour,
                retainedFileCountLimit: 48)
            .CreateLogger();
    }

    /// <summary>
    /// Configures Serilog for production environment with optimized settings
    /// </summary>
    public static Serilog.ILogger ConfigureForProduction()
    {
        return new Serilog.LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .WriteTo.File(
                path: Path.Combine(AppConstants.LogsFolder, AppConstants.LogFileName),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 90,
                fileSizeLimitBytes: 500_000_000, // 500 MB
                rollOnFileSizeLimit: true,
                buffered: true,
                flushToDiskInterval: TimeSpan.FromSeconds(5))
            .CreateLogger();
    }

    /// <summary>
    /// Ensures log directory exists
    /// </summary>
    public static void EnsureLogDirectoryExists()
    {
        var logPath = AppConstants.LogsFolder;
        if (!Directory.Exists(logPath))
        {
            Directory.CreateDirectory(logPath);
        }
    }

    /// <summary>
    /// Parses log level string to LogEventLevel
    /// </summary>
    private static LogEventLevel ParseLogLevel(string logLevel)
    {
        return logLevel.ToLowerInvariant() switch
        {
            "verbose" => LogEventLevel.Verbose,
            "debug" => LogEventLevel.Debug,
            "information" => LogEventLevel.Information,
            "warning" => LogEventLevel.Warning,
            "error" => LogEventLevel.Error,
            "fatal" => LogEventLevel.Fatal,
            _ => LogEventLevel.Information
        };
    }

    /// <summary>
    /// Cleans up old log files beyond retention period
    /// </summary>
    public static void CleanupOldLogs(int retentionDays = 30)
    {
        try
        {
            var logPath = AppConstants.LogsFolder;
            if (!Directory.Exists(logPath))
                return;

            var cutoffDate = DateTime.Now.AddDays(-retentionDays);
            var logFiles = Directory.GetFiles(logPath, "*.txt");

            foreach (var file in logFiles)
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.CreationTime < cutoffDate)
                {
                    fileInfo.Delete();
                }
            }
        }
        catch (Exception ex)
        {
            // Log cleanup failure but don't throw
            Console.WriteLine($"Failed to cleanup old logs: {ex.Message}");
        }
    }
}
