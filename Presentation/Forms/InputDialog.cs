namespace AIDebugPro.Presentation.Forms;

/// <summary>
/// Simple input dialog for getting text input from user
/// </summary>
public class InputDialog : Form
{
    private Label? _promptLabel;
    private TextBox? _inputTextBox;
    private Button? _okButton;
    private Button? _cancelButton;

    public string InputText => _inputTextBox?.Text ?? string.Empty;

    public InputDialog(string title, string prompt, string defaultValue = "")
    {
        InitializeDialog(title, prompt, defaultValue);
    }

    private void InitializeDialog(string title, string prompt, string defaultValue)
    {
        this.Text = title;
        this.Size = new Size(400, 150);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.StartPosition = FormStartPosition.CenterParent;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        _promptLabel = new Label
        {
            Text = prompt,
            Location = new Point(12, 15),
            Size = new Size(360, 20),
            AutoSize = false
        };
        this.Controls.Add(_promptLabel);

        _inputTextBox = new TextBox
        {
            Location = new Point(12, 40),
            Size = new Size(360, 23),
            Text = defaultValue
        };
        _inputTextBox.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        };
        this.Controls.Add(_inputTextBox);

        _okButton = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Location = new Point(216, 75),
            Size = new Size(75, 26)
        };
        this.Controls.Add(_okButton);
        this.AcceptButton = _okButton;

        _cancelButton = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Location = new Point(297, 75),
            Size = new Size(75, 26)
        };
        this.Controls.Add(_cancelButton);
        this.CancelButton = _cancelButton;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _inputTextBox?.Focus();
        _inputTextBox?.SelectAll();
    }
}
