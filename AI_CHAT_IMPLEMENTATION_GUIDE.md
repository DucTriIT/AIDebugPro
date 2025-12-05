# ?? AI Chat Integration - Quick Implementation Guide

**Goal:** Make AI Assistant talk to your telemetry in 3 weeks!

---

## Week 1: Foundation (Days 1-5)

### Day 1: Create Context Builder

**File:** `AIIntegration/TelemetryContextBuilder.cs`

```csharp
using AIDebugPro.Core.Interfaces;
using AIDebugPro.Core.Models;

namespace AIDebugPro.AIIntegration;

public class TelemetryContextBuilder
{
    private readonly ITelemetryAggregator _aggregator;
    
    public TelemetryContextBuilder(ITelemetryAggregator aggregator)
    {
        _aggregator = aggregator;
    }
    
    public async Task<TelemetryContext> BuildContextAsync(
        Guid sessionId,
        ActiveTab currentTab,
        object? selectedItem = null)
    {
        // Get recent telemetry (last 30 seconds)
        var consoleMessages = await _aggregator.GetConsoleMessagesAsync(
            sessionId, TimeSpan.FromSeconds(30));
        
        var networkRequests = await _aggregator.GetNetworkRequestsAsync(
            sessionId, TimeSpan.FromSeconds(30));
        
        var performanceMetrics = await _aggregator.GetPerformanceMetricsAsync(
            sessionId, TimeSpan.FromSeconds(10));
        
        var stats = await _aggregator.GetStatisticsAsync(sessionId);
        
        return new TelemetryContext
        {
            SessionId = sessionId,
            CurrentTab = currentTab,
            RecentConsoleMessages = consoleMessages.ToList(),
            RecentNetworkRequests = networkRequests.ToList(),
            LatestPerformanceMetrics = performanceMetrics.OrderByDescending(m => m.Timestamp).FirstOrDefault(),
            SelectedConsoleMessage = selectedItem as ConsoleMessage,
            SelectedNetworkRequest = selectedItem as NetworkRequest,
            SessionStatistics = stats,
            Timestamp = DateTime.UtcNow
        };
    }
}
```

**File:** `AIIntegration/Models/TelemetryContext.cs`

```csharp
namespace AIDebugPro.AIIntegration.Models;

public class TelemetryContext
{
    public Guid SessionId { get; set; }
    public ActiveTab CurrentTab { get; set; }
    public List<ConsoleMessage> RecentConsoleMessages { get; set; } = new();
    public List<NetworkRequest> RecentNetworkRequests { get; set; } = new();
    public PerformanceMetrics? LatestPerformanceMetrics { get; set; }
    public ConsoleMessage? SelectedConsoleMessage { get; set; }
    public NetworkRequest? SelectedNetworkRequest { get; set; }
    public TelemetryStatistics SessionStatistics { get; set; } = new();
    public string? CurrentUrl { get; set; }
    public DateTime Timestamp { get; set; }
}

public enum ActiveTab
{
    Console,
    Network,
    Performance
}
```

---

### Day 2-3: Create AI Debug Assistant

**File:** `AIIntegration/AIDebugAssistant.cs`

```csharp
using AIDebugPro.Core.Interfaces;
using AIDebugPro.AIIntegration.Models;
using Microsoft.Extensions.Logging;

namespace AIDebugPro.AIIntegration;

public class AIDebugAssistant
{
    private readonly IAIClient _aiClient;
    private readonly PromptComposer _promptComposer;
    private readonly ILogger<AIDebugAssistant>? _logger;

    public AIDebugAssistant(
        IAIClient aiClient,
        PromptComposer promptComposer,
        ILogger<AIDebugAssistant>? logger = null)
    {
        _aiClient = aiClient;
        _promptComposer = promptComposer;
        _logger = logger;
    }

    public async Task<AIDebugResponse> AnalyzeAsync(
        string userQuery,
        TelemetryContext context)
    {
        try
        {
            _logger?.LogInformation("?? Analyzing user query with telemetry context");
            _logger?.LogDebug("Query: {Query}, Context: {Tab} tab, {Console} console msgs, {Network} network reqs",
                userQuery, context.CurrentTab, context.RecentConsoleMessages.Count, context.RecentNetworkRequests.Count);

            // Compose context-aware prompt
            var prompt = _promptComposer.ComposeDebugPrompt(
                userQuery,
                context
            );

            // Call OpenAI
            var response = await _aiClient.AnalyzeAsync(prompt);

            // Parse response
            var debugResponse = new AIDebugResponse
            {
                Message = response.Analysis,
                Severity = DetermineSeverity(context),
                RelatedTelemetryIds = ExtractRelatedIds(response, context),
                CodeExamples = ExtractCodeExamples(response),
                RecommendedFixes = response.Recommendations.Select(r => r.Description).ToList()
            };

            _logger?.LogInformation("? AI analysis complete: {Severity} severity, {Fixes} recommended fixes",
                debugResponse.Severity, debugResponse.RecommendedFixes.Count);

            return debugResponse;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "? Error during AI analysis");
            return new AIDebugResponse
            {
                Message = $"Sorry, I encountered an error analyzing your request: {ex.Message}",
                Severity = IssueSeverity.Unknown
            };
        }
    }

    private IssueSeverity DetermineSeverity(TelemetryContext context)
    {
        var errors = context.RecentConsoleMessages.Count(m => m.Level == ConsoleMessageLevel.Error);
        var failures = context.RecentNetworkRequests.Count(r => r.IsFailed);

        if (errors > 5 || failures > 3) return IssueSeverity.Critical;
        if (errors > 0 || failures > 0) return IssueSeverity.High;
        return IssueSeverity.Low;
    }

    private List<Guid> ExtractRelatedIds(AIAnalysisResult result, TelemetryContext context)
    {
        // TODO: Parse AI response for referenced telemetry items
        // For now, return IDs of selected items
        var ids = new List<Guid>();
        
        if (context.SelectedConsoleMessage != null)
            ids.Add(context.SelectedConsoleMessage.Id);
        
        if (context.SelectedNetworkRequest != null)
            ids.Add(context.SelectedNetworkRequest.Id);

        return ids;
    }

    private List<CodeExample> ExtractCodeExamples(AIAnalysisResult result)
    {
        // TODO: Parse code blocks from AI response
        return new List<CodeExample>();
    }
}
```

**File:** `AIIntegration/Models/AIDebugResponse.cs`

```csharp
namespace AIDebugPro.AIIntegration.Models;

public class AIDebugResponse
{
    public string Message { get; set; } = string.Empty;
    public List<Guid> RelatedTelemetryIds { get; set; } = new();
    public List<CodeExample> CodeExamples { get; set; } = new();
    public IssueSeverity Severity { get; set; }
    public List<string> RecommendedFixes { get; set; } = new();
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class CodeExample
{
    public string Language { get; set; } = "javascript";
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
```

---

### Day 4: Enhance PromptComposer

**File:** `AIIntegration/PromptComposer.cs` (Add new method)

```csharp
public string ComposeDebugPrompt(string userQuery, TelemetryContext context)
{
    var prompt = new StringBuilder();

    prompt.AppendLine("# Debug Assistant Request");
    prompt.AppendLine();
    prompt.AppendLine($"User Query: {userQuery}");
    prompt.AppendLine();

    // Add telemetry context
    prompt.AppendLine("## Current Context");
    prompt.AppendLine($"- Active Tab: {context.CurrentTab}");
    prompt.AppendLine($"- Current URL: {context.CurrentUrl ?? "N/A"}");
    prompt.AppendLine($"- Session Duration: {(DateTime.UtcNow - context.Timestamp).TotalMinutes:F1} minutes");
    prompt.AppendLine();

    // Console errors
    var errors = context.RecentConsoleMessages
        .Where(m => m.Level == ConsoleMessageLevel.Error)
        .ToList();
    
    if (errors.Any())
    {
        prompt.AppendLine("## Console Errors (Recent)");
        foreach (var error in errors.Take(5))
        {
            prompt.AppendLine($"- [{error.Timestamp:HH:mm:ss}] {error.Message}");
            if (!string.IsNullOrEmpty(error.StackTrace))
                prompt.AppendLine($"  Stack: {error.StackTrace.Truncate(200)}");
        }
        prompt.AppendLine();
    }

    // Network failures
    var failures = context.RecentNetworkRequests
        .Where(r => r.IsFailed || r.StatusCode >= 400)
        .ToList();
    
    if (failures.Any())
    {
        prompt.AppendLine("## Network Issues");
        foreach (var req in failures.Take(5))
        {
            prompt.AppendLine($"- {req.Method} {req.Url} -> {req.StatusCode} ({req.DurationMs}ms)");
            if (!string.IsNullOrEmpty(req.ErrorText))
                prompt.AppendLine($"  Error: {req.ErrorText}");
        }
        prompt.AppendLine();
    }

    // Performance metrics
    if (context.LatestPerformanceMetrics != null)
    {
        var metrics = context.LatestPerformanceMetrics;
        prompt.AppendLine("## Performance Metrics");
        prompt.AppendLine($"- Load Time: {metrics.LoadEventMs}ms");
        prompt.AppendLine($"- First Contentful Paint: {metrics.FirstContentfulPaintMs}ms");
        prompt.AppendLine($"- Memory Usage: {metrics.MemoryUsageBytes / 1024 / 1024}MB");
        prompt.AppendLine($"- DOM Nodes: {metrics.DomNodeCount}");
        prompt.AppendLine();
    }

    // Selected item details
    if (context.SelectedConsoleMessage != null)
    {
        prompt.AppendLine("## Selected Console Message");
        var msg = context.SelectedConsoleMessage;
        prompt.AppendLine($"Level: {msg.Level}");
        prompt.AppendLine($"Message: {msg.Message}");
        prompt.AppendLine($"Source: {msg.Source}:{msg.LineNumber}");
        if (!string.IsNullOrEmpty(msg.StackTrace))
            prompt.AppendLine($"Stack Trace:\n{msg.StackTrace}");
        prompt.AppendLine();
    }

    if (context.SelectedNetworkRequest != null)
    {
        prompt.AppendLine("## Selected Network Request");
        var req = context.SelectedNetworkRequest;
        prompt.AppendLine($"Method: {req.Method}");
        prompt.AppendLine($"URL: {req.Url}");
        prompt.AppendLine($"Status: {req.StatusCode} {req.StatusText}");
        prompt.AppendLine($"Duration: {req.DurationMs}ms");
        if (req.RequestHeaders.Any())
        {
            prompt.AppendLine("Request Headers:");
            foreach (var header in req.RequestHeaders.Take(5))
                prompt.AppendLine($"  {header.Key}: {header.Value}");
        }
        if (req.ResponseHeaders.Any())
        {
            prompt.AppendLine("Response Headers:");
            foreach (var header in req.ResponseHeaders.Take(5))
                prompt.AppendLine($"  {header.Key}: {header.Value}");
        }
        prompt.AppendLine();
    }

    // Instructions
    prompt.AppendLine("## Instructions");
    prompt.AppendLine("Based on the above telemetry data:");
    prompt.AppendLine("1. Analyze the root cause of any issues");
    prompt.AppendLine("2. Explain what's happening in simple terms");
    prompt.AppendLine("3. Provide specific, actionable fix recommendations");
    prompt.AppendLine("4. Include code examples where helpful");
    prompt.AppendLine("5. Assess the severity and impact");
    prompt.AppendLine();
    prompt.AppendLine("Format your response in a friendly, helpful tone as if explaining to a developer colleague.");

    return prompt.ToString();
}
```

---

### Day 5: Wire Up AI Panel

**File:** `Presentation/UserControls/AIAssistantPanel.cs` (Enhance)

```csharp
// Add fields
private AIDebugAssistant? _aiAssistant;
private TelemetryContextBuilder? _contextBuilder;
private Guid? _currentSessionId;
private ActiveTab _currentTab = ActiveTab.Console;

// Add initialization method (called from MainForm)
public void Initialize(
    AIDebugAssistant aiAssistant,
    TelemetryContextBuilder contextBuilder)
{
    _aiAssistant = aiAssistant;
    _contextBuilder = contextBuilder;
}

// Update send button handler
private async void OnSendMessage(object? sender, EventArgs e)
{
    if (string.IsNullOrWhiteSpace(_messageInput?.Text)) return;
    if (_aiAssistant == null || _contextBuilder == null) return;

    var userMessage = _messageInput.Text;
    _messageInput.Text = string.Empty;

    try
    {
        // Add user message to chat
        AddMessageToChat($"You: {userMessage}", isUser: true);
        
        // Show typing indicator
        AddMessageToChat("AI is thinking...", isUser: false);

        // Build telemetry context
        var context = await _contextBuilder.BuildContextAsync(
            _currentSessionId ?? Guid.Empty,
            _currentTab,
            selectedItem: null // TODO: Get from MainForm
        );

        // Get AI response
        var response = await _aiAssistant.AnalyzeAsync(userMessage, context);

        // Remove typing indicator
        RemoveLastMessage();

        // Add AI response
        AddMessageToChat($"AI: {response.Message}", isUser: false);

        // Highlight related items
        OnHighlightRequested?.Invoke(this, response.RelatedTelemetryIds);
    }
    catch (Exception ex)
    {
        RemoveLastMessage();
        AddMessageToChat($"Error: {ex.Message}", isUser: false);
    }
}

// Add event for highlighting
public event EventHandler<List<Guid>>? OnHighlightRequested;

// Add method to set context
public void SetContext(Guid sessionId, ActiveTab tab)
{
    _currentSessionId = sessionId;
    _currentTab = tab;
}
```

---

## Week 2: User Experience (Days 6-10)

### Day 6-7: Add Context Menus

**File:** `Presentation/UserControls/LogsDashboard.cs` (Add)

```csharp
private void CreateConsoleListView()
{
    var listView = new ListView { /* ... existing code ... */ };
    
    // Add context menu
    var contextMenu = new ContextMenuStrip();
    contextMenu.Items.Add("?? Ask AI about this error", null, OnAskAIAboutError);
    contextMenu.Items.Add("?? Copy message", null, OnCopyMessage);
    
    listView.ContextMenuStrip = contextMenu;
    
    return listView;
}

private void OnAskAIAboutError(object? sender, EventArgs e)
{
    var selected = GetSelectedConsoleMessage();
    if (selected == null) return;

    // Raise event to MainForm
    OnAskAIRequested?.Invoke(this, new AIRequestEventArgs
    {
        TelemetryItem = selected,
        DefaultQuery = $"Explain this error: {selected.Message.Truncate(100)}"
    });
}

// Add event
public event EventHandler<AIRequestEventArgs>? OnAskAIRequested;

public class AIRequestEventArgs : EventArgs
{
    public object TelemetryItem { get; set; }
    public string DefaultQuery { get; set; }
}
```

---

### Day 8: Wire Everything Together

**File:** `Presentation/Forms/MainForm.cs` (Enhance)

```csharp
// Add fields
private AIDebugAssistant? _aiAssistant;
private TelemetryContextBuilder? _contextBuilder;

// Update constructor DI
public MainForm(
    ISessionManager sessionManager,
    ITelemetryAggregator telemetryAggregator,
    MainViewModel viewModel,
    AIDebugAssistant aiAssistant,           // NEW
    TelemetryContextBuilder contextBuilder,  // NEW
    ILogger<MainForm>? logger = null)
{
    // ... existing code ...
    _aiAssistant = aiAssistant;
    _contextBuilder = contextBuilder;
    
    // ...
}

// In SetupEventHandlers()
private void SetupEventHandlers()
{
    // ... existing code ...

    // Wire up AI panel
    if (_aiAssistantPanel != null)
    {
        _aiAssistantPanel.Initialize(_aiAssistant!, _contextBuilder!);
        _aiAssistantPanel.OnHighlightRequested += OnHighlightTelemetryItems;
    }

    // Wire up logs dashboard
    if (_logsDashboard != null)
    {
        _logsDashboard.OnAskAIRequested += OnAskAIRequested;
    }
}

// Handle AI request from context menu
private async void OnAskAIRequested(object? sender, AIRequestEventArgs e)
{
    // Switch to AI panel
    // TODO: Add tab switching logic

    // Set context
    _aiAssistantPanel?.SetContext(
        _currentSessionId ?? Guid.Empty,
        DetermineTabFromItem(e.TelemetryItem)
    );

    // Auto-populate query
    _aiAssistantPanel?.SetQuery(e.DefaultQuery);
}

// Handle highlight request from AI
private void OnHighlightTelemetryItems(object? sender, List<Guid> ids)
{
    _logsDashboard?.HighlightItems(ids);
}
```

---

### Day 9-10: Quick Prompts & Polish

**File:** `Presentation/UserControls/AIAssistantPanel.cs` (Add)

```csharp
private void CreateQuickPromptsPanel()
{
    var panel = new FlowLayoutPanel
    {
        Dock = DockStyle.Top,
        Height = 60,
        Padding = new Padding(10)
    };

    var prompts = new[]
    {
        ("??", "Analyze all errors"),
        ("??", "Check network failures"),
        ("?", "Performance bottlenecks"),
        ("??", "Generate summary")
    };

    foreach (var (emoji, text) in prompts)
    {
        var button = new Button
        {
            Text = $"{emoji} {text}",
            AutoSize = true,
            Margin = new Padding(5)
        };
        button.Click += (s, e) => OnQuickPrompt(text);
        panel.Controls.Add(button);
    }

    this.Controls.Add(panel);
}

private void OnQuickPrompt(string prompt)
{
    _messageInput.Text = prompt;
    OnSendMessage(null, EventArgs.Empty);
}
```

---

## Week 3: Testing & Polish (Days 11-15)

### Day 11-12: Auto-Analysis

**File:** `Presentation/Forms/MainForm.cs` (Enhance StopCaptureAsync)

```csharp
private async Task StopCaptureAsync()
{
    try
    {
        // ... existing stop logic ...

        // Auto-analyze if errors detected
        var stats = await _telemetryAggregator.GetStatisticsAsync(_currentSessionId.Value);
        
        if (stats.ConsoleErrors > 0 || stats.FailedNetworkRequests > 0)
        {
            _logger?.LogInformation("?? Auto-analyzing session: {Errors} errors, {Failures} failures",
                stats.ConsoleErrors, stats.FailedNetworkRequests);

            var context = await _contextBuilder!.BuildContextAsync(
                _currentSessionId.Value,
                ActiveTab.Console
            );

            var autoQuery = $"Summarize the {stats.ConsoleErrors} errors and {stats.FailedNetworkRequests} network failures detected in this session.";
            
            var response = await _aiAssistant!.AnalyzeAsync(autoQuery, context);

            // Show notification
            var notification = new Form
            {
                Text = "AI Analysis Complete",
                Size = new Size(400, 200),
                StartPosition = FormStartPosition.CenterScreen
            };
            var label = new Label
            {
                Text = $"Found {stats.ConsoleErrors} errors and {stats.FailedNetworkRequests} failures.\n\nClick to view AI analysis.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            notification.Controls.Add(label);
            notification.Click += (s, e) =>
            {
                _aiAssistantPanel?.ShowAnalysis(response);
                notification.Close();
            };
            notification.Show();
        }
    }
    catch (Exception ex)
    {
        _logger?.LogError(ex, "Error in auto-analysis");
    }
}
```

---

### Day 13: Register Services

**File:** `Services/DependencyInjection/ServiceRegistration.cs`

```csharp
public static class ServiceRegistration
{
    public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // ... existing services ...

        // ? NEW: AI Debug Assistant
        services.AddSingleton<AIDebugAssistant>();
        services.AddSingleton<TelemetryContextBuilder>();

        // ... rest of services ...
    }
}
```

---

### Day 14-15: Testing & Documentation

**Create test scenarios:**

1. Console error ? Right-click ? Ask AI
2. Network failure ? Right-click ? Analyze
3. Performance issues ? Quick prompt "? Performance bottlenecks"
4. Auto-analysis on stop
5. Highlight related items

**Update docs:**
- README examples
- AI_CHAT_INTEGRATION_SPEC.md
- Demo video/GIF

---

## ?? Success Criteria

After 3 weeks, you should have:

? AI chat responds to user queries  
? AI analyzes telemetry context  
? Right-click menus work on all tabs  
? Quick prompts functional  
? Auto-analysis on capture stop  
? Telemetry highlighting works  
? 5+ end-to-end scenarios tested  

---

## ?? **LET'S BUILD THIS!**

Start with `TelemetryContextBuilder.cs` - it's the foundation for everything else!
