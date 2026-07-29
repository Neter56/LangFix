using System.Text;
using System.Windows.Forms;

namespace LangFix;

/// <summary>A parsed hotkey chord such as "F10", "Ctrl+Shift+X" or "Win+Alt+H".</summary>
public sealed record HotKeyDefinition(uint Modifiers, Keys Key)
{
    public static readonly HotKeyDefinition Default = new(0, Keys.F10);
    public static readonly HotKeyDefinition CapsDefault = new(NativeMethods.MOD_SHIFT, Keys.F10);

    public static bool TryParse(string? text, out HotKeyDefinition hotKey)
    {
        hotKey = Default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        uint modifiers = 0;
        Keys key = Keys.None;

        foreach (string rawPart in text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            switch (rawPart.ToLowerInvariant())
            {
                case "ctrl":
                case "control":
                    modifiers |= NativeMethods.MOD_CONTROL;
                    break;
                case "alt":
                    modifiers |= NativeMethods.MOD_ALT;
                    break;
                case "shift":
                    modifiers |= NativeMethods.MOD_SHIFT;
                    break;
                case "win":
                case "windows":
                    modifiers |= NativeMethods.MOD_WIN;
                    break;
                default:
                    if (key != Keys.None || !Enum.TryParse(rawPart, ignoreCase: true, out Keys parsed) || parsed == Keys.None)
                    {
                        return false;
                    }

                    key = parsed;
                    break;
            }
        }

        if (key == Keys.None)
        {
            return false;
        }

        hotKey = new HotKeyDefinition(modifiers, key);
        return true;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        if ((Modifiers & NativeMethods.MOD_CONTROL) != 0) sb.Append("Ctrl+");
        if ((Modifiers & NativeMethods.MOD_ALT) != 0) sb.Append("Alt+");
        if ((Modifiers & NativeMethods.MOD_SHIFT) != 0) sb.Append("Shift+");
        if ((Modifiers & NativeMethods.MOD_WIN) != 0) sb.Append("Win+");
        sb.Append(Key);
        return sb.ToString();
    }
}
