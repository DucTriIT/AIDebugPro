# AIDebugPro - Layer Overview

## ?? Complete Project Structure

```
AIDebugPro/
?
??? ?? Presentation/                    [UI Layer - Windows Forms]
?   ??? Forms/                          Main windows and dialogs
?   ??? UserControls/                   Reusable UI components
?   ??? ViewModels/                     MVVM pattern view models
?
??? ?? BrowserIntegration/              [WebView2 & CDP Layer]
?   ??? DevToolsProtocol/               Console, Network, Perf, DOM listeners
?
??? ?? DataOrchestration/               [Data Aggregation Layer]
?                                       Telemetry, Session, Context Building
?
??? ?? AIIntegration/                   [AI/LLM Layer]
?   ??? Interfaces/                     IAIClient abstraction
?   ??? Clients/                        OpenAI, Local LLM implementations
?
??? ?? Persistence/                     [Storage & Reports Layer]
?   ??? Database/                       DB context and migrations
?   ??? Repositories/                   Data access implementations
?   ??? Templates/                      Report templates (PDF/HTML)
?
??? ?? Core/                            [Shared Domain Layer]
?   ??? Models/                         Data models and DTOs
?   ??? Interfaces/                     Core abstractions
?   ??? Enums/                         Shared enumerations
?   ??? Constants/                      Application constants
?   ??? Exceptions/                     Custom exceptions
?
??? ?? Services/                        [Cross-Cutting Services]
    ??? Logging/                        Serilog/NLog configuration
    ??? DependencyInjection/           DI container setup
    ??? Configuration/                  Settings management
    ??? BackgroundTasks/               Task queue and scheduling
    ??? Utilities/                      Helpers and extensions
```

## ?? Data Flow

```
???????????????
?  User Input ?
???????????????
       ?
       ?
???????????????????????????????
?   Presentation Layer        ?
?   (MainForm, Panels)        ?
???????????????????????????????
           ?
           ?
????????????????????????????????
?  BrowserIntegration Layer    ?
?  (WebView2 + CDP)            ?
?  • Console Errors            ?
?  • Network Requests          ?
?  • Performance Metrics       ?
?  • DOM Snapshots             ?
????????????????????????????????
           ?
           ?
????????????????????????????????
?  DataOrchestration Layer     ?
?  • TelemetryAggregator       ?
?  • SessionManager            ?
?  • ContextBuilder            ?
????????????????????????????????
           ?
           ?
????????????????????????????????
?  AIIntegration Layer         ?
?  • Prompt Composition        ?
?  • AI API Call               ?
?  • Response Parsing          ?
????????????????????????????????
           ?
           ?
????????????????????????????????
?  Presentation Layer          ?
?  (AI Insights Display)       ?
????????????????????????????????
```

## ?? Key Components by Layer

### 1. Presentation Layer
**Purpose**: User interface and interaction
- `MainForm.cs` - Main application window
- `WebViewPanel.cs` - Browser display
- `AIAssistantPanel.cs` - AI chat interface
- `LogsDashboard.cs` - Metrics and logs view
- `CommandToolbar.cs` - Action buttons

### 2. BrowserIntegration Layer
**Purpose**: Browser telemetry capture
- `WebView2Host.cs` - WebView2 management
- `ConsoleListener.cs` - JS errors/logs
- `NetworkListener.cs` - HTTP requests/responses
- `PerformanceCollector.cs` - Performance metrics
- `DOMSnapshotManager.cs` - DOM structure

### 3. DataOrchestration Layer
**Purpose**: Data aggregation and preparation
- `TelemetryAggregator.cs` - Event buffering
- `SessionManager.cs` - Session tracking
- `ContextBuilder.cs` - AI prompt generation
- `DataNormalizer.cs` - Schema normalization
- `RedactionService.cs` - Sensitive data removal

### 4. AIIntegration Layer
**Purpose**: AI analysis
- `IAIClient.cs` - Provider abstraction
- `OpenAIClient.cs` - GPT-4/5 integration
- `LocalLLMClient.cs` - Ollama/LM Studio
- `PromptComposer.cs` - Prompt building
- `ResponseParser.cs` - Result parsing

### 5. Persistence Layer
**Purpose**: Data storage and reporting
- `SessionRepository.cs` - Session CRUD
- `LogRepository.cs` - Log storage
- `SettingsRepository.cs` - App settings
- `ReportGenerator.cs` - PDF/HTML reports

### 6. Core Layer
**Purpose**: Shared domain
- `TelemetryModels.cs` - Console, Network, Perf models
- `SessionModels.cs` - Session data
- `AIResponseModels.cs` - AI result structures
- Interfaces, Enums, Constants, Exceptions

### 7. Services Layer
**Purpose**: Infrastructure
- `LoggerConfiguration.cs` - Logging setup
- `ServiceRegistration.cs` - DI configuration
- `AppSettings.cs` - Configuration management
- `TaskQueue.cs` - Background processing

## ?? Dependency Rules

1. **Core** - No dependencies (pure domain)
2. **Services** - Only depends on Core
3. **All other layers** - Can depend on Core and Services
4. **Presentation** - Can depend on all layers (orchestration)
5. **No circular dependencies** between layers

## ?? Next Implementation Steps

1. ? **Project structure created**
2. ? Define core models (`Core/Models/`)
3. ? Set up dependency injection (`Services/DependencyInjection/`)
4. ? Implement WebView2 host (`BrowserIntegration/`)
5. ? Build UI components (`Presentation/`)
6. ? Integrate AI providers (`AIIntegration/`)
7. ? Add persistence layer (`Persistence/`)
8. ? Wire everything together

## ?? Architecture Benefits

? **Separation of Concerns** - Each layer has a single responsibility
? **Testability** - Easy to mock dependencies and unit test
? **Maintainability** - Clear structure for finding and modifying code
? **Scalability** - Easy to add new features or swap implementations
? **Flexibility** - Can replace AI providers or storage without touching UI
