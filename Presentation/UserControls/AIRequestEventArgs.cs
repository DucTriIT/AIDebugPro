namespace AIDebugPro.Presentation.UserControls;

/// <summary>
/// Event args for AI request from context menu
/// </summary>
public class AIRequestEventArgs : EventArgs
{
    public object? TelemetryItem { get; set; }
    public string DefaultQuery { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty; // "Console", "Network", "Performance"
}
