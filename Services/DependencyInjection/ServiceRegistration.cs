using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AIDebugPro.Core.Interfaces;
using AIDebugPro.Core.Constants;
using System.IO;
using AIDebugPro.BrowserIntegration.DevToolsProtocol;
using AIDebugPro.Core.Exceptions;
using AIDebugPro.Presentation.ViewModels;
using AIDebugPro.Presentation.UserControls;

namespace AIDebugPro.Services.DependencyInjection;

/// <summary>
/// Provides extension methods for configuring dependency injection
/// </summary>
public static class ServiceRegistration
{
    /// <summary>
    /// Registers all application services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAIDebugProServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register core services
        services.AddCoreServices();

        // Register browser integration services
        services.AddBrowserIntegrationServices();

        // Register data orchestration services
        services.AddDataOrchestrationServices();

        // Register persistence services
        services.AddPersistenceServices(configuration);

        // Register AI integration services
        services.AddAIIntegrationServices(configuration);

        // Register infrastructure services
        services.AddInfrastructureServices();

        return services;
    }

    /// <summary>
    /// Registers core layer services (usually just interfaces, implementations in other layers)
    /// </summary>
    private static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        // Core layer typically only contains models, interfaces, and contracts
        // No concrete implementations to register here
        // Implementations will be registered in their respective layers
        return services;
    }

    /// <summary>
    /// Registers browser integration services (WebView2, CDP listeners)
    /// </summary>
    private static IServiceCollection AddBrowserIntegrationServices(this IServiceCollection services)
    {
        // WebView2 Host - Singleton since we typically have one browser instance
        // services.AddSingleton<IWebView2Host, WebView2Host>();

        // CDP Listeners - Scoped for each session
        services.AddScoped<IConsoleListener, ConsoleListener>();
        services.AddScoped<INetworkListener, NetworkListener>();
        services.AddScoped<IPerformanceCollector, PerformanceCollector>();
        services.AddScoped<IDOMSnapshotManager, DOMSnapshotManager>();


        // Script Executor
        //services.AddScoped<IScriptExecutor, ScriptExecutor>();

        // Event Processor
        //services.AddScoped<IEventProcessor, EventProcessor>();

        return services;
    }

    /// <summary>
    /// Registers data orchestration services (Telemetry, Session, Context)
    /// </summary>
    private static IServiceCollection AddDataOrchestrationServices(this IServiceCollection services)
    {
        // Telemetry Aggregator - Scoped per session
        services.AddScoped<ITelemetryAggregator, DataOrchestration.TelemetryAggregator>();

        // Session Manager - Singleton for application-wide session management
        services.AddSingleton<ISessionManager, DataOrchestration.SessionManager>();

        // Context Builder - Scoped for building AI contexts
        services.AddScoped<IContextBuilder, DataOrchestration.ContextBuilder>();

        // Data Normalizer - Scoped for data transformation
        services.AddScoped<DataOrchestration.DataNormalizer>();

        // Redaction Service - Singleton for pattern compilation efficiency
        services.AddSingleton<DataOrchestration.RedactionService>();

        return services;
    }

    /// <summary>
    /// Registers AI integration services (OpenAI, Prompts, Parsing)
    /// </summary>
    private static IServiceCollection AddAIIntegrationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Get OpenAI API key from configuration
        var apiKey = configuration[ConfigurationKeys.OpenAIApiKey];

        // AI Client - Scoped for per-request usage
        services.AddScoped<AIIntegration.Interfaces.IAIClient>(sp =>
        {
            var contextBuilder = sp.GetRequiredService<IContextBuilder>();
            var logger = sp.GetService<ILogger<AIIntegration.Clients.OpenAIClient>>();
            return new AIIntegration.Clients.OpenAIClient(apiKey, contextBuilder, logger);
        });

        // Prompt Composer - Scoped
        services.AddScoped<AIIntegration.PromptComposer>();

        // Response Parser - Scoped
        services.AddScoped<AIIntegration.ResponseParser>();

        // Token Manager - Singleton for caching
        services.AddSingleton<AIIntegration.TokenManager>();

        return services;
    }

    /// <summary>
    /// Registers persistence services (Database, Repositories)
    /// </summary>
    private static IServiceCollection AddPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database Context - Singleton for LiteDB
        var dbPath = configuration["Database:Path"] ?? Path.Combine("data", "aidebugpro.db");
        services.AddSingleton<Persistence.Database.AppDbContext>(sp =>
        {
            var logger = sp.GetService<ILogger<Persistence.Database.AppDbContext>>();
            return new Persistence.Database.AppDbContext(dbPath, logger);
        });

        // Repositories - Scoped for each request/session
        services.AddScoped<Persistence.Repositories.SessionRepository>();
        services.AddScoped<Persistence.Repositories.LogRepository>();
        services.AddScoped<Persistence.Repositories.SettingsRepository>();

        // Report Generator
        services.AddScoped<Persistence.ReportGenerator>();

        return services;
    }

    /// <summary>
    /// Registers infrastructure services (Logging, Background Tasks, Utilities)
    /// </summary>
    private static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Background Task Queue - Singleton for application-wide queue
        services.AddSingleton<BackgroundTasks.BackgroundTaskQueueOptions>();
        services.AddSingleton<BackgroundTasks.IBackgroundTaskQueue, BackgroundTasks.BackgroundTaskQueue>();

        // Background Task Service (hosted service) - Automatically starts/stops with application
        services.AddHostedService<BackgroundTasks.BackgroundTaskService>();

        // Utility Services - Singleton for application-wide utilities
        services.AddSingleton<Utilities.IDateTimeProvider, Utilities.DateTimeProvider>();
        
        // Event Aggregator - Singleton for application-wide events
        services.AddSingleton<Utilities.EventAggregator>();

        return services;
    }

    /// <summary>
    /// Registers presentation layer view models
    /// </summary>
    public static IServiceCollection AddViewModels(this IServiceCollection services)
    {
        // Main Form ✅ ADDED
        services.AddTransient<Presentation.Forms.MainForm>();

        // Main View Model
        services.AddTransient<MainViewModel>();

        // AI Assistant View Model
        services.AddTransient<AIAssistantViewModel>();

        // Other view models as needed
        services.AddTransient<LogsDashboard>();

        return services;
    }

    /// <summary>
    /// Configures application settings from configuration
    /// </summary>
    public static IServiceCollection ConfigureAppSettings(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind configuration sections to strongly-typed options
        //services.Configure<OpenAISettings>(configuration.GetSection("OpenAI"));
        // services.Configure<DatabaseSettings>(configuration.GetSection("Database"));
        // services.Configure<TelemetrySettings>(configuration.GetSection("Telemetry"));
        // services.Configure<LoggingSettings>(configuration.GetSection("Logging"));

        return services;
    }

    /// <summary>
    /// Validates that required services are registered
    /// </summary>
    public static void ValidateServiceRegistration(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        // Validate critical services are registered
        // This helps catch configuration issues early

        // Example validations:
        _ = serviceProvider.GetRequiredService<ISessionManager>();
        _ = serviceProvider.GetRequiredService<ITelemetryAggregator>();
        _ = serviceProvider.GetRequiredService<IContextBuilder>();

        // Validate configuration
        var apiKey = configuration[ConfigurationKeys.OpenAIApiKey];
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ConfigurationException("OpenAI API Key is not configured", ConfigurationKeys.OpenAIApiKey);
        }
    }
}

/// <summary>
/// Service lifetime extensions for easier registration
/// </summary>
public static class ServiceLifetimeExtensions
{
    /// <summary>
    /// Registers a service with automatic interface detection
    /// </summary>
    public static IServiceCollection AddService<TImplementation>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TImplementation : class
    {
        var implementationType = typeof(TImplementation);
        var interfaceType = implementationType.GetInterfaces().FirstOrDefault();

        if (interfaceType != null)
        {
            services.Add(new ServiceDescriptor(interfaceType, implementationType, lifetime));
        }
        else
        {
            services.Add(new ServiceDescriptor(implementationType, implementationType, lifetime));
        }

        return services;
    }
}
