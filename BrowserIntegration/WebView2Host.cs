using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Extensions.Logging;
using AIDebugPro.Core.Interfaces;
using AIDebugPro.Core.Exceptions;
using System.Text.Json;

namespace AIDebugPro.BrowserIntegration;

/// <summary>
/// Manages WebView2 control and CDP communication
/// </summary>
public class WebView2Host : IDisposable
{
    private readonly WebView2 _webView;
    private readonly ITelemetryAggregator _telemetryAggregator;
    private readonly ILogger<WebView2Host>? _logger;
    private Guid _currentSessionId;
    private bool _isInitialized;
    
    // CDP Listeners
    private DevToolsProtocol.ConsoleListener? _consoleListener;
    private DevToolsProtocol.NetworkListener? _networkListener;
    private DevToolsProtocol.PerformanceCollector? _performanceCollector;
    private DevToolsProtocol.DOMSnapshotManager? _domSnapshotManager;

    public WebView2Host(
        WebView2 webView,
        ITelemetryAggregator telemetryAggregator,
        ILogger<WebView2Host>? logger = null)
    {
        _webView = webView ?? throw new ArgumentNullException(nameof(webView));
        _telemetryAggregator = telemetryAggregator ?? throw new ArgumentNullException(nameof(telemetryAggregator));
        _logger = logger;
    }

    #region Initialization

    /// <summary>
    /// Initializes WebView2 with CDP enabled
    /// </summary>
    public async Task InitializeAsync(string? userDataFolder = null)
    {
        if (_isInitialized)
        {
            _logger?.LogWarning("WebView2 already initialized");
            return;
        }

        try
        {
            _logger?.LogInformation("Initializing WebView2...");

            // Set user data folder
            userDataFolder ??= Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AIDebugPro",
                "WebView2");

            var environment = await CoreWebView2Environment.CreateAsync(
                browserExecutableFolder: null,
                userDataFolder: userDataFolder,
                options: new CoreWebView2EnvironmentOptions
                {
                    AdditionalBrowserArguments = "--enable-features=NetworkService,NetworkServiceInProcess"
                });

            await _webView.EnsureCoreWebView2Async(environment);

            // Set up event handlers
            _webView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
            _webView.CoreWebView2.SourceChanged += OnSourceChanged;
            _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
            _webView.CoreWebView2.ProcessFailed += OnProcessFailed;

            _isInitialized = true;

            _logger?.LogInformation("WebView2 initialized successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize WebView2");
            throw new WebView2InitializationException("WebView2 initialization failed", ex);
        }
    }

    /// <summary>
    /// Starts CDP session and enables listeners
    /// </summary>
    public async Task StartCDPSessionAsync(Guid sessionId)
    {
        if (!_isInitialized)
            throw new InvalidOperationException("WebView2 not initialized");

        _currentSessionId = sessionId;

        try
        {
            _logger?.LogInformation("Starting CDP session for {SessionId}", sessionId);

            // Initialize CDP listeners
            _consoleListener = new DevToolsProtocol.ConsoleListener(
                _webView.CoreWebView2,
                _telemetryAggregator,
                _logger);

            _networkListener = new DevToolsProtocol.NetworkListener(
                _webView.CoreWebView2,
                _telemetryAggregator,
                _logger);

            _performanceCollector = new DevToolsProtocol.PerformanceCollector(
                _webView.CoreWebView2,
                _telemetryAggregator,
                _logger);

            _domSnapshotManager = new DevToolsProtocol.DOMSnapshotManager(
                _webView.CoreWebView2,
                _telemetryAggregator,
                _logger);

            // Start listeners
            await _consoleListener.StartAsync(sessionId);
            await _networkListener.StartAsync(sessionId);
            await _performanceCollector.StartAsync(sessionId);
            await _domSnapshotManager.StartAsync(sessionId);

            _logger?.LogInformation("CDP session started successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to start CDP session");
            throw new CDPException("Failed to start CDP session", ex);
        }
    }

    /// <summary>
    /// Stops CDP session and disables listeners
    /// </summary>
    public async Task StopCDPSessionAsync()
    {
        try
        {
            _logger?.LogInformation("Stopping CDP session");

            if (_consoleListener != null)
                await _consoleListener.StopAsync();

            if (_networkListener != null)
                await _networkListener.StopAsync();

            if (_performanceCollector != null)
                await _performanceCollector.StopAsync();

            if (_domSnapshotManager != null)
                await _domSnapshotManager.StopAsync();

            _logger?.LogInformation("CDP session stopped");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error stopping CDP session");
        }
    }

    #endregion

    #region Navigation

    /// <summary>
    /// Navigates to a URL
    /// </summary>
    public async Task NavigateAsync(string url)
    {
        if (!_isInitialized)
            throw new InvalidOperationException("WebView2 not initialized");

        try
        {
            _logger?.LogInformation("Navigating to {Url}", url);
            _webView.CoreWebView2.Navigate(url);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Navigation failed for {Url}", url);
            throw;
        }
    }

    /// <summary>
    /// Reloads the current page
    /// </summary>
    public void Reload()
    {
        if (!_isInitialized)
            throw new InvalidOperationException("WebView2 not initialized");

        _webView.CoreWebView2.Reload();
        _logger?.LogDebug("Page reloaded");
    }

    /// <summary>
    /// Stops the current navigation
    /// </summary>
    public void Stop()
    {
        if (!_isInitialized)
            throw new InvalidOperationException("WebView2 not initialized");

        _webView.CoreWebView2.Stop();
        _logger?.LogDebug("Navigation stopped");
    }

    #endregion

    #region JavaScript Execution

    /// <summary>
    /// Executes JavaScript in the page
    /// </summary>
    public async Task<string> ExecuteScriptAsync(string script)
    {
        if (!_isInitialized)
            throw new InvalidOperationException("WebView2 not initialized");

        try
        {
            var result = await _webView.CoreWebView2.ExecuteScriptAsync(script);
            _logger?.LogDebug("Script executed successfully");
            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Script execution failed");
            throw;
        }
    }

    /// <summary>
    /// Injects JavaScript file into the page
    /// </summary>
    public async Task InjectScriptFileAsync(string scriptPath)
    {
        if (!File.Exists(scriptPath))
            throw new FileNotFoundException("Script file not found", scriptPath);

        var script = await File.ReadAllTextAsync(scriptPath);
        await ExecuteScriptAsync(script);

        _logger?.LogDebug("Injected script from {Path}", scriptPath);
    }

    #endregion

    #region CDP Commands

    /// <summary>
    /// Captures a screenshot
    /// </summary>
    public async Task<byte[]> CaptureScreenshotAsync()
    {
        try
        {
            var result = await _webView.CoreWebView2.CallDevToolsProtocolMethodAsync(
                "Page.captureScreenshot",
                "{}");

            var json = JsonDocument.Parse(result);
            var base64 = json.RootElement.GetProperty("data").GetString();
            
            return Convert.FromBase64String(base64 ?? string.Empty);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Screenshot capture failed");
            throw;
        }
    }

    /// <summary>
    /// Gets current DOM snapshot
    /// </summary>
    public async Task CaptureDOMSnapshotAsync()
    {
        if (_domSnapshotManager != null)
        {
            await _domSnapshotManager.CaptureSnapshotAsync(_currentSessionId);
        }
    }

    /// <summary>
    /// Gets current performance metrics
    /// </summary>
    public async Task CapturePerformanceMetricsAsync()
    {
        if (_performanceCollector != null)
        {
            await _performanceCollector.CollectMetricsAsync(_currentSessionId);
        }
    }

    #endregion

    #region Event Handlers

    private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        _logger?.LogInformation(
            "Navigation completed: {Url}, Success: {Success}",
            _webView.Source,
            e.IsSuccess);
    }

    private void OnSourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e)
    {
        _logger?.LogDebug("Source changed to {Url}", _webView.Source);
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        _logger?.LogDebug("Web message received: {Message}", e.TryGetWebMessageAsString());
    }

    private void OnProcessFailed(object? sender, CoreWebView2ProcessFailedEventArgs e)
    {
        _logger?.LogError(
            "WebView2 process failed: {Reason}",
            e.Reason);
    }

    #endregion

    #region Properties

    public bool IsInitialized => _isInitialized;
    
    public string CurrentUrl => _webView.Source?.ToString() ?? string.Empty;

    public CoreWebView2? CoreWebView2 => _webView.CoreWebView2;

    #endregion

    public void Dispose()
    {
        _consoleListener?.Dispose();
        _networkListener?.Dispose();
        _performanceCollector?.Dispose();
        _domSnapshotManager?.Dispose();
        
        if (_webView.CoreWebView2 != null)
        {
            _webView.CoreWebView2.NavigationCompleted -= OnNavigationCompleted;
            _webView.CoreWebView2.SourceChanged -= OnSourceChanged;
            _webView.CoreWebView2.WebMessageReceived -= OnWebMessageReceived;
            _webView.CoreWebView2.ProcessFailed -= OnProcessFailed;
        }

        _logger?.LogDebug("WebView2Host disposed");
    }
}
