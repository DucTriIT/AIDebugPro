using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using AIDebugPro.Core.Models;

namespace AIDebugPro.Presentation.ViewModels;

/// <summary>
/// View model for AI Assistant panel
/// </summary>
public class AIAssistantViewModel : INotifyPropertyChanged
{
    private bool _isAnalyzing;
    private string _currentMessage = string.Empty;
    private AIAnalysisResult? _lastAnalysis;

    #region Properties

    /// <summary>
    /// Whether AI analysis is in progress
    /// </summary>
    public bool IsAnalyzing
    {
        get => _isAnalyzing;
        set => SetProperty(ref _isAnalyzing, value);
    }

    /// <summary>
    /// Current message being composed
    /// </summary>
    public string CurrentMessage
    {
        get => _currentMessage;
        set => SetProperty(ref _currentMessage, value);
    }

    /// <summary>
    /// Last AI analysis result
    /// </summary>
    public AIAnalysisResult? LastAnalysis
    {
        get => _lastAnalysis;
        set => SetProperty(ref _lastAnalysis, value);
    }

    /// <summary>
    /// Chat history
    /// </summary>
    public ObservableCollection<ChatMessage> ChatHistory { get; } = new();

    /// <summary>
    /// AI insights and recommendations
    /// </summary>
    public ObservableCollection<Issue> Issues { get; } = new();

    /// <summary>
    /// AI recommendations
    /// </summary>
    public ObservableCollection<Recommendation> Recommendations { get; } = new();

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
    /// Adds a chat message
    /// </summary>
    public void AddChatMessage(string message, bool isUser)
    {
        ChatHistory.Add(new ChatMessage
        {
            Message = message,
            IsUser = isUser,
            Timestamp = DateTime.Now
        });
    }

    /// <summary>
    /// Updates analysis results
    /// </summary>
    public void UpdateAnalysisResults(AIAnalysisResult result)
    {
        LastAnalysis = result;
        
        Issues.Clear();
        foreach (var issue in result.Issues)
        {
            Issues.Add(issue);
        }

        Recommendations.Clear();
        foreach (var rec in result.Recommendations)
        {
            Recommendations.Add(rec);
        }
    }

    /// <summary>
    /// Clears all chat and analysis data
    /// </summary>
    public void Clear()
    {
        ChatHistory.Clear();
        Issues.Clear();
        Recommendations.Clear();
        LastAnalysis = null;
        CurrentMessage = string.Empty;
    }

    #endregion
}

/// <summary>
/// Represents a chat message
/// </summary>
public class ChatMessage
{
    public string Message { get; set; } = string.Empty;
    public bool IsUser { get; set; }
    public DateTime Timestamp { get; set; }
}
