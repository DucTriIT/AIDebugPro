using AIDebugPro.Core.Models;
using AIDebugPro.AIIntegration.Models;

namespace AIDebugPro.Presentation.UserControls;

/// <summary>
/// User control for displaying telemetry logs and metrics
/// </summary>
public partial class LogsDashboard : UserControl
{
    private TabControl? _tabControl;
    private ListView? _consoleListView;
    private ListView? _networkListView;
    private Panel? _performancePanel;
    private Label? _loadTimeLabel;
    private Label? _fcpLabel;
    private Label? _lcpLabel;
    private Label? _memoryLabel;
    private Label? _domNodesLabel;

    // ? NEW: Events for AI integration
    public event EventHandler<AIRequestEventArgs>? OnAskAIRequested;

    public LogsDashboard()
    {
        InitializeComponent();
        InitializeControls();
    }

    private void InitializeControls()
    {
        this.Dock = DockStyle.Fill;

        _tabControl = new TabControl
        {
            Dock = DockStyle.Fill
        };

        // Console tab
        var consoleTab = new TabPage("Console");
        _consoleListView = CreateConsoleListView();
        consoleTab.Controls.Add(_consoleListView);
        _tabControl.TabPages.Add(consoleTab);

        // Network tab
        var networkTab = new TabPage("Network");
        _networkListView = CreateNetworkListView();
        networkTab.Controls.Add(_networkListView);
        _tabControl.TabPages.Add(networkTab);

        // Performance tab
        var performanceTab = new TabPage("Performance");
        _performancePanel = CreatePerformancePanel();
        performanceTab.Controls.Add(_performancePanel);
        _tabControl.TabPages.Add(performanceTab);

        this.Controls.Add(_tabControl);
    }

    private ListView CreateConsoleListView()
    {
        var listView = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true
        };

        listView.Columns.Add("Time", 80);
        listView.Columns.Add("Level", 80);
        listView.Columns.Add("Message", 400);
        listView.Columns.Add("Source", 200);
        listView.Columns.Add("Line", 60);

        // ? NEW: Add context menu
        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("?? Ask AI about this error", null, OnAskAIAboutError);
        contextMenu.Items.Add("?? Explain this message", null, OnExplainMessage);
        contextMenu.Items.Add("?? Copy message", null, OnCopyConsoleMessage);
        
        listView.ContextMenuStrip = contextMenu;

        return listView;
    }

    private ListView CreateNetworkListView()
    {
        var listView = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true
        };

        listView.Columns.Add("Method", 60);
        listView.Columns.Add("Status", 60);
        listView.Columns.Add("URL", 300);
        listView.Columns.Add("Duration", 80);
        listView.Columns.Add("Size", 80);
        listView.Columns.Add("Type", 100);

        // ? NEW: Add context menu
        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("?? Analyze this request", null, OnAnalyzeNetworkRequest);
        contextMenu.Items.Add("?? Why did this fail?", null, OnAnalyzeNetworkFailure);
        contextMenu.Items.Add("?? Copy URL", null, OnCopyURL);
        
        listView.ContextMenuStrip = contextMenu;

        return listView;
    }

    private Panel CreatePerformancePanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20)
        };

        var y = 20;
        var spacing = 40;

        // Load Time
        var loadTimeTitle = new Label
        {
            Text = "Load Time:",
            Location = new Point(20, y),
            Width = 150,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        panel.Controls.Add(loadTimeTitle);

        _loadTimeLabel = new Label
        {
            Text = "0 ms",
            Location = new Point(180, y),
            Width = 200,
            Font = new Font("Segoe UI", 10)
        };
        panel.Controls.Add(_loadTimeLabel);

        y += spacing;

        // First Contentful Paint
        var fcpTitle = new Label
        {
            Text = "First Contentful Paint:",
            Location = new Point(20, y),
            Width = 150,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        panel.Controls.Add(fcpTitle);

        _fcpLabel = new Label
        {
            Text = "0 ms",
            Location = new Point(180, y),
            Width = 200,
            Font = new Font("Segoe UI", 10)
        };
        panel.Controls.Add(_fcpLabel);

        y += spacing;

        // Largest Contentful Paint
        var lcpTitle = new Label
        {
            Text = "Largest Contentful Paint:",
            Location = new Point(20, y),
            Width = 150,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        panel.Controls.Add(lcpTitle);

        _lcpLabel = new Label
        {
            Text = "0 ms",
            Location = new Point(180, y),
            Width = 200,
            Font = new Font("Segoe UI", 10)
        };
        panel.Controls.Add(_lcpLabel);

        y += spacing;

        // Memory Usage
        var memoryTitle = new Label
        {
            Text = "Memory Usage:",
            Location = new Point(20, y),
            Width = 150,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        panel.Controls.Add(memoryTitle);

        _memoryLabel = new Label
        {
            Text = "0 MB",
            Location = new Point(180, y),
            Width = 200,
            Font = new Font("Segoe UI", 10)
        };
        panel.Controls.Add(_memoryLabel);

        y += spacing;

        // DOM Nodes
        var domNodesTitle = new Label
        {
            Text = "DOM Nodes:",
            Location = new Point(20, y),
            Width = 150,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        panel.Controls.Add(domNodesTitle);

        _domNodesLabel = new Label
        {
            Text = "0",
            Location = new Point(180, y),
            Width = 200,
            Font = new Font("Segoe UI", 10)
        };
        panel.Controls.Add(_domNodesLabel);

        return panel;
    }

    #region Public Methods

    public void ShowConsoleTab()
    {
        if (_tabControl != null)
            _tabControl.SelectedIndex = 0;
    }

    public void ShowNetworkTab()
    {
        if (_tabControl != null)
            _tabControl.SelectedIndex = 1;
    }

    public void ShowPerformanceTab()
    {
        if (_tabControl != null)
            _tabControl.SelectedIndex = 2;
    }

    // ? NEW: Context menu handlers
    private void OnAskAIAboutError(object? sender, EventArgs e)
    {
        var selected = GetSelectedConsoleMessage();
        if (selected == null) return;

        OnAskAIRequested?.Invoke(this, new AIRequestEventArgs
        {
            TelemetryItem = selected,
            DefaultQuery = $"Explain this error: {selected.Message.Substring(0, Math.Min(100, selected.Message.Length))}",
            ItemType = "Console"
        });
    }

    private void OnExplainMessage(object? sender, EventArgs e)
    {
        var selected = GetSelectedConsoleMessage();
        if (selected == null) return;

        OnAskAIRequested?.Invoke(this, new AIRequestEventArgs
        {
            TelemetryItem = selected,
            DefaultQuery = $"What does this console message mean? {selected.Message}",
            ItemType = "Console"
        });
    }

    private void OnCopyConsoleMessage(object? sender, EventArgs e)
    {
        var selected = GetSelectedConsoleMessage();
        if (selected != null && !string.IsNullOrEmpty(selected.Message))
        {
            Clipboard.SetText(selected.Message);
        }
    }

    private void OnAnalyzeNetworkRequest(object? sender, EventArgs e)
    {
        var selected = GetSelectedNetworkRequest();
        if (selected == null) return;

        OnAskAIRequested?.Invoke(this, new AIRequestEventArgs
        {
            TelemetryItem = selected,
            DefaultQuery = $"Analyze this network request: {selected.Method} {selected.Url} -> {selected.StatusCode}",
            ItemType = "Network"
        });
    }

    private void OnAnalyzeNetworkFailure(object? sender, EventArgs e)
    {
        var selected = GetSelectedNetworkRequest();
        if (selected == null) return;

        OnAskAIRequested?.Invoke(this, new AIRequestEventArgs
        {
            TelemetryItem = selected,
            DefaultQuery = $"Why did this network request fail? {selected.Method} {selected.Url} returned {selected.StatusCode}",
            ItemType = "Network"
        });
    }

    private void OnCopyURL(object? sender, EventArgs e)
    {
        var selected = GetSelectedNetworkRequest();
        if (selected != null && !string.IsNullOrEmpty(selected.Url))
        {
            Clipboard.SetText(selected.Url);
        }
    }

    // ? NEW: Get selected items
    private ConsoleMessage? GetSelectedConsoleMessage()
    {
        if (_consoleListView?.SelectedItems.Count > 0)
        {
            return _consoleListView.SelectedItems[0].Tag as ConsoleMessage;
        }
        return null;
    }

    private NetworkRequest? GetSelectedNetworkRequest()
    {
        if (_networkListView?.SelectedItems.Count > 0)
        {
            return _networkListView.SelectedItems[0].Tag as NetworkRequest;
        }
        return null;
    }

    #endregion

    #region Helper Methods

    private string FormatFileSize(long bytes)
    {
        if (bytes == 0) return "0 B";

        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    // ? NEW: Highlight items requested by AI
    public void HighlightItems(List<Guid> ids)
    {
        if (InvokeRequired)
        {
            Invoke(() => HighlightItems(ids));
            return;
        }

        // Highlight in console tab
        if (_consoleListView != null)
        {
            foreach (ListViewItem item in _consoleListView.Items)
            {
                if (item.Tag is ConsoleMessage msg && ids.Contains(msg.Id))
                {
                    item.BackColor = Color.LightGreen;
                    item.Font = new Font(item.Font, FontStyle.Bold);
                }
            }
        }

        // Highlight in network tab
        if (_networkListView != null)
        {
            foreach (ListViewItem item in _networkListView.Items)
            {
                if (item.Tag is NetworkRequest req && ids.Contains(req.Id))
                {
                    item.BackColor = Color.LightGreen;
                    item.Font = new Font(item.Font, FontStyle.Bold);
                }
            }
        }
    }

    public void ClearAll()
    {
        _consoleListView?.Items.Clear();
        _networkListView?.Items.Clear();

        if (_loadTimeLabel != null) _loadTimeLabel.Text = "0 ms";
        if (_fcpLabel != null) _fcpLabel.Text = "0 ms";
        if (_lcpLabel != null) _lcpLabel.Text = "0 ms";
        if (_memoryLabel != null) _memoryLabel.Text = "0 MB";
        if (_domNodesLabel != null) _domNodesLabel.Text = "0";
    }

    public void AddConsoleMessage(ConsoleMessage message)
    {
        if (InvokeRequired)
        {
            Invoke(() => AddConsoleMessage(message));
            return;
        }

        if (_consoleListView == null)
            return;

        var item = new ListViewItem(message.Timestamp.ToString("HH:mm:ss.fff"));
        item.SubItems.Add(message.Level.ToString());
        item.SubItems.Add(message.Message);
        item.SubItems.Add(message.Source ?? "");
        item.SubItems.Add(message.LineNumber.ToString());

        // ? Store the message object in Tag for context menu
        item.Tag = message;

        // Color code by level
        item.BackColor = message.Level switch
        {
            ConsoleMessageLevel.Error => Color.LightPink,
            ConsoleMessageLevel.Warning => Color.LightYellow,
            ConsoleMessageLevel.Info => Color.LightCyan,
            _ => Color.White
        };

        _consoleListView.Items.Add(item);
    }

    public void AddNetworkRequest(NetworkRequest request)
    {
        if (InvokeRequired)
        {
            Invoke(() => AddNetworkRequest(request));
            return;
        }

        if (_networkListView == null)
            return;

        var item = new ListViewItem(request.Method);
        item.SubItems.Add(request.StatusCode.ToString());
        item.SubItems.Add(request.Url ?? "");
        item.SubItems.Add($"{request.DurationMs:F2} ms");
        item.SubItems.Add($"{(request.ResponseSize ?? 0) / 1024.0:F2} KB"); // Format bytes
        item.SubItems.Add(request.MimeType ?? "");

        // ? Store the request object in Tag for context menu
        item.Tag = request;

        // Color code by status
        if (request.IsFailed || request.StatusCode >= 400)
        {
            item.BackColor = Color.LightSalmon;
        }
        else if (request.StatusCode >= 300)
        {
            item.BackColor = Color.LightYellow;
        }

        _networkListView.Items.Add(item);
    }

    public void UpdatePerformanceMetrics(PerformanceMetrics metrics)
    {
        if (InvokeRequired)
        {
            Invoke(() => UpdatePerformanceMetrics(metrics));
            return;
        }

        if (_loadTimeLabel != null)
            _loadTimeLabel.Text = $"Load Time: {metrics.LoadEventMs:F2} ms";

        if (_fcpLabel != null)
            _fcpLabel.Text = $"First Contentful Paint: {metrics.FirstContentfulPaintMs:F2} ms";

        if (_lcpLabel != null)
            _lcpLabel.Text = $"Largest Contentful Paint: {metrics.LargestContentfulPaintMs:F2} ms";

        if (_memoryLabel != null)
            _memoryLabel.Text = $"Memory Usage: {metrics.MemoryUsageBytes / 1024 / 1024:F2} MB";

        if (_domNodesLabel != null)
            _domNodesLabel.Text = $"DOM Nodes: {metrics.DomNodeCount}";
    }
    #endregion
}
