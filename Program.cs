using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using AIDebugPro.Services.DependencyInjection;
using AIDebugPro.Services.Configuration;
using LoggerConfig = AIDebugPro.Services.Logging.LoggerConfiguration;
using AIDebugPro.Presentation.Forms;

namespace AIDebugPro;

internal static class Program
{
    private static IHost? _host;
    private static IServiceProvider? _serviceProvider;

    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // Ensure log directory exists
        LoggerConfig.EnsureLogDirectoryExists();

        try
        {
            // Build configuration
            var configuration = BuildConfiguration();

            // Configure Serilog
            Log.Logger = LoggerConfig.ConfigureSerilog(configuration);
            
            Log.Information("Starting AIDebugPro application...");
            Log.Information("Application Version: {Version}", "1.0.0");

            // Build host with dependency injection
            _host = CreateHostBuilder(configuration).Build();
            _serviceProvider = _host.Services;

            // Validate service registration
            ServiceRegistration.ValidateServiceRegistration(_serviceProvider,configuration);

            Log.Information("Service registration validated successfully");

            // Configure Windows Forms
            ApplicationConfiguration.Initialize();
            
            Log.Information("Launching main form...");

            //Get MainForm
            var mainForm = _serviceProvider.GetRequiredService<MainForm>();


            Application.Run(mainForm);

            Log.Information("Application shutdown completed");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            MessageBox.Show(
                $"A fatal error occurred: {ex.Message}\n\nPlease check the logs for more details.",
                "Fatal Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            Log.CloseAndFlush();
            _host?.Dispose();
        }
    }

    /// <summary>
    /// Builds the application configuration from appsettings.json
    /// </summary>
    private static IConfiguration BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("Services/Configuration/appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"Services/Configuration/appsettings.{Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "Production"}.json", optional: true)
            .AddEnvironmentVariables()
            .AddUserSecrets<AppSettings>(optional: true);

        return builder.Build();
    }

    /// <summary>
    /// Creates the host builder with dependency injection
    /// </summary>
    private static IHostBuilder CreateHostBuilder(IConfiguration configuration)
    {
        return Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices((context, services) =>
            {
                // Register all application services
                services.AddAIDebugProServices(configuration);

                // Register view models
                services.AddViewModels();

                // Configure strongly-typed settings
                services.ConfigureAppSettings(configuration);

            });
    }

    /// <summary>
    /// Gets a service from the DI container
    /// </summary>
    /// <typeparam name="T">The service type</typeparam>
    /// <returns>The service instance</returns>
    public static T? GetService<T>() where T : class
    {
        return _serviceProvider?.GetService<T>();
    }

    /// <summary>
    /// Gets a required service from the DI container
    /// </summary>
    /// <typeparam name="T">The service type</typeparam>
    /// <returns>The service instance</returns>
    /// <exception cref="InvalidOperationException">If service is not registered</exception>
    public static T GetRequiredService<T>() where T : class
    {
        if (_serviceProvider == null)
            throw new InvalidOperationException("Service provider is not initialized");

        return _serviceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Creates a new service scope
    /// </summary>
    /// <returns>A new service scope</returns>
    public static IServiceScope CreateScope()
    {
        if (_serviceProvider == null)
            throw new InvalidOperationException("Service provider is not initialized");

        return _serviceProvider.CreateScope();
    }
}