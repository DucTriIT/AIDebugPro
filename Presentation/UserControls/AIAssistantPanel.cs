using AIDebugPro.Core.Models;
using AIDebugPro.Presentation.ViewModels;
using AIDebugPro.AIIntegration;
using AIDebugPro.AIIntegration.Models;

namespace AIDebugPro.Presentation.UserControls;

/// <summary>
/// User control for AI chat interface
/// </summary>
public partial class AIAssistantPanel : UserControl
{
    private RichTextBox? _chatDisplay;
    private TextBox? _messageInput;
    private Button? _sendButton;
    private Button? _clearButton;
    private TabControl? _tabControl;
    private ListView? _issuesListView;
    private ListView? _recommendationsListView;
    private FlowLayoutPanel? _quickPromptsPanel;

    private AIAssistantViewModel? _viewModel;
    
    // ? NEW: AI Integration
    private AIDebugAssistant? _aiAssistant;
    private TelemetryContextBuilder? _contextBuilder;
    private Guid? _currentSessionId;
    private ActiveTab _currentTab = ActiveTab.Console;
    private Label? _typingIndicator;

    #region Events

    public event EventHandler<string>? MessageSent;
    public event EventHandler<List<Guid>>? OnHighlightRequested;

    #endregion

    public AIAssistantPanel()
    {
        InitializeComponent();
        InitializeControls();
    }

    // ? NEW: Initialize with AI services
    public void Initialize(
        AIDebugAssistant aiAssistant,
        TelemetryContextBuilder contextBuilder)
    {
        _aiAssistant = aiAssistant;
        _contextBuilder = contextBuilder;
    }

    // ? NEW: Set current context
    public void SetContext(Guid sessionId, ActiveTab tab)
    {
        _currentSessionId = sessionId;
        _currentTab = tab;
    }

    // ? NEW: Set query programmatically
    public void SetQuery(string query)
    {
        if (_messageInput != null)
        {
            _messageInput.Text = query;
            _messageInput.Focus();
        }
    }

    private void InitializeControls()
    {
        this.Dock = DockStyle.Fill;

        // Create tab control
        _tabControl = new TabControl
        {
            Dock = DockStyle.Fill
        };

        // Chat tab
        var chatTab = new TabPage("Chat");
        var chatPanel = CreateChatPanel();
        chatTab.Controls.Add(chatPanel);
        _tabControl.TabPages.Add(chatTab);

        // Issues tab
        var issuesTab = new TabPage("Issues");
        _issuesListView = CreateIssuesListView();
        issuesTab.Controls.Add(_issuesListView);
        _tabControl.TabPages.Add(issuesTab);

        // Recommendations tab
        var recommendationsTab = new TabPage("Recommendations");
        _recommendationsListView = CreateRecommendationsListView();
        recommendationsTab.Controls.Add(_recommendationsListView);
        _tabControl.TabPages.Add(recommendationsTab);

        this.Controls.Add(_tabControl);
    }

    private Panel CreateChatPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill };

        // ? NEW: Quick prompts panel
        _quickPromptsPanel = CreateQuickPromptsPanel();
        panel.Controls.Add(_quickPromptsPanel);

        // Chat display
        _chatDisplay = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BackColor = Color.White,
            Font = new Font("Segoe UI", 10)
        };
        panel.Controls.Add(_chatDisplay);

        // ? NEW: Typing indicator
        _typingIndicator = new Label
        {
            Text = "?? AI is thinking...",
            Dock = DockStyle.Bottom,
            Height = 25,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.Gray,
            Visible = false,
            Padding = new Padding(5, 0, 0, 0)
        };
        panel.Controls.Add(_typingIndicator);

        // Input panel
        var inputPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 80,
            Padding = new Padding(5)
        };

        // Clear button
        _clearButton = new Button
        {
            Text = "Clear",
            Dock = DockStyle.Right,
            Width = 60
        };
        _clearButton.Click += ClearButton_Click;
        inputPanel.Controls.Add(_clearButton);

        // Send button
        _sendButton = new Button
        {
            Text = "Send",
            Dock = DockStyle.Right,
            Width = 60
        };
        _sendButton.Click += SendButton_Click;
        inputPanel.Controls.Add(_sendButton);

        // Message input
        _messageInput = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            Font = new Font("Segoe UI", 10)
        };
        _messageInput.KeyDown += MessageInput_KeyDown;
        inputPanel.Controls.Add(_messageInput);

        panel.Controls.Add(inputPanel);

        return panel;
    }

    // ? NEW: Create quick prompts panel
    private FlowLayoutPanel CreateQuickPromptsPanel()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 60,
            Padding = new Padding(10),
            AutoSize = false
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
                Margin = new Padding(5),
                Height = 30
            };
            button.Click += (s, e) => OnQuickPrompt(text);
            panel.Controls.Add(button);
        }

        return panel;
    }

    // ? NEW: Handle quick prompts
    private void OnQuickPrompt(string prompt)
    {
        if (_messageInput != null)
        {
            _messageInput.Text = prompt;
            SendMessage();
        }
    }

    private ListView CreateIssuesListView()
    {
        var listView = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true
        };

        listView.Columns.Add("Severity", 80);
        listView.Columns.Add("Category", 120);
        listView.Columns.Add("Title", 200);
        listView.Columns.Add("Description", 300);

        return listView;
    }

    private ListView CreateRecommendationsListView()
    {
        var listView = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true
        };

        listView.Columns.Add("Priority", 80);
        listView.Columns.Add("Type", 120);
        listView.Columns.Add("Title", 200);
        listView.Columns.Add("Description", 300);

        return listView;
    }

    private void MessageInput_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.Enter)
        {
            SendMessage();
            e.SuppressKeyPress = true;
        }
    }

    private void SendButton_Click(object? sender, EventArgs e)
    {
        SendMessage();
    }

    private void ClearButton_Click(object? sender, EventArgs e)
    {
        _chatDisplay?.Clear();
        _viewModel?.Clear();
    }

    // ? ENHANCED: Send message with AI integration
    private async void SendMessage()
    {
        if (string.IsNullOrWhiteSpace(_messageInput?.Text))
            return;

        if (_aiAssistant == null || _contextBuilder == null)
        {
            AddMessageToChat("AI Assistant not initialized", false);
            return;
        }

        var userMessage = _messageInput.Text;
        _messageInput.Clear();

        try
        {
            // Add user message to chat
            AddMessageToChat(userMessage, isUser: true);
            
            // Show typing indicator
            ShowTypingIndicator(true);

            // Build telemetry context
            var context = await _contextBuilder.BuildContextAsync(
                _currentSessionId ?? Guid.Empty,
                _currentTab,
                selectedItem: null // TODO: Get from MainForm if available
            );

            // Get AI response
            var response = await _aiAssistant.AnalyzeAsync(userMessage, context);

            // Hide typing indicator
            ShowTypingIndicator(false);

            // Add AI response
            AddMessageToChat(response.Message, isUser: false);

            // Highlight related items
            if (response.RelatedTelemetryIds.Any())
            {
                OnHighlightRequested?.Invoke(this, response.RelatedTelemetryIds);
            }

            // Raise event
            MessageSent?.Invoke(this, userMessage);
        }
        catch (Exception ex)
        {
            ShowTypingIndicator(false);
            AddMessageToChat($"Error: {ex.Message}", isUser: false);
        }
    }

    // ? NEW: Show/hide typing indicator
    private void ShowTypingIndicator(bool show)
    {
        if (InvokeRequired)
        {
            Invoke(() => ShowTypingIndicator(show));
            return;
        }

        if (_typingIndicator != null)
        {
            _typingIndicator.Visible = show;
        }
    }

    public void AddMessageToChat(string message, bool isUser)
    {
        if (InvokeRequired)
        {
            Invoke(() => AddMessageToChat(message, isUser));
            return;
        }

        if (_chatDisplay == null)
            return;

        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var prefix = isUser ? "You" : "AI";
        var color = isUser ? Color.Blue : Color.Green;

        _chatDisplay.SelectionStart = _chatDisplay.TextLength;
        _chatDisplay.SelectionLength = 0;

        _chatDisplay.SelectionColor = Color.Gray;
        _chatDisplay.AppendText($"[{timestamp}] ");

        _chatDisplay.SelectionColor = color;
        _chatDisplay.SelectionFont = new Font(_chatDisplay.Font, FontStyle.Bold);
        _chatDisplay.AppendText($"{prefix}: ");

        _chatDisplay.SelectionColor = Color.Black;
        _chatDisplay.SelectionFont = new Font(_chatDisplay.Font, FontStyle.Regular);
        _chatDisplay.AppendText($"{message}\n\n");

        _chatDisplay.SelectionStart = _chatDisplay.TextLength;
        _chatDisplay.ScrollToCaret();

        _viewModel?.AddChatMessage(message, isUser);
    }

    // ? NEW: Show AI analysis
    public void ShowAnalysis(AIDebugResponse response)
    {
        AddMessageToChat(response.Message, isUser: false);
        _tabControl.SelectedIndex = 0; // Switch to Chat tab
    }

    public void SetViewModel(AIAssistantViewModel viewModel)
    {
        _viewModel = viewModel;
    }

    public void UpdateIssues(IEnumerable<Core.Models.Issue> issues)
    {
        if (InvokeRequired)
        {
            Invoke(() => UpdateIssues(issues));
            return;
        }

        _issuesListView?.Items.Clear();
        
        foreach (var issue in issues)
        {
            var item = new ListViewItem(issue.Severity.ToString());
            item.SubItems.Add(issue.Category.ToString());
            item.SubItems.Add(issue.Title);
            item.SubItems.Add(issue.Description);
            
            // Color code by severity
            item.BackColor = issue.Severity switch
            {
                IssueSeverity.Critical => Color.LightPink,
                IssueSeverity.High => Color.LightSalmon,
                IssueSeverity.Medium => Color.LightYellow,
                _ => Color.White
            };

            _issuesListView?.Items.Add(item);
        }
    }

    public void UpdateRecommendations(IEnumerable<Core.Models.Recommendation> recommendations)
    {
        if (InvokeRequired)
        {
            Invoke(() => UpdateRecommendations(recommendations));
            return;
        }

        _recommendationsListView?.Items.Clear();
        
        foreach (var rec in recommendations)
        {
            var item = new ListViewItem(rec.Priority.ToString());
            item.SubItems.Add(rec.Type.ToString());
            item.SubItems.Add(rec.Title);
            item.SubItems.Add(rec.Description);
            
            // Color code by priority
            item.BackColor = rec.Priority switch
            {
                RecommendationPriority.Critical => Color.LightPink,
                RecommendationPriority.High => Color.LightSalmon,
                RecommendationPriority.Medium => Color.LightYellow,
                _ => Color.White
            };

            _recommendationsListView?.Items.Add(item);
        }
    }
}
