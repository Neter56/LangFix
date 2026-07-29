using System.ComponentModel;
using System.Windows.Forms;

namespace LangFix;

/// <summary>Message-only window that owns the global hotkey registration.</summary>
internal sealed class HotKeyWindow : NativeWindow, IDisposable
{
    private const int HotKeyId = 0xB0B0;

    private bool _registered;

    public HotKeyWindow()
    {
        CreateHandle(new CreateParams
        {
            Caption = "LangFixHotKeyWindow",
            // HWND_MESSAGE - invisible, message-only window.
            Parent = new IntPtr(-3),
        });
    }

    public event EventHandler? HotKeyPressed;

    public void Register(HotKeyDefinition hotKey)
    {
        Unregister();

        uint modifiers = hotKey.Modifiers | NativeMethods.MOD_NOREPEAT;
        if (!NativeMethods.RegisterHotKey(Handle, HotKeyId, modifiers, (uint)hotKey.Key))
        {
            throw new Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error(),
                $"Could not register the hotkey '{hotKey}'. It is probably taken by another application.");
        }

        _registered = true;
    }

    public void Unregister()
    {
        if (_registered)
        {
            NativeMethods.UnregisterHotKey(Handle, HotKeyId);
            _registered = false;
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_HOTKEY && m.WParam.ToInt32() == HotKeyId)
        {
            HotKeyPressed?.Invoke(this, EventArgs.Empty);
            return;
        }

        base.WndProc(ref m);
    }

    public void Dispose()
    {
        Unregister();
        DestroyHandle();
    }
}
