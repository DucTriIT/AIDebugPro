namespace AIDebugPro.Presentation.UserControls;

/// <summary>
/// Toolbar with quick action buttons
/// </summary>
public partial class CommandToolbar : UserControl
{
    private Button? _startButton;
    private Button? _stopButton;
    private Button? _analyzeButton;
    private Button? _exportButton;
    private Label? _statusLabel;
    private bool _isCapturing;

    #region Events

    public event EventHandler? StartClicked;
    public event EventHandler? StopClicked;
    public event EventHandler? AnalyzeClicked;
    public event EventHandler? ExportClicked;

    #endregion

    #region Properties

    public bool GetIsCapturing()
    {
        return _isCapturing;
    }

    public void SetIsCapturing(bool value)
    {
        _isCapturing = value;
        UpdateButtonStates();
    }

    #endregion

    public CommandToolbar()
    {
        InitializeComponent();
        InitializeControls();
    }

    private void InitializeControls()
    {
        this.Dock = DockStyle.Top;
        this.Height = 50;
        this.BackColor = Color.FromArgb(240, 240, 240);
        this.Padding = new Padding(10);

        var flowPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };

        // Start button
        _startButton = new Button
        {
            Text = "▶ Start Capture",
            Width = 120,
            Height = 30,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(76, 175, 80),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };
        _startButton.Click += (s, e) => StartClicked?.Invoke(this, EventArgs.Empty);
        flowPanel.Controls.Add(_startButton);

        // Stop button
        _stopButton = new Button
        {
            Text = "■ Stop Capture",
            Width = 120,
            Height = 30,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(244, 67, 54),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Enabled = false
        };
        _stopButton.Click += (s, e) => StopClicked?.Invoke(this, EventArgs.Empty);
        flowPanel.Controls.Add(_stopButton);

        // Spacer
        flowPanel.Controls.Add(new Panel { Width = 20 });

        // Analyze button
        _analyzeButton = new Button
        {
            Text = "🤖 Analyze with AI",
            Width = 140,
            Height = 30,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(33, 150, 243),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };
        _analyzeButton.Click += (s, e) => AnalyzeClicked?.Invoke(this, EventArgs.Empty);
        flowPanel.Controls.Add(_analyzeButton);

        // Export button
        _exportButton = new Button
        {
            Text = "📄 Export Report",
            Width = 130,
            Height = 30,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(255, 152, 0),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };
        _exportButton.Click += (s, e) => ExportClicked?.Invoke(this, EventArgs.Empty);
        flowPanel.Controls.Add(_exportButton);

        // Spacer
        flowPanel.Controls.Add(new Panel { Width = 20 });

        // Status label
        _statusLabel = new Label
        {
            Text = "Ready",
            AutoSize = true,
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.Gray,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(10, 7, 0, 0)
        };
        flowPanel.Controls.Add(_statusLabel);

        this.Controls.Add(flowPanel);
    }

    private void UpdateButtonStates()
    {
        if (InvokeRequired)
        {
            Invoke(UpdateButtonStates);
            return;
        }

        if (_startButton != null)
            _startButton.Enabled = !_isCapturing;

        if (_stopButton != null)
            _stopButton.Enabled = _isCapturing;

        if (_statusLabel != null)
            _statusLabel.Text = _isCapturing ? "Capturing..." : "Ready";
    }

    public void SetStatus(string status)
    {
        if (InvokeRequired)
        {
            Invoke(() => SetStatus(status));
            return;
        }

        if (_statusLabel != null)
            _statusLabel.Text = status;
    }
}
