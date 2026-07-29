using System.ComponentModel;
using System.Windows.Forms;

namespace LangFix;

/// <summary>What a hotkey does. The value doubles as the Win32 hotkey id.</summary>
internal enum HotKeyAction
{
    ConvertLayout = 0xB0B0,
    FixCase = 0xB0B1,
}

/// <summary>Message-only window that owns the global hotkey registrations.</summary>
internal sealed class HotKeyWindow : NativeWindow, IDisposable
{
    private readonly HashSet<HotKeyAction> _registered = new();

    public HotKeyWindow()
    {
        CreateHandle(new CreateParams
        {
            Caption = "LangFixHotKeyWindow",
            // HWND_MESSAGE - invisible, message-only window.
            Parent = new IntPtr(-3),
        });
    }

    public event EventHandler<HotKeyAction>? HotKeyPressed;

    public void Register(HotKeyAction action, HotKeyDefinition hotKey)
    {
        Unregister(action);

        uint modifiers = hotKey.Modifiers | NativeMethods.MOD_NOREPEAT;
        if (!NativeMethods.RegisterHotKey(Handle, (int)action, modifiers, (uint)hotKey.Key))
        {
            throw new Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error(),
                $"Could not register the hotkey '{hotKey}'. It is probably taken by another application.");
        }

        _registered.Add(action);
    }

    public void Unregister(HotKeyAction action)
    {
        if (_registered.Remove(action))
        {
            NativeMethods.UnregisterHotKey(Handle, (int)action);
        }
    }

    public void UnregisterAll()
    {
        foreach (HotKeyAction action in _registered.ToArray())
        {
            Unregister(action);
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_HOTKEY)
        {
            var action = (HotKeyAction)m.WParam.ToInt32();
            if (_registered.Contains(action))
            {
                HotKeyPressed?.Invoke(this, action);
                return;
            }
        }

        base.WndProc(ref m);
    }

    public void Dispose()
    {
        UnregisterAll();
        DestroyHandle();
    }
}
