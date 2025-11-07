# AIDebugPro - Project Structure

This document outlines the complete folder structure for the AIDebugPro application.

## Project Architecture

```
AIDebugPro/
?
??? Program.cs                          # Application entry point
??? Form1.cs                            # Main form (to be moved to Presentation/Forms/)
??? Form1.Designer.cs
?
??? Core/                               # Shared models, interfaces, and common code
?   ??? Models/                         # Data models and DTOs
?   ?   ??? TelemetryModels.cs         # Console, Network, Performance, DOM models
?   ?   ??? SessionModels.cs           # Debug session data
?   ?   ??? AIResponseModels.cs        # AI analysis results
?   ??? Interfaces/                     # Core abstractions
?   ?   ??? ISessionManager.cs
?   ?   ??? ITelemetryAggregator.cs
?   ??? Enums/                         # Shared enumerations
?   ??? Constants/                      # Application constants
?   ??? Exceptions/                     # Custom exceptions
?
??? Presentation/                       # UI Layer (Windows Forms)
?   ??? Forms/                          # Main windows and dialogs
?   ?   ??? MainForm.cs                # Main application window
?   ??? UserControls/                   # Reusable UI components
?   ?   ??? WebViewPanel.cs           # Browser display control
?   ?   ??? AIAssistantPanel.cs       # AI chat and insights
?   ?   ??? LogsDashboard.cs          # Logs and metrics display
?   ?   ??? CommandToolbar.cs         # Action buttons
?   ??? ViewModels/                     # MVVM view models
?       ??? MainViewModel.cs
?       ??? AIAssistantViewModel.cs
?
??? BrowserIntegration/                 # WebView2 and CDP Layer
?   ??? WebView2Host.cs                # WebView2 control management
?   ??? DevToolsProtocol/              # CDP listeners
?   ?   ??? ConsoleListener.cs        # JavaScript console errors
?   ?   ??? NetworkListener.cs        # Network requests/responses
?   ?   ??? PerformanceCollector.cs   # Performance metrics
?   ?   ??? DOMSnapshotManager.cs     # DOM structure capture
?   ??? ScriptExecutor.cs              # JavaScript injection
?   ??? EventProcessor.cs              # CDP event stream handler
?
??? DataOrchestration/                  # Data aggregation and preparation
?   ??? TelemetryAggregator.cs         # Event buffering and timestamping
?   ??? SessionManager.cs              # Debug session tracking
?   ??? ContextBuilder.cs              # AI prompt context generation
?   ??? DataNormalizer.cs              # Schema normalization
?   ??? RedactionService.cs            # Sensitive data removal
?
??? AIIntegration/                      # AI/LLM Layer
?   ??? Interfaces/
?   ?   ??? IAIClient.cs               # AI provider abstraction
?   ??? Clients/                        # AI provider implementations
?   ?   ??? OpenAIClient.cs           # GPT-4/5 integration
?   ?   ??? LocalLLMClient.cs         # Ollama/LM Studio
?   ??? PromptComposer.cs              # Prompt generation
?   ??? ResponseParser.cs              # AI response parsing
?   ??? TokenManager.cs                # Caching and limiting
?
??? Persistence/                        # Data storage and reports
?   ??? Database/                       # DB context and migrations
?   ?   ??? AppDbContext.cs
?   ??? Repositories/                   # Data access layer
?   ?   ??? SessionRepository.cs
?   ?   ??? LogRepository.cs
?   ?   ??? SettingsRepository.cs
?   ??? ReportGenerator.cs             # PDF/HTML report creation
?   ??? Templates/                      # Report templates
?       ??? DebugReport.html
?       ??? PerfAudit.html
?
??? Services/                           # Cross-cutting concerns
    ??? Logging/                        # Serilog/NLog configuration
    ?   ??? LoggerConfiguration.cs
    ??? DependencyInjection/           # DI container setup
    ?   ??? ServiceRegistration.cs
    ??? Configuration/                  # App configuration
    ?   ??? AppSettings.cs
    ?   ??? appsettings.json
    ??? BackgroundTasks/               # Background processing
    ?   ??? TaskQueue.cs
    ??? Utilities/                      # Helper classes
        ??? Extensions.cs
```

## Layer Dependencies

```
Presentation
    ?
BrowserIntegration ? ? DataOrchestration ? AIIntegration
    ?                        ?                  ?
  Core  ?  ?  ?  ?  ?  ?  ? Core ? ? ? ? ? ? Core
    ?                        ?                  ?
Services                 Persistence        Services
```

## Key Principles

1. **Core** has no dependencies (domain models and interfaces only)
2. **Services** provides infrastructure to all layers
3. **Presentation** orchestrates and displays data
4. **BrowserIntegration** ? **DataOrchestration** ? **AIIntegration** forms the data pipeline
5. **Persistence** is accessed through repositories

## Next Steps

1. Define core models in `Core/Models/`
2. Implement browser integration with WebView2
3. Set up dependency injection in `Services/`
4. Build UI components in `Presentation/`
5. Integrate AI providers in `AIIntegration/`
