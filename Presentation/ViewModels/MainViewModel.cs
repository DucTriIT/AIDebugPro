using System.ComponentModel;
using System.Runtime.CompilerServices;
using AIDebugPro.Core.Models;

namespace AIDebugPro.Presentation.ViewModels;

/// <summary>
/// Main view model for application state
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    private bool _isCapturing;
    private string _statusMessage = "Ready";
    private string _currentUrl = string.Empty;
    private Guid? _currentSessionId;
    private DebugSession? _currentSession;
    private bool _isBusy;
    private int _consoleMessageCount;
    private int _networkRequestCount;
    private int _errorCount;
    private int _warningCount;

    #region Properties

    /// <summary>
    /// Whether telemetry capture is active
    /// </summary>
    public bool IsCapturing
    {
        get => _isCapturing;
        set => SetProperty(ref _isCapturing, value);
    }

    /// <summary>
    /// Current status message
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>
    /// Current browser URL
    /// </summary>
    public string CurrentUrl
    {
        get => _currentUrl;
        set => SetProperty(ref _currentUrl, value);
    }

    /// <summary>
    /// Current session ID
    /// </summary>
    public Guid? CurrentSessionId
    {
        get => _currentSessionId;
        set => SetProperty(ref _currentSessionId, value);
    }

    /// <summary>
    /// Current debug session
    /// </summary>
    public DebugSession? CurrentSession
    {
        get => _currentSession;
        set => SetProperty(ref _currentSession, value);
    }

    /// <summary>
    /// Whether a long-running operation is in progress
    /// </summary>
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    /// <summary>
    /// Total console messages captured
    /// </summary>
    public int ConsoleMessageCount
    {
        get => _consoleMessageCount;
        set => SetProperty(ref _consoleMessageCount, value);
    }

    /// <summary>
    /// Total network requests captured
    /// </summary>
    public int NetworkRequestCount
    {
        get => _networkRequestCount;
        set => SetProperty(ref _networkRequestCount, value);
    }

    /// <summary>
    /// Total error count
    /// </summary>
    public int ErrorCount
    {
        get => _errorCount;
        set => SetProperty(ref _errorCount, value);
    }

    /// <summary>
    /// Total warning count
    /// </summary>
    public int WarningCount
    {
        get => _warningCount;
        set => SetProperty(ref _warningCount, value);
    }

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Updates telemetry counts
    /// </summary>
    public void UpdateTelemetryCounts(int consoleMessages, int networkRequests, int errors, int warnings)
    {
        ConsoleMessageCount = consoleMessages;
        NetworkRequestCount = networkRequests;
        ErrorCount = errors;
        WarningCount = warnings;
    }

    /// <summary>
    /// Resets all counters
    /// </summary>
    public void ResetCounters()
    {
        ConsoleMessageCount = 0;
        NetworkRequestCount = 0;
        ErrorCount = 0;
        WarningCount = 0;
    }

    #endregion
}
