using static LangFix.NativeMethods;

namespace LangFix;

internal static class KeySender
{
    private static readonly int InputSize = System.Runtime.InteropServices.Marshal.SizeOf<INPUT>();

    /// <summary>Sends Ctrl+<paramref name="vk"/> to the foreground window.</summary>
    public static void SendCtrlCombo(ushort vk)
    {
        INPUT[] inputs =
        {
            Key(VK_CONTROL, down: true),
            Key(vk, down: true),
            Key(vk, down: false),
            Key(VK_CONTROL, down: false),
        };

        SendInput((uint)inputs.Length, inputs, InputSize);
    }

    /// <summary>
    /// Lifts any modifier the user is still physically holding, so the injected Ctrl+C / Ctrl+V
    /// is not corrupted by e.g. a held Shift from the hotkey chord.
    /// </summary>
    public static void ReleaseHeldModifiers()
    {
        ushort[] modifiers = { VK_SHIFT, VK_CONTROL, VK_MENU, VK_LWIN, VK_RWIN };
        var up = new List<INPUT>(modifiers.Length);

        foreach (ushort vk in modifiers)
        {
            if ((GetAsyncKeyState(vk) & 0x8000) != 0)
            {
                up.Add(Key(vk, down: false));
            }
        }

        if (up.Count > 0)
        {
            SendInput((uint)up.Count, up.ToArray(), InputSize);
        }
    }

    private static INPUT Key(ushort vk, bool down) => new()
    {
        type = INPUT_KEYBOARD,
        U = new InputUnion
        {
            ki = new KEYBDINPUT
            {
                wVk = vk,
                dwFlags = down ? 0 : KEYEVENTF_KEYUP,
            },
        },
    };
}
