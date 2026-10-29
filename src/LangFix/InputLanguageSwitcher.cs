namespace LangFix;

/// <summary>Switches the Windows input language of the foreground window to English or Hebrew.</summary>
internal static class InputLanguageSwitcher
{
    private const ushort PrimaryEnglish = 0x09;
    private const ushort PrimaryHebrew = 0x0D;
    private const ushort UsEnglish = 0x0409;

    /// <summary>
    /// Switches to the layout the converted text belongs to. Does nothing when no matching
    /// layout is installed or there is no foreground window.
    /// </summary>
    public static void SwitchTo(LayoutConverter.Direction convertedDirection)
    {
        ushort primary = convertedDirection == LayoutConverter.Direction.HebrewToLatin
            ? PrimaryEnglish
            : PrimaryHebrew;

        IntPtr target = FindLayout(primary);
        IntPtr window = NativeMethods.GetForegroundWindow();
        if (target == IntPtr.Zero || window == IntPtr.Zero)
        {
            return;
        }

        NativeMethods.PostMessage(window, NativeMethods.WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, target);
    }

    private static IntPtr FindLayout(ushort primaryLanguage)
    {
        int count = NativeMethods.GetKeyboardLayoutList(0, null);
        if (count <= 0)
        {
            return IntPtr.Zero;
        }

        var layouts = new IntPtr[count];
        count = NativeMethods.GetKeyboardLayoutList(count, layouts);

        IntPtr fallback = IntPtr.Zero;
        for (int i = 0; i < count; i++)
        {
            ushort langId = (ushort)((long)layouts[i] & 0xFFFF);
            if ((langId & 0x3FF) != primaryLanguage)
            {
                continue;
            }

            if (langId == UsEnglish || primaryLanguage == PrimaryHebrew)
            {
                return layouts[i];
            }

            if (fallback == IntPtr.Zero)
            {
                fallback = layouts[i];
            }
        }

        return fallback;
    }
}
