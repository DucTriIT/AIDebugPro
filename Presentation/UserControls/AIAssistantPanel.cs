using AIDebugPro.Core.Models;
using AIDebugPro.Presentation.ViewModels;

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

    private AIAssistantViewModel? _viewModel;

    #region Events

    public event EventHandler<string>? MessageSent;

    #endregion

    public AIAssistantPanel()
    {
        InitializeComponent();
        InitializeControls();
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

        // Chat display
        _chatDisplay = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BackColor = Color.White,
            Font = new Font("Segoe UI", 10)
        };
        panel.Controls.Add(_chatDisplay);

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

    private void SendMessage()
    {
        if (string.IsNullOrWhiteSpace(_messageInput?.Text))
            return;

        var message = _messageInput.Text;

        // Add user message to chat
        AddMessageToChat(message, true);

        // Raise event
        MessageSent?.Invoke(this, message);

        // Clear input
        _messageInput.Clear();

        // Simulate AI response (will be replaced with actual AI integration)
        Task.Run(async () =>
        {
            await Task.Delay(1000);
            Invoke(() => AddMessageToChat("Processing your request...", false));
        });
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
