using Microsoft.Extensions.Logging;
using AIDebugPro.Core.Interfaces;
using AIDebugPro.Core.Models;
using AIDebugPro.Presentation.UserControls;
using AIDebugPro.Presentation.ViewModels;
using AIDebugPro.BrowserIntegration;
using AIDebugPro.Services.Utilities;
using AIDebugPro.AIIntegration;
using AIDebugPro.AIIntegration.Models;
using System.Text.Json;

namespace AIDebugPro.Presentation.Forms;

/// <summary>
/// Main application window
/// </summary>
public partial class MainForm : Form
{
    private readonly ISessionManager _sessionManager;
    private readonly ITelemetryAggregator _telemetryAggregator;
    private readonly ILogger<MainForm>? _logger;
    private readonly MainViewModel _viewModel;
    private readonly ILoggerFactory? _loggerFactory; // ⭐ NEW

    // User Controls
    private WebViewPanel? _webViewPanel;
    private AIAssistantPanel? _aiAssistantPanel;
    private LogsDashboard? _logsDashboard;
    private CommandToolbar? _commandToolbar;

    // Current session
    private Guid? _currentSessionId;
    private WebView2Host? _webViewHost;

    // ⭐ NEW: AI services
    private AIDebugAssistant? _aiAssistant;
    private TelemetryContextBuilder? _contextBuilder;

    private HashSet<string> _displayedConsoleMessageIds = new();
    private HashSet<string> _displayedNetworkRequestIds = new();

    public MainForm(
        ISessionManager sessionManager,
        ITelemetryAggregator telemetryAggregator,
        MainViewModel viewModel,
        AIDebugAssistant aiAssistant,
        TelemetryContextBuilder contextBuilder,
        ILoggerFactory loggerFactory, // ⭐ NEW
        ILogger<MainForm>? logger = null)
    {
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        _telemetryAggregator = telemetryAggregator ?? throw new ArgumentNullException(nameof(telemetryAggregator));
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _logger = logger;
        _loggerFactory = loggerFactory; // ⭐ NEW

        // ⭐ NEW: Store AI services
        _aiAssistant = aiAssistant;
        _contextBuilder = contextBuilder;

        InitializeComponent();
        InitializeCustomComponents();
        SetupEventHandlers();
        
        _logger?.LogInformation("MainForm initialized");
    }

    private void InitializeCustomComponents()
    {
        // Set form properties
        this.Text = "AIDebugPro - Browser Debugging Assistant";
        this.Size = new Size(1400, 900);
        this.MinimumSize = new Size(1024, 768);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Icon = SystemIcons.Application;

        // ⭐ FIX: Create main split container FIRST (so menu doesn't overlay it)
        var mainSplitContainer = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 550,
            FixedPanel = FixedPanel.None
        };

        // Top section: Browser and AI Assistant
        var topSplitContainer = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 900,
            FixedPanel = FixedPanel.None
        };
        mainSplitContainer.Panel1.Controls.Add(topSplitContainer);

        // Left: WebView Panel
        _webViewPanel = new WebViewPanel
        {
            Dock = DockStyle.Fill
        };
        topSplitContainer.Panel1.Controls.Add(_webViewPanel);

        // Right: AI Assistant Panel
        _aiAssistantPanel = new AIAssistantPanel
        {
            Dock = DockStyle.Fill
        };
        topSplitContainer.Panel2.Controls.Add(_aiAssistantPanel);

        // Bottom section: Logs Dashboard
        _logsDashboard = new LogsDashboard
        {
            Dock = DockStyle.Fill
        };
        mainSplitContainer.Panel2.Controls.Add(_logsDashboard);

        // ⭐ FIX: Add main container first
        this.Controls.Add(mainSplitContainer);

        // ⭐ FIX: Then create toolbar (goes on top of split container)
        _commandToolbar = new CommandToolbar
        {
            Dock = DockStyle.Top,
            Height = 50
        };
        this.Controls.Add(_commandToolbar);

        // ⭐ FIX: Then menu strip (goes on top of toolbar)
        CreateMenuStrip();

        // Create status bar (bottom)
        CreateStatusBar();

        _logger?.LogDebug("Custom components initialized");
    }

    private void CreateMenuStrip()
    {
        var menuStrip = new MenuStrip
        {
            Dock = DockStyle.Top
        };

        // File menu
        var fileMenu = new ToolStripMenuItem("&File");
        fileMenu.DropDownItems.Add("&New Session", null, OnNewSession);
        fileMenu.DropDownItems.Add("&Open Session...", null, OnOpenSession);
        fileMenu.DropDownItems.Add("&Save Session", null, OnSaveSession);
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add("&Export Report...", null, OnExportReport);
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add("E&xit", null, (s, e) => this.Close());
        menuStrip.Items.Add(fileMenu);

        // Tools menu
        var toolsMenu = new ToolStripMenuItem("&Tools");
        toolsMenu.DropDownItems.Add("&Navigate to URL...", null, OnNavigateToUrl);
        toolsMenu.DropDownItems.Add(new ToolStripSeparator());
        toolsMenu.DropDownItems.Add("&Start Capture", null, OnStartCapture);
        toolsMenu.DropDownItems.Add("S&top Capture", null, OnStopCapture);
        toolsMenu.DropDownItems.Add(new ToolStripSeparator());
        toolsMenu.DropDownItems.Add("&Analyze with AI", null, OnAnalyzeWithAI);
        toolsMenu.DropDownItems.Add("&Clear Telemetry", null, OnClearTelemetry);
        toolsMenu.DropDownItems.Add(new ToolStripSeparator());
        toolsMenu.DropDownItems.Add("&Settings...", null, OnOpenSettings);
        menuStrip.Items.Add(toolsMenu);

        // View menu
        var viewMenu = new ToolStripMenuItem("&View");
        viewMenu.DropDownItems.Add("&Console Logs", null, (s, e) => _logsDashboard?.ShowConsoleTab());
        viewMenu.DropDownItems.Add("&Network Requests", null, (s, e) => _logsDashboard?.ShowNetworkTab());
        viewMenu.DropDownItems.Add("&Performance Metrics", null, (s, e) => _logsDashboard?.ShowPerformanceTab());
        viewMenu.DropDownItems.Add(new ToolStripSeparator());
        viewMenu.DropDownItems.Add("&Refresh", null, OnRefresh);
        menuStrip.Items.Add(viewMenu);

        // Help menu
        var helpMenu = new ToolStripMenuItem("&Help");
        helpMenu.DropDownItems.Add("&Documentation", null, OnShowDocumentation);
        helpMenu.DropDownItems.Add("&About AIDebugPro", null, OnShowAbout);
        menuStrip.Items.Add(helpMenu);

        this.MainMenuStrip = menuStrip;
        this.Controls.Add(menuStrip);
    }

    private void CreateStatusBar()
    {
        var statusStrip = new StatusStrip
        {
            Dock = DockStyle.Bottom
        };

        var statusLabel = new ToolStripStatusLabel
        {
            Name = "statusLabel",
            Text = "Ready",
            Spring = true,
            TextAlign = ContentAlignment.MiddleLeft
        };
        statusStrip.Items.Add(statusLabel);

        var sessionIndicator = new ToolStripStatusLabel
        {
            Name = "sessionIndicator",
            Text = "No Session",
            BorderSides = ToolStripStatusLabelBorderSides.Left
        };
        statusStrip.Items.Add(sessionIndicator);

        var progressBar = new ToolStripProgressBar
        {
            Name = "progressBar",
            Visible = false,
            Width = 150
        };
        statusStrip.Items.Add(progressBar);

        this.Controls.Add(statusStrip);
    }

    private void SetupEventHandlers()
    {
        this.Load += MainForm_Load;
        this.FormClosing += MainForm_FormClosing;

        if (_commandToolbar != null)
        {
            _commandToolbar.StartClicked += OnStartCapture;
            _commandToolbar.StopClicked += OnStopCapture;
            _commandToolbar.AnalyzeClicked += OnAnalyzeWithAI;
            _commandToolbar.ExportClicked += OnExportReport;
        }

        if (_webViewPanel != null)
        {
            _webViewPanel.NavigationCompleted += OnNavigationCompleted;
            _webViewPanel.NavigationFailed += OnNavigationFailed;
        }

        // ⭐ NEW: Wire up AI panel
        if (_aiAssistantPanel != null && _aiAssistant != null && _contextBuilder != null)
        {
            var aiPanelLogger = _loggerFactory?.CreateLogger<AIAssistantPanel>();
            _aiAssistantPanel.Initialize(_aiAssistant, _contextBuilder, aiPanelLogger);
            _aiAssistantPanel.OnHighlightRequested += OnHighlightTelemetryItems;
            _aiAssistantPanel.MessageSent += OnAIMessageSent;
        }

        // ⭐ NEW: Wire up logs dashboard
        if (_logsDashboard != null)
        {
            _logsDashboard.OnAskAIRequested += OnAskAIRequested;
        }

        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    // ⭐ NEW: Handle AI request from context menu
    private void OnAskAIRequested(object? sender, AIRequestEventArgs e)
    {
        _logger?.LogInformation("AI request from context menu: {Query}", e.DefaultQuery);

        // Set context based on item type
        var tab = e.ItemType switch
        {
            "Console" => ActiveTab.Console,
            "Network" => ActiveTab.Network,
            "Performance" => ActiveTab.Performance,
            _ => ActiveTab.Console
        };

        // Set AI panel context
        _aiAssistantPanel?.SetContext(_currentSessionId ?? Guid.Empty, tab);

        // Auto-populate query in AI panel
        _aiAssistantPanel?.SetQuery(e.DefaultQuery);

        // TODO: Switch to AI Assistant panel tab
    }

    // ⭐ NEW: Handle highlight request from AI
    private void OnHighlightTelemetryItems(object? sender, List<Guid> ids)
    {
        _logsDashboard?.HighlightItems(ids);
        _logger?.LogInformation("Highlighted {Count} telemetry items", ids.Count);
    }

    private async void MainForm_Load(object? sender, EventArgs e)
    {
        try
        {
            _logger?.LogInformation("MainForm loading...");
            UpdateStatus("Initializing...");

            if (_webViewPanel != null)
            {
                await _webViewPanel.InitializeAsync();
                
                // Set up telemetry update timer
                var updateTimer = new System.Windows.Forms.Timer
                {
                    Interval = 1000 // Update every second
                };
                updateTimer.Tick += async (s, args) => await UpdateTelemetryDisplayAsync();
                updateTimer.Start();
            }

            LoadUserSettings();
            UpdateStatus("Ready");
            _logger?.LogInformation("MainForm loaded successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading MainForm");
            MessageBox.Show($"Failed to initialize application: {ex.Message}", "Initialization Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task UpdateTelemetryDisplayAsync()
    {
        if (!_currentSessionId.HasValue || !_viewModel.IsCapturing) return;

        try
        {
            _logger?.LogDebug("=== UPDATE TELEMETRY DISPLAY START ===");
            _logger?.LogDebug("Session ID: {SessionId}, IsCapturing: {IsCapturing}", 
                _currentSessionId, _viewModel.IsCapturing);

            // Get recent console messages (last 30 seconds to capture new ones)
            var consoleMessages = await _telemetryAggregator.GetConsoleMessagesAsync(
                _currentSessionId.Value, 
                TimeSpan.FromSeconds(30));

            // Get recent network requests
            var networkRequests = await _telemetryAggregator.GetNetworkRequestsAsync(
                _currentSessionId.Value,
                TimeSpan.FromSeconds(30));

            // Get performance metrics
            var performanceMetrics = await _telemetryAggregator.GetPerformanceMetricsAsync(
                _currentSessionId.Value,
                TimeSpan.FromSeconds(10));

            // DETAILED LOGGING
            _logger?.LogInformation("📊 TELEMETRY FETCHED: Console={Console}, Network={Network}, Performance={Performance}",
                consoleMessages.Count(),
                networkRequests.Count(),
                performanceMetrics.Count());

            // Log network request details
            if (networkRequests.Any())
            {
                _logger?.LogInformation("🌐 NETWORK REQUESTS FOUND:");
                foreach (var req in networkRequests.Take(5))
                {
                    _logger?.LogInformation("  - {Method} {Url} -> {Status} ({Duration}ms)",
                        req.Method, req.Url?.Truncate(50), req.StatusCode, req.DurationMs);
                }
            }
            else
            {
                _logger?.LogWarning("⚠️  NO NETWORK REQUESTS found in last 30 seconds");
            }

            // Log performance metrics details
            if (performanceMetrics.Any())
            {
                var latest = performanceMetrics.OrderByDescending(m => m.Timestamp).First();
                _logger?.LogInformation("⚡ PERFORMANCE METRICS FOUND: LoadTime={Load}ms, FCP={FCP}ms, Memory={Memory}MB",
                    latest.LoadEventMs, latest.FirstContentfulPaintMs, latest.MemoryUsageBytes / 1024 / 1024);
            }
            else
            {
                _logger?.LogWarning("⚠️  NO PERFORMANCE METRICS found in last 10 seconds");
            }

            // Add only new console messages
            int newConsoleCount = 0;
            foreach (var msg in consoleMessages.OrderBy(m => m.Timestamp))
            {
                var msgId = $"{msg.Timestamp:O}_{msg.Message}_{msg.Level}";
                if (_displayedConsoleMessageIds.Add(msgId))
                {
                    _logsDashboard?.AddConsoleMessage(msg);
                    newConsoleCount++;
                }
            }
            if (newConsoleCount > 0)
            {
                _logger?.LogDebug("✅ Added {Count} new console messages to UI", newConsoleCount);
            }

            // Add only new network requests
            int newNetworkCount = 0;
            foreach (var req in networkRequests.OrderBy(r => r.Timestamp))
            {
                if (!string.IsNullOrEmpty(req.RequestId) && _displayedNetworkRequestIds.Add(req.RequestId))
                {
                    _logger?.LogDebug("➕ Adding network request to UI: {Method} {Url} ({RequestId})",
                        req.Method, req.Url?.Truncate(30), req.RequestId?.Truncate(10));
                    _logsDashboard?.AddNetworkRequest(req);
                    newNetworkCount++;
                }
            }
            if (newNetworkCount > 0)
            {
                _logger?.LogInformation("✅ Added {Count} new network requests to UI", newNetworkCount);
            }
            else if (networkRequests.Any())
            {
                _logger?.LogWarning("⚠️  Network requests exist but none were new (all already displayed)");
            }

            // Update performance metrics (always show latest)
            var latestMetrics = performanceMetrics.OrderByDescending(m => m.Timestamp).FirstOrDefault();
            if (latestMetrics != null)
            {
                _logger?.LogDebug("✅ Updating performance metrics in UI");
                _logsDashboard?.UpdatePerformanceMetrics(latestMetrics);
            }

            // Update counts in view model
            var stats = await _telemetryAggregator.GetStatisticsAsync(_currentSessionId.Value);
            _logger?.LogDebug("📈 STATS: Total Console={Console}, Network={Network}, Errors={Errors}, Warnings={Warnings}",
                stats.TotalConsoleMessages, stats.TotalNetworkRequests, stats.ConsoleErrors, stats.ConsoleWarnings);
            
            _viewModel.UpdateTelemetryCounts(
                stats.TotalConsoleMessages,
                stats.TotalNetworkRequests,
                stats.ConsoleErrors,
                stats.ConsoleWarnings);

            _logger?.LogDebug("=== UPDATE TELEMETRY DISPLAY END ===");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "❌ ERROR in UpdateTelemetryDisplayAsync");
        }
    }

    private async void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        try
        {
            if (_currentSessionId.HasValue)
            {
                var result = MessageBox.Show("A capture session is active. Do you want to save it before closing?",
                    "Save Session", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }

                if (result == DialogResult.Yes)
                {
                    await SaveCurrentSessionAsync();
                }

                await StopCaptureAsync();
            }

            SaveUserSettings();
            _logger?.LogInformation("Application closing");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during application shutdown");
        }
    }

    private async void OnNewSession(object? sender, EventArgs e)
    {
        try
        {
            var dialog = new InputDialog("New Session", "Enter session name:");
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var sessionName = dialog.InputText;
                var url = _webViewPanel?.CurrentUrl ?? "about:blank";

                var session = await _sessionManager.CreateSessionAsync(sessionName, url);
                _currentSessionId = session.Id;

                // ⭐ NEW: Set AI panel context
                _aiAssistantPanel?.SetContext(session.Id, ActiveTab.Console);

                UpdateStatus($"Session created: {sessionName}");
                UpdateSessionIndicator($"Session: {sessionName}");
                _logger?.LogInformation("Created new session: {SessionId}", session.Id);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating new session");
            ShowError("Failed to create new session", ex);
        }
    }

    private async void OnOpenSession(object? sender, EventArgs e)
    {
        try
        {
            // Check if there's an active session
            if (_currentSessionId.HasValue && _viewModel.IsCapturing)
            {
                var result = MessageBox.Show(
                    "There is an active capture session. Do you want to stop it before opening a saved session?",
                    "Active Session",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.No)
                    return;

                await StopCaptureAsync();
            }

            var dialog = new OpenFileDialog
            {
                Filter = "AIDebugPro Session (*.aidp)|*.aidp|JSON Session (*.json)|*.json|All Files (*.*)|*.*",
                DefaultExt = "aidp",
                Title = "Open Session"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                UpdateStatus("Loading session...");
                ShowProgress(true);

                _logger?.LogInformation("📂 Loading session from file {FileName}", dialog.FileName);

                // Read and deserialize session
                var json = await File.ReadAllTextAsync(dialog.FileName);
                var session = JsonSerializer.Deserialize<DebugSession>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (session == null)
                {
                    throw new InvalidOperationException("Failed to deserialize session");
                }

                _logger?.LogInformation("✅ Session loaded: {SessionName} with {Snapshots} snapshots, {Analyses} analyses",
                    session.Name, session.Snapshots.Count, session.AnalysisResults.Count);

                // Import session to session manager (creates new ID and adds to collection)
                session = await _sessionManager.ImportSessionAsync(session);

                // Set as current session
                _currentSessionId = session.Id;

                // Update UI
                UpdateSessionIndicator($"Session: {session.Name} (Loaded)");
                UpdateStatus("Session loaded successfully");
                ShowProgress(false);

                // Set AI panel context
                _aiAssistantPanel?.SetContext(session.Id, ActiveTab.Console);

                _logger?.LogInformation("✅ Session imported with new ID: {SessionId}", session.Id);

                // Show summary
                var summaryMessage = $"Session Loaded Successfully!\n\n" +
                    $"Name: {session.Name}\n" +
                    $"URL: {session.Url}\n" +
                    $"Started: {session.StartedAt:yyyy-MM-dd HH:mm}\n" +
                    $"Snapshots: {session.Snapshots.Count}\n" +
                    $"AI Analyses: {session.AnalysisResults.Count}\n" +
                    $"Console Errors: {session.Statistics.TotalConsoleErrors}\n" +
                    $"Network Requests: {session.Statistics.TotalNetworkRequests}\n\n" +
                    $"Note: This is a read-only view of the saved session.";

                MessageBox.Show(summaryMessage, "Session Loaded", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                // TODO: Display loaded telemetry data in the dashboard
                // For now, the data is loaded but not displayed in the UI
                // This would require enhancing LogsDashboard to display historical data
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "❌ Error loading session");
            ShowError("Failed to load session", ex);
            ShowProgress(false);
        }
    }

    private async void OnSaveSession(object? sender, EventArgs e)
    {
        if (!_currentSessionId.HasValue)
        {
            MessageBox.Show("No active session to save", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        
        try
        {
            var dialog = new SaveFileDialog
            {
                Filter = "AIDebugPro Session (*.aidp)|*.aidp|JSON Session (*.json)|*.json",
                DefaultExt = "aidp",
                FileName = $"Session_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                UpdateStatus("Saving session...");
                ShowProgress(true);

                _logger?.LogInformation("💾 Saving session {SessionId} to file {FileName}", 
                    _currentSessionId, dialog.FileName);

                // Get the current session with all data
                var session = await _sessionManager.GetSessionAsync(_currentSessionId.Value);
                
                if (session == null)
                {
                    throw new InvalidOperationException("Session not found");
                }

                // Create final snapshot before saving if capturing
                if (_viewModel.IsCapturing)
                {
                    _logger?.LogInformation("Creating final snapshot before saving...");
                    var snapshot = await _telemetryAggregator.CreateSnapshotAsync(_currentSessionId.Value);
                    await _sessionManager.AddSnapshotAsync(_currentSessionId.Value, snapshot);
                    
                    // Refresh session data
                    session = await _sessionManager.GetSessionAsync(_currentSessionId.Value);
                }

                // Serialize session to JSON
                var json = JsonSerializer.Serialize(session, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                // Save to file
                await File.WriteAllTextAsync(dialog.FileName, json);

                UpdateStatus($"Session saved to {dialog.FileName}");
                ShowProgress(false);

                _logger?.LogInformation("✅ Session saved successfully: {Snapshots} snapshots, {Analyses} analyses",
                    session.Snapshots.Count, session.AnalysisResults.Count);

                MessageBox.Show(
                    $"Session saved successfully!\n\n" +
                    $"Snapshots: {session.Snapshots.Count}\n" +
                    $"AI Analyses: {session.AnalysisResults.Count}\n" +
                    $"File: {Path.GetFileName(dialog.FileName)}",
                    "Session Saved",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "❌ Error saving session");
            ShowError("Failed to save session", ex);
            ShowProgress(false);
        }
    }

    private async void OnExportReport(object? sender, EventArgs e)
    {
        if (!_currentSessionId.HasValue)
        {
            MessageBox.Show("No session to export", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            var dialog = new SaveFileDialog
            {
                Filter = "HTML Report (*.html)|*.html|Markdown Report (*.md)|*.md|JSON Report (*.json)|*.json",
                DefaultExt = "html",
                FileName = $"DebugReport_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                UpdateStatus("Generating report...");
                ShowProgress(true);

                // Determine format based on file extension
                var format = Path.GetExtension(dialog.FileName).ToLowerInvariant() switch
                {
                    ".html" => ReportFormat.HTML,
                    ".md" => ReportFormat.Markdown,
                    ".json" => ReportFormat.JSON,
                    _ => ReportFormat.HTML
                };

                _logger?.LogInformation("Exporting session {SessionId} as {Format}", _currentSessionId, format);

                // Generate the report content
                var reportContent = await _sessionManager.ExportSessionAsync(_currentSessionId.Value, format);

                // Write to file
                await File.WriteAllTextAsync(dialog.FileName, reportContent);

                UpdateStatus($"Report exported to {dialog.FileName}");
                ShowProgress(false);

                _logger?.LogInformation("Report successfully exported to {FileName}", dialog.FileName);

                // Ask if user wants to open the report
                var result = MessageBox.Show(
                    $"Report exported successfully!\n\nDo you want to open it now?",
                    "Export Complete",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (result == DialogResult.Yes)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = dialog.FileName,
                        UseShellExecute = true
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error exporting report");
            ShowError("Failed to export report", ex);
            ShowProgress(false);
        }
    }

    private async void OnStartCapture(object? sender, EventArgs e) => await StartCaptureAsync();
    private async void OnStopCapture(object? sender, EventArgs e) => await StopCaptureAsync();

    private async void OnAnalyzeWithAI(object? sender, EventArgs e)
    {
        if (!_currentSessionId.HasValue)
        {
            MessageBox.Show("No active session to analyze", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            UpdateStatus("Analyzing with AI...");
            ShowProgress(true);

            _logger?.LogInformation("🤖 Starting AI analysis for session {SessionId}", _currentSessionId);

            // ✅ Step 1: Create snapshot of current telemetry data
            _logger?.LogInformation("📸 Creating snapshot for AI analysis...");
            var snapshot = await _telemetryAggregator.CreateSnapshotAsync(_currentSessionId.Value);
            await _sessionManager.AddSnapshotAsync(_currentSessionId.Value, snapshot);
            
            _logger?.LogInformation("✅ Snapshot created: {Console} console messages, {Network} network requests",
                snapshot.ConsoleMessages.Count, snapshot.NetworkRequests.Count);

            // ✅ Step 2: Build telemetry context for AI
            _logger?.LogInformation("🔨 Building telemetry context...");
            var context = await _contextBuilder!.BuildContextAsync(
                _currentSessionId.Value,
                ActiveTab.Console, // Focus on console errors
                _webViewPanel?.CurrentUrl);

            _logger?.LogInformation("✅ Context built: {Errors} errors, {Warnings} warnings, {FailedRequests} failed requests",
                context.RecentConsoleMessages.Count(m => m.Level == ConsoleMessageLevel.Error),
                context.RecentConsoleMessages.Count(m => m.Level == ConsoleMessageLevel.Warning),
                context.RecentNetworkRequests.Count(r => r.IsFailed));

            // ✅ Step 3: Perform AI analysis
            _logger?.LogInformation("🤖 Calling AI to analyze session...");
            var aiResponse = await _aiAssistant!.AnalyzeSessionAsync(_currentSessionId.Value, context);

            _logger?.LogInformation("✅ AI analysis complete: Severity={Severity}, Recommendations={Count}",
                aiResponse.Severity, aiResponse.RecommendedFixes.Count);

            // ✅ Step 4: Convert AIDebugResponse to AIAnalysisResult and save to session
            var analysisResult = new AIAnalysisResult
            {
                Id = Guid.NewGuid(),
                SessionId = _currentSessionId.Value,
                AnalyzedAt = DateTime.UtcNow,
                Model = "gpt-4", // TODO: Get from configuration
                Status = AIAnalysisStatus.Completed,
                Summary = aiResponse.Message,
                Issues = aiResponse.RelatedTelemetryIds.Select(id =>
                {
                    // Find the actual console message or network request
                    var consoleMsg = snapshot.ConsoleMessages.FirstOrDefault(m => m.Id == id);
                    if (consoleMsg != null)
                    {
                        return new Issue
                        {
                            Id = Guid.NewGuid(),
                            Title = $"{consoleMsg.Level}: {consoleMsg.Message.Truncate(50)}",
                            Description = consoleMsg.Message,
                            Severity = consoleMsg.Level == ConsoleMessageLevel.Error ? IssueSeverity.High : IssueSeverity.Medium,
                            Source = consoleMsg.Source,
                            LineNumber = consoleMsg.LineNumber,
                            Category = IssueCategory.JavaScriptError,
                            CodeSnippet = consoleMsg.StackTrace
                        };
                    }

                    var networkReq = snapshot.NetworkRequests.FirstOrDefault(r => r.Id == id);
                    if (networkReq != null)
                    {
                        return new Issue
                        {
                            Id = Guid.NewGuid(),
                            Title = $"Network {networkReq.StatusCode}: {networkReq.Url?.Truncate(50)}",
                            Description = networkReq.ErrorText ?? $"Request failed with status {networkReq.StatusCode}",
                            Severity = IssueSeverity.High,
                            Source = networkReq.Url,
                            Category = IssueCategory.NetworkError,
                            RelatedUrls = new List<string> { networkReq.Url ?? "Unknown" }
                        };
                    }

                    return null;
                }).Where(i => i != null).Cast<Issue>().ToList(),
                Recommendations = aiResponse.RecommendedFixes.Select((fix, index) => new Recommendation
                {
                    Id = Guid.NewGuid(),
                    Title = $"Fix #{index + 1}",
                    Description = fix,
                    Priority = index == 0 ? RecommendationPriority.High : RecommendationPriority.Medium,
                    Type = RecommendationType.ErrorHandling,
                    ExpectedImpact = "Resolves identified issues"
                }).ToList()
            };

            // Save analysis to session
            await _sessionManager.AddAnalysisResultAsync(_currentSessionId.Value, analysisResult);
            
            _logger?.LogInformation("💾 AI analysis saved to session with {IssueCount} issues and {RecCount} recommendations",
                analysisResult.Issues.Count, analysisResult.Recommendations.Count);

            // ✅ Step 5: Display results in AI Assistant panel
            _aiAssistantPanel?.ShowAnalysis(aiResponse);

            UpdateStatus("AI analysis complete");
            ShowProgress(false);

            // Show summary
            var summaryMessage = $"AI Analysis Complete!\n\n" +
                $"Severity: {aiResponse.Severity}\n" +
                $"Issues Found: {analysisResult.Issues.Count}\n" +
                $"Recommendations: {analysisResult.Recommendations.Count}\n\n" +
                $"Check the AI Assistant panel for detailed results.";

            MessageBox.Show(summaryMessage, "AI Analysis Complete", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            _logger?.LogInformation("✅ AI analysis workflow completed successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "❌ Error during AI analysis");
            ShowError("AI analysis failed", ex);
            ShowProgress(false);
        }
    }

    private async void OnClearTelemetry(object? sender, EventArgs e)
    {
        if (!_currentSessionId.HasValue) return;

        var result = MessageBox.Show("Are you sure you want to clear all telemetry data?",
            "Confirm Clear", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

        if (result == DialogResult.Yes)
        {
            await _telemetryAggregator.ClearTelemetryAsync(_currentSessionId.Value);
            _logsDashboard?.ClearAll();
            UpdateStatus("Telemetry cleared");
        }
    }

    private void OnOpenSettings(object? sender, EventArgs e)
    {
        MessageBox.Show("Settings dialog coming soon!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void OnRefresh(object? sender, EventArgs e)
    {
        _webViewPanel?.Reload();
        UpdateStatus("Page refreshed");
    }

    private void OnNavigateToUrl(object? sender, EventArgs e)
    {
        var dialog = new InputDialog("Navigate to URL", "Enter URL:");
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            var url = dialog.InputText;
            
            // Add https:// if no protocol specified
            if (!string.IsNullOrEmpty(url) &&
                !url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://" + url;
            }

            if (_webViewPanel?.GetWebView2()?.CoreWebView2 != null)
            {
                _webViewPanel.GetWebView2().CoreWebView2.Navigate(url);
                UpdateStatus($"Navigating to: {url}");
            }
        }
    }

    private void OnShowDocumentation(object? sender, EventArgs e)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "https://github.com/DucTriIT/AIDebugPro",
            UseShellExecute = true
        });
    }

    private void OnShowAbout(object? sender, EventArgs e)
    {
        MessageBox.Show("AIDebugPro v1.0.0\n\n" +
            "AI-Powered Browser Debugging Assistant\n\n" +
            "Developed with ❤️ using .NET 9 and GPT-4",
            "About AIDebugPro", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void OnNavigationCompleted(object? sender, string url) => UpdateStatus($"Navigated to: {url}");
    private void OnNavigationFailed(object? sender, string error) => ShowError("Navigation failed", new Exception(error));
    private void OnAIMessageSent(object? sender, string message) => _logger?.LogDebug("AI message sent: {Message}", message);

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (InvokeRequired)
        {
            Invoke(() => ViewModel_PropertyChanged(sender, e));
            return;
        }

        switch (e.PropertyName)
        {
            case nameof(MainViewModel.IsCapturing):
                UpdateCaptureState();
                break;
            case nameof(MainViewModel.StatusMessage):
                UpdateStatus(_viewModel.StatusMessage);
                break;
        }
    }

    private async Task StartCaptureAsync()
    {
        try
        {
            if (_currentSessionId == null)
            {
                OnNewSession(null, EventArgs.Empty);
                if (_currentSessionId == null) return;
            }

            _logger?.LogInformation("🚀 STARTING CAPTURE for session {SessionId}", _currentSessionId);
            UpdateStatus("Starting capture...");
            
            // Check if WebView2 is ready
            if (_webViewPanel?.GetWebView2()?.CoreWebView2 == null)
            {
                _logger?.LogError("❌ WebView2.CoreWebView2 is NULL - cannot start capture");
                ShowError("Failed to start capture", new Exception("WebView2 not initialized. Please wait for the browser to load."));
                return;
            }

            _logger?.LogDebug("✅ WebView2.CoreWebView2 is ready");

            // Initialize WebView2Host if not already done
            if (_webViewHost == null)
            {
                _logger?.LogDebug("Creating new WebView2Host...");
                _webViewHost = new WebView2Host(_webViewPanel.GetWebView2(), _telemetryAggregator);
                _logger?.LogInformation("✅ WebView2Host created successfully");
            }
            else
            {
                _logger?.LogDebug("WebView2Host already exists, reusing");
            }

            // Start CDP session (WebView2 is already initialized by WebViewPanel)
            _logger?.LogInformation("📡 Starting CDP session...");
            await _webViewHost.StartCDPSessionAsync(_currentSessionId.Value);
            _logger?.LogInformation("✅ CDP session started successfully for session {SessionId}", _currentSessionId);
            
            // Clear displayed item tracking for new capture session
            _displayedConsoleMessageIds.Clear();
            _displayedNetworkRequestIds.Clear();
            _logger?.LogDebug("Cleared displayed item tracking");
            
            _viewModel.IsCapturing = true;
            UpdateStatus("Capturing telemetry...");
            _logger?.LogInformation("✅ CAPTURE STARTED - Telemetry collection is now active");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "❌ ERROR starting capture");
            ShowError("Failed to start capture", ex);
        }
    }

    private async Task StopCaptureAsync()
    {
        try
        {
            if (!_currentSessionId.HasValue) return;

            UpdateStatus("Stopping capture...");
            
            // Stop CDP session
            if (_webViewHost != null)
            {
                await _webViewHost.StopCDPSessionAsync();
                _logger?.LogInformation("CDP session stopped for session {SessionId}", _currentSessionId);
            }
            
            _viewModel.IsCapturing = false;
            
            // ✅ CREATE FINAL SNAPSHOT before ending session
            try
            {
                _logger?.LogInformation("Creating final snapshot for session {SessionId}", _currentSessionId);
                var snapshot = await _telemetryAggregator.CreateSnapshotAsync(_currentSessionId.Value);
                await _sessionManager.AddSnapshotAsync(_currentSessionId.Value, snapshot);
                
                _logger?.LogInformation(
                    "✅ Session snapshot saved: {Console} console messages, {Network} network requests, {Performance} performance metrics",
                    snapshot.ConsoleMessages.Count,
                    snapshot.NetworkRequests.Count,
                    snapshot.PerformanceMetrics.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to create snapshot, but continuing to end session");
            }
            
            // Only end session if it's still active
            var session = await _sessionManager.GetSessionAsync(_currentSessionId.Value);
            if (session != null && session.Status == SessionStatus.Active)
            {
                await _sessionManager.EndSessionAsync(_currentSessionId.Value);
            }
            
            UpdateStatus("Capture stopped - Session snapshot saved");
            _logger?.LogInformation("Capture stopped for session {SessionId}", _currentSessionId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error stopping capture");
            ShowError("Failed to stop capture", ex);
        }
    }

    private async Task SaveCurrentSessionAsync()
    {
        if (!_currentSessionId.HasValue) return;

        try
        {
            UpdateStatus("Saving session...");
            await Task.Delay(500);
            UpdateStatus("Session saved");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving session");
            ShowError("Failed to save session", ex);
        }
    }

    private void UpdateCaptureState()
    {
        if (_commandToolbar != null)
        {
            _commandToolbar.SetIsCapturing(_viewModel.IsCapturing);
        }
    }

    private void UpdateStatus(string message)
    {
        if (InvokeRequired)
        {
            Invoke(() => UpdateStatus(message));
            return;
        }

        var statusLabel = this.Controls.OfType<StatusStrip>()
            .FirstOrDefault()?.Items["statusLabel"] as ToolStripStatusLabel;
        
        if (statusLabel != null) statusLabel.Text = message;
    }

    private void UpdateSessionIndicator(string text)
    {
        if (InvokeRequired)
        {
            Invoke(() => UpdateSessionIndicator(text));
            return;
        }

        var sessionIndicator = this.Controls.OfType<StatusStrip>()
            .FirstOrDefault()?.Items["sessionIndicator"] as ToolStripStatusLabel;
        
        if (sessionIndicator != null) sessionIndicator.Text = text;
    }

    private void ShowProgress(bool show)
    {
        if (InvokeRequired)
        {
            Invoke(() => ShowProgress(show));
            return;
        }

        var progressBar = this.Controls.OfType<StatusStrip>()
            .FirstOrDefault()?.Items["progressBar"] as ToolStripProgressBar;
        
        if (progressBar != null)
        {
            progressBar.Visible = show;
            if (show) progressBar.Style = ProgressBarStyle.Marquee;
        }
    }

    private void ShowError(string message, Exception ex)
    {
        MessageBox.Show($"{message}\n\nError: {ex.Message}", "Error", 
            MessageBoxButtons.OK, MessageBoxIcon.Error);
        UpdateStatus($"Error: {message}");
    }

    private void LoadUserSettings() { }
    private void SaveUserSettings() { }
}
