using System.Diagnostics;
using System.Windows.Forms;

namespace LangFix;

/// <summary>Small editor for settings.json. Saving rewrites the file; the caller re-applies the values.</summary>
internal sealed class SettingsForm : Form
{
    private readonly Settings _settings;
    private readonly TextBox _hotKeyBox;
    private readonly CheckBox _autoPaste;
    private readonly CheckBox _restoreClipboard;
    private readonly CheckBox _showNotifications;
    private readonly CheckBox _startWithWindows;
    private HotKeyDefinition _hotKey;

    public SettingsForm(Settings settings)
    {
        _settings = settings;
        _hotKey = settings.GetHotKey();

        Text = "LangFix settings";
        Icon = TrayIconFactory.Create();
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(380, 262);
        Padding = new Padding(12);

        var hotKeyLabel = new Label
        {
            Text = "Hotkey",
            Location = new Point(12, 16),
            AutoSize = true,
        };

        _hotKeyBox = new TextBox
        {
            Location = new Point(80, 12),
            Width = 200,
            ReadOnly = true,
            Text = _hotKey.ToString(),
            TextAlign = HorizontalAlignment.Center,
            BackColor = SystemColors.Window,
        };

        var resetHotKey = new Button
        {
            Text = "Reset",
            Location = new Point(288, 11),
            Width = 78,
        };
        resetHotKey.Click += (_, _) => SetHotKey(HotKeyDefinition.Default);

        var hint = new Label
        {
            Text = "Click the box above and press the key combination you want.",
            Location = new Point(80, 40),
            Width = 286,
            ForeColor = SystemColors.GrayText,
        };

        _autoPaste = NewCheck("Paste the result over the selection", 70, _settings.AutoPaste);
        _restoreClipboard = NewCheck("Restore the previous clipboard contents", 96, _settings.RestoreClipboard);
        _showNotifications = NewCheck("Show a tray notification after each conversion", 122, _settings.ShowNotifications);
        _startWithWindows = NewCheck("Start with Windows", 148, StartupRegistration.IsEnabled());

        var openJson = new LinkLabel
        {
            Text = "Open settings.json",
            Location = new Point(12, 182),
            AutoSize = true,
        };
        openJson.LinkClicked += (_, _) => OpenSettingsFile();

        // ProductVersion can carry a "+<commit>" suffix; show the plain number.
        string productVersion = Application.ProductVersion.Split('+')[0];
        var version = new Label
        {
            Text = $"LangFix {productVersion}",
            Location = new Point(12, 210),
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
        };

        var save = new Button
        {
            Text = "Save",
            Location = new Point(198, 205),
            Width = 80,
        };
        save.Click += (_, _) =>
        {
            Apply();
            Close();
        };

        var cancel = new Button
        {
            Text = "Cancel",
            Location = new Point(286, 205),
            Width = 80,
            DialogResult = DialogResult.Cancel,
        };
        cancel.Click += (_, _) => Close();

        Controls.AddRange([
            hotKeyLabel, _hotKeyBox, resetHotKey, hint,
            _autoPaste, _restoreClipboard, _showNotifications, _startWithWindows,
            openJson, version, save, cancel,
        ]);

        AcceptButton = save;
        CancelButton = cancel;
    }

    private CheckBox NewCheck(string text, int top, bool value) => new()
    {
        Text = text,
        Location = new Point(12, top),
        Width = 354,
        Checked = value,
    };

    private void SetHotKey(HotKeyDefinition hotKey)
    {
        _hotKey = hotKey;
        _hotKeyBox.Text = hotKey.ToString();
    }

    // ProcessCmdKey sees keys the TextBox never gets, such as F10, Tab and Alt combinations.
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        bool keyDown = msg.Msg is NativeMethods.WM_KEYDOWN or NativeMethods.WM_SYSKEYDOWN;
        if (keyDown && _hotKeyBox.Focused && TryCapture(keyData))
        {
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private bool TryCapture(Keys keyData)
    {
        Keys key = keyData & Keys.KeyCode;
        if (key is Keys.None or Keys.ControlKey or Keys.ShiftKey or Keys.Menu
            or Keys.LWin or Keys.RWin or Keys.Escape)
        {
            return false;
        }

        uint modifiers = 0;
        if ((keyData & Keys.Control) != 0) modifiers |= NativeMethods.MOD_CONTROL;
        if ((keyData & Keys.Alt) != 0) modifiers |= NativeMethods.MOD_ALT;
        if ((keyData & Keys.Shift) != 0) modifiers |= NativeMethods.MOD_SHIFT;
        if (IsDown(NativeMethods.VK_LWIN) || IsDown(NativeMethods.VK_RWIN)) modifiers |= NativeMethods.MOD_WIN;

        SetHotKey(new HotKeyDefinition(modifiers, key));
        return true;
    }

    private static bool IsDown(ushort virtualKey) =>
        (NativeMethods.GetAsyncKeyState(virtualKey) & 0x8000) != 0;

    private void Apply()
    {
        _settings.HotKey = _hotKey.ToString();
        _settings.AutoPaste = _autoPaste.Checked;
        _settings.RestoreClipboard = _restoreClipboard.Checked;
        _settings.ShowNotifications = _showNotifications.Checked;
        _settings.Save();

        if (_startWithWindows.Checked != StartupRegistration.IsEnabled())
        {
            try
            {
                StartupRegistration.SetEnabled(_startWithWindows.Checked);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                MessageBox.Show(this, $"Could not update the startup entry.\n\n{ex.Message}", "LangFix",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    private void OpenSettingsFile()
    {
        _settings.Save();
        try
        {
            Process.Start(new ProcessStartInfo(Settings.FilePath) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or IOException)
        {
            MessageBox.Show(this, $"Could not open {Settings.FilePath}\n\n{ex.Message}", "LangFix",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
