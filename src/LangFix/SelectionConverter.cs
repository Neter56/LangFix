namespace LangFix;

internal enum ConversionOutcome
{
    Converted,
    NoSelection,
    ClipboardBusy,
    NotEnglish,
}

internal sealed record ConversionResult(ConversionOutcome Outcome, string? Original = null, string? Converted = null);

/// <summary>
/// Copies the current selection out of the foreground window, flips its keyboard layout and
/// pastes it back. Must run off the UI thread so our message pump stays alive while we wait.
/// </summary>
internal static class SelectionConverter
{
    private static readonly TimeSpan CopyTimeout = TimeSpan.FromMilliseconds(1200);
    private const int PollIntervalMs = 25;
    private const int PasteSettleMs = 250;

    public static ConversionResult Run(Settings settings, IntPtr clipboardOwner, HotKeyAction action)
    {
        string? backup = ClipboardNative.GetText();

        try
        {
            KeySender.ReleaseHeldModifiers();
            Thread.Sleep(40);

            uint sequenceBefore = NativeMethods.GetClipboardSequenceNumber();
            KeySender.SendCtrlCombo(NativeMethods.VK_C);

            string? selection = WaitForClipboardText(sequenceBefore, backup);
            if (string.IsNullOrEmpty(selection))
            {
                return new ConversionResult(ConversionOutcome.NoSelection);
            }

            string converted;
            if (action == HotKeyAction.FixCase)
            {
                if (!CaseConverter.TryFix(selection, out string? cased))
                {
                    return new ConversionResult(ConversionOutcome.NotEnglish, selection);
                }

                converted = cased;
            }
            else
            {
                converted = LayoutConverter.Convert(selection);
            }

            if (!ClipboardNative.SetText(converted, clipboardOwner))
            {
                return new ConversionResult(ConversionOutcome.ClipboardBusy);
            }

            if (settings.AutoPaste)
            {
                Thread.Sleep(40);
                KeySender.SendCtrlCombo(NativeMethods.VK_V);
                Thread.Sleep(PasteSettleMs);
            }
            else
            {
                // Leave the result on the clipboard for the user to paste.
                backup = null;
            }

            return new ConversionResult(ConversionOutcome.Converted, selection, converted);
        }
        finally
        {
            if (settings.RestoreClipboard && backup is not null)
            {
                ClipboardNative.SetText(backup, clipboardOwner);
            }
        }
    }

    private static string? WaitForClipboardText(uint sequenceBefore, string? backup)
    {
        DateTime deadline = DateTime.UtcNow + CopyTimeout;

        while (DateTime.UtcNow < deadline)
        {
            Thread.Sleep(PollIntervalMs);

            if (NativeMethods.GetClipboardSequenceNumber() == sequenceBefore)
            {
                continue;
            }

            string? text = ClipboardNative.GetText();
            if (!string.IsNullOrEmpty(text))
            {
                return text;
            }
        }

        // Fallback for apps whose copy lands without a sequence bump we observed in time.
        string? final = ClipboardNative.GetText();
        return !string.IsNullOrEmpty(final) && final != backup ? final : null;
    }
}
