using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace LangFix;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly HotKeyWindow _hotKeyWindow;
    private readonly ToolStripMenuItem _startupItem;
    private readonly ToolStripMenuItem _autoPasteItem;
    private readonly Control _marshal;
    private SettingsForm? _settingsForm;
    private Settings _settings;
    private int _busy;

    public TrayApplicationContext()
    {
        _settings = Settings.Load();

        // Handle is forced here, on the UI thread, so the worker can BeginInvoke back onto it.
        _marshal = new Control();
        _ = _marshal.Handle;

        _startupItem = new ToolStripMenuItem("Start with Windows", null, OnToggleStartup)
        {
            Checked = StartupRegistration.IsEnabled(),
            CheckOnClick = false,
        };

        _autoPasteItem = new ToolStripMenuItem("Paste result automatically", null, OnToggleAutoPaste)
        {
            Checked = _settings.AutoPaste,
            CheckOnClick = false,
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add(new ToolStripMenuItem("Convert selection now", null, (_, _) => ConvertSelection(HotKeyAction.ConvertLayout)));
        menu.Items.Add(new ToolStripMenuItem("Fix CAPS LOCK in selection", null, (_, _) => ConvertSelection(HotKeyAction.FixCase)));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_autoPasteItem);
        menu.Items.Add(_startupItem);
        menu.Items.Add(new ToolStripMenuItem("Settings\u2026", null, (_, _) => OpenSettings()));
        menu.Items.Add(new ToolStripMenuItem("Reload settings", null, (_, _) => ReloadSettings()));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Exit", null, (_, _) => ExitThread()));

        _notifyIcon = new NotifyIcon
        {
            Icon = TrayIconFactory.Create(),
            ContextMenuStrip = menu,
            Visible = true,
        };
        _notifyIcon.DoubleClick += (_, _) => ConvertSelection(HotKeyAction.ConvertLayout);

        _hotKeyWindow = new HotKeyWindow();
        _hotKeyWindow.HotKeyPressed += (_, action) => ConvertSelection(action);

        RegisterHotKeys(showErrors: true);
        UpdateTooltip();
    }

    private void RegisterHotKeys(bool showErrors)
    {
        Register(HotKeyAction.ConvertLayout, _settings.GetHotKey(), showErrors);
        Register(HotKeyAction.FixCase, _settings.GetCapsHotKey(), showErrors);
    }

    private void Register(HotKeyAction action, HotKeyDefinition hotKey, bool showErrors)
    {
        try
        {
            _hotKeyWindow.Register(action, hotKey);
        }
        catch (Win32Exception ex) when (showErrors)
        {
            MessageBox.Show(
                $"{ex.Message}\n\nChange the hotkey in the Settings window, or in:\n{Settings.FilePath}\n\nLangFix keeps running - use the tray menu to convert.",
                "LangFix",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        catch (Win32Exception)
        {
            // Silent when reloading; tooltip below reflects the state.
        }
    }

    private void ConvertSelection(HotKeyAction action)
    {
        // The clipboard round-trip blocks for up to a second. Doing that inside the WM_HOTKEY
        // handler stops our message pump, which in turn stalls the Ctrl+C of the foreground app
        // once we own the clipboard - so the work happens on a worker thread instead.
        if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
        {
            return;
        }

        Settings settings = _settings;
        IntPtr owner = _hotKeyWindow.Handle;

        var worker = new Thread(() =>
        {
            try
            {
                ConversionResult result = SelectionConverter.Run(settings, owner, action);
                _marshal.BeginInvoke(() => Report(result));
            }
            finally
            {
                Volatile.Write(ref _busy, 0);
            }
        })
        {
            IsBackground = true,
            Name = "LangFix.Convert",
        };

        worker.Start();
    }

    private void Report(ConversionResult result)
    {
        switch (result.Outcome)
        {
            case ConversionOutcome.Converted when _settings.ShowNotifications:
                Notify("Converted", Truncate(result.Converted!), ToolTipIcon.Info);
                break;
            case ConversionOutcome.NoSelection:
                Notify("Nothing to convert", "Select some text first, then press the hotkey.", ToolTipIcon.Warning);
                break;
            case ConversionOutcome.ClipboardBusy:
                Notify("Clipboard is busy", "Another application is holding the clipboard. Try again.", ToolTipIcon.Warning);
                break;
            case ConversionOutcome.NotEnglish:
                Notify("Left unchanged", "The Caps Lock fix only applies to English text.", ToolTipIcon.Warning);
                break;
        }
    }

    private void OnToggleStartup(object? sender, EventArgs e)
    {
        bool enable = !_startupItem.Checked;
        try
        {
            StartupRegistration.SetEnabled(enable);
            _startupItem.Checked = StartupRegistration.IsEnabled();
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            MessageBox.Show($"Could not update the startup entry.\n\n{ex.Message}", "LangFix",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void OnToggleAutoPaste(object? sender, EventArgs e)
    {
        _settings.AutoPaste = !_settings.AutoPaste;
        _autoPasteItem.Checked = _settings.AutoPaste;
        _settings.Save();
    }

    private void OpenSettings()
    {
        if (_settingsForm is { IsDisposed: false })
        {
            _settingsForm.Activate();
            return;
        }

        // Pick up edits made to the file by hand, and free the chords so they can be captured in the dialog.
        _settings = Settings.Load();
        _hotKeyWindow.UnregisterAll();

        _settingsForm = new SettingsForm(_settings);
        _settingsForm.FormClosed += (_, _) =>
        {
            _settingsForm = null;
            ApplySettings();
        };
        _settingsForm.Show();
    }

    private void ReloadSettings()
    {
        _settings = Settings.Load();
        ApplySettings();
    }

    private void ApplySettings()
    {
        _autoPasteItem.Checked = _settings.AutoPaste;
        _startupItem.Checked = StartupRegistration.IsEnabled();
        RegisterHotKeys(showErrors: true);
        UpdateTooltip();
    }

    private void UpdateTooltip()
    {
        // NotifyIcon.Text is capped at 63 characters.
        _notifyIcon.Text = Truncate($"LangFix - {_settings.GetHotKey()} layout, {_settings.GetCapsHotKey()} caps", 63);
    }

    private void Notify(string title, string message, ToolTipIcon icon)
    {
        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.BalloonTipIcon = icon;
        _notifyIcon.ShowBalloonTip(2000);
    }

    private static string Truncate(string text, int max = 120) =>
        text.Length <= max ? text : text[..(max - 1)] + "\u2026";

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _settingsForm?.Dispose();
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _hotKeyWindow.Dispose();
            _marshal.Dispose();
        }

        base.Dispose(disposing);
    }
}

internal static class TrayIconFactory
{
    /// <summary>Builds the tray icon at runtime so the app ships without binary assets.</summary>
    public static Icon Create()
    {
        using var bitmap = new Bitmap(32, 32);
        using (Graphics g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAlias;
            g.Clear(Color.Transparent);

            using var background = new SolidBrush(Color.FromArgb(0, 99, 177));
            g.FillEllipse(background, 0, 0, 31, 31);

            using var font = new Font("Segoe UI", 15f, FontStyle.Bold, GraphicsUnit.Pixel);
            using var hebrewFont = new Font("Segoe UI", 15f, FontStyle.Bold, GraphicsUnit.Pixel);
            g.DrawString("A", font, Brushes.White, new PointF(1f, 1f));
            g.DrawString("\u05D0", hebrewFont, Brushes.White, new PointF(14f, 12f));
        }

        IntPtr handle = bitmap.GetHicon();
        try
        {
            using var temp = Icon.FromHandle(handle);
            return (Icon)temp.Clone();
        }
        finally
        {
            NativeMethods.DestroyIcon(handle);
        }
    }
}
