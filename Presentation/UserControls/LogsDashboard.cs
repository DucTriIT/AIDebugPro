using AIDebugPro.Core.Models;

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

        // Color code by level
        item.BackColor = message.Level switch
        {
            ConsoleMessageLevel.Error => Color.LightPink,
            ConsoleMessageLevel.Warning => Color.LightYellow,
            ConsoleMessageLevel.Info => Color.LightCyan,
            _ => Color.White
        };

        _consoleListView.Items.Insert(0, item);

        // Limit to 1000 items
        while (_consoleListView.Items.Count > 1000)
        {
            _consoleListView.Items.RemoveAt(_consoleListView.Items.Count - 1);
        }
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
        item.SubItems.Add(request.Url);
        item.SubItems.Add($"{request.DurationMs:F0} ms");
        item.SubItems.Add(FormatFileSize(request.ResponseSize ?? 0));
        item.SubItems.Add(request.MimeType ?? "");

        // Color code by status
        if (request.IsFailed)
        {
            item.BackColor = Color.LightPink;
        }
        else if (request.StatusCode >= 400)
        {
            item.BackColor = Color.LightSalmon;
        }
        else if (request.DurationMs > 1000)
        {
            item.BackColor = Color.LightYellow;
        }

        _networkListView.Items.Insert(0, item);

        // Limit to 500 items
        while (_networkListView.Items.Count > 500)
        {
            _networkListView.Items.RemoveAt(_networkListView.Items.Count - 1);
        }
    }

    public void UpdatePerformanceMetrics(PerformanceMetrics metrics)
    {
        if (InvokeRequired)
        {
            Invoke(() => UpdatePerformanceMetrics(metrics));
            return;
        }

        if (_loadTimeLabel != null)
            _loadTimeLabel.Text = $"{metrics.LoadEventMs:F0} ms";

        if (_fcpLabel != null)
            _fcpLabel.Text = $"{metrics.FirstContentfulPaintMs:F0} ms";

        if (_lcpLabel != null)
            _lcpLabel.Text = $"{metrics.LargestContentfulPaintMs:F0} ms";

        if (_memoryLabel != null)
            _memoryLabel.Text = $"{metrics.MemoryUsageBytes / (1024.0 * 1024.0):F2} MB";

        if (_domNodesLabel != null)
            _domNodesLabel.Text = metrics.DomNodeCount.ToString();
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

    #endregion
}
