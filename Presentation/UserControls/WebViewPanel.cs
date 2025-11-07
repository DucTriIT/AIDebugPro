using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;

namespace AIDebugPro.Presentation.UserControls;

/// <summary>
/// User control for hosting WebView2 browser
/// </summary>
public partial class WebViewPanel : UserControl
{
    private WebView2? _webView;
    private TextBox? _urlTextBox;
    private Button? _goButton;
    private Button? _backButton;
    private Button? _forwardButton;
    private Button? _refreshButton;
    private ToolStrip? _toolbar;

    public string CurrentUrl => _webView?.Source?.ToString() ?? string.Empty;

    #region Events

    public event EventHandler<string>? NavigationCompleted;
    public event EventHandler<string>? NavigationFailed;

    #endregion

    public WebViewPanel()
    {
        InitializeComponent();
        InitializeControls();
    }

    private void InitializeControls()
    {
        this.Dock = DockStyle.Fill;

        // Create toolbar
        _toolbar = new ToolStrip
        {
            Dock = DockStyle.Top,
            GripStyle = ToolStripGripStyle.Hidden
        };

        // Back button
        _backButton = new Button
        {
            Text = "?",
            Width = 30,
            Enabled = false
        };
        _backButton.Click += (s, e) => _webView?.GoBack();
        var backItem = new ToolStripControlHost(_backButton);
        _toolbar.Items.Add(backItem);

        // Forward button
        _forwardButton = new Button
        {
            Text = "?",
            Width = 30,
            Enabled = false
        };
        _forwardButton.Click += (s, e) => _webView?.GoForward();
        var forwardItem = new ToolStripControlHost(_forwardButton);
        _toolbar.Items.Add(forwardItem);

        // Refresh button
        _refreshButton = new Button
        {
            Text = "?",
            Width = 30
        };
        _refreshButton.Click += (s, e) => _webView?.Reload();
        var refreshItem = new ToolStripControlHost(_refreshButton);
        _toolbar.Items.Add(refreshItem);

        // URL textbox
        _urlTextBox = new TextBox
        {
            Width = 400,
            Text = "https://example.com"
        };
        _urlTextBox.KeyDown += UrlTextBox_KeyDown;
        var urlItem = new ToolStripControlHost(_urlTextBox)
        {
            AutoSize = true
        };
        _toolbar.Items.Add(urlItem);

        // Go button
        _goButton = new Button
        {
            Text = "Go",
            Width = 50
        };
        _goButton.Click += GoButton_Click;
        var goItem = new ToolStripControlHost(_goButton);
        _toolbar.Items.Add(goItem);

        this.Controls.Add(_toolbar);

        // WebView2
        _webView = new WebView2
        {
            Dock = DockStyle.Fill
        };
        this.Controls.Add(_webView);
    }

    public async Task InitializeAsync()
    {
        if (_webView == null)
            return;

        try
        {
            await _webView.EnsureCoreWebView2Async();

            // Set up event handlers
            _webView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
            _webView.CoreWebView2.SourceChanged += CoreWebView2_SourceChanged;
            _webView.CoreWebView2.HistoryChanged += CoreWebView2_HistoryChanged;

            // Navigate to initial URL
            if (!string.IsNullOrEmpty(_urlTextBox?.Text))
            {
                _webView.CoreWebView2.Navigate(_urlTextBox.Text);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to initialize WebView2: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void UrlTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            NavigateToUrl();
            e.SuppressKeyPress = true;
        }
    }

    private void GoButton_Click(object? sender, EventArgs e)
    {
        NavigateToUrl();
    }

    private void NavigateToUrl()
    {
        if (_webView?.CoreWebView2 == null || string.IsNullOrEmpty(_urlTextBox?.Text))
            return;

        var url = _urlTextBox.Text;
        
        // Add https:// if no protocol specified
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            url = "https://" + url;
        }

        _webView.CoreWebView2.Navigate(url);
    }

    private void CoreWebView2_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (InvokeRequired)
        {
            Invoke(() => CoreWebView2_NavigationCompleted(sender, e));
            return;
        }

        if (e.IsSuccess)
        {
            if (_urlTextBox != null && _webView?.Source != null)
            {
                _urlTextBox.Text = _webView.Source.ToString();
            }
            NavigationCompleted?.Invoke(this, _webView?.Source?.ToString() ?? string.Empty);
        }
        else
        {
            NavigationFailed?.Invoke(this, $"Navigation failed with error: {e.WebErrorStatus}");
        }
    }

    private void CoreWebView2_SourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e)
    {
        if (InvokeRequired)
        {
            Invoke(() => CoreWebView2_SourceChanged(sender, e));
            return;
        }

        if (_urlTextBox != null && _webView?.Source != null)
        {
            _urlTextBox.Text = _webView.Source.ToString();
        }
    }

    private void CoreWebView2_HistoryChanged(object? sender, object e)
    {
        if (InvokeRequired)
        {
            Invoke(() => CoreWebView2_HistoryChanged(sender, e));
            return;
        }

        if (_backButton != null)
            _backButton.Enabled = _webView?.CanGoBack ?? false;

        if (_forwardButton != null)
            _forwardButton.Enabled = _webView?.CanGoForward ?? false;
    }

    public void Reload()
    {
        _webView?.Reload();
    }

    public WebView2? GetWebView2() => _webView;
}
