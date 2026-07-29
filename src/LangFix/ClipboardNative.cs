using System.Runtime.InteropServices;
using static LangFix.NativeMethods;

namespace LangFix;

/// <summary>
/// Direct Win32 clipboard access.
/// <para>
/// <see cref="System.Windows.Forms.Clipboard"/> is deliberately avoided: it publishes an OLE data
/// object owned by the calling STA thread, so every later <c>EmptyClipboard</c> from another
/// process has to marshal back into that thread. While we are busy waiting for a copy to land,
/// that thread is not pumping messages and the other application's Ctrl+C stalls - which made the
/// utility work exactly once per launch.
/// </para>
/// </summary>
internal static class ClipboardNative
{
    private const int OpenAttempts = 12;
    private const int OpenRetryDelayMs = 20;

    public static string? GetText()
    {
        if (!IsClipboardFormatAvailable(CF_UNICODETEXT) || !TryOpen(IntPtr.Zero))
        {
            return null;
        }

        try
        {
            IntPtr handle = GetClipboardData(CF_UNICODETEXT);
            if (handle == IntPtr.Zero)
            {
                return null;
            }

            IntPtr pointer = GlobalLock(handle);
            if (pointer == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                return Marshal.PtrToStringUni(pointer);
            }
            finally
            {
                GlobalUnlock(handle);
            }
        }
        finally
        {
            CloseClipboard();
        }
    }

    /// <param name="owner">
    /// Window that becomes the clipboard owner. Must be a real handle - opening the clipboard with
    /// a null owner makes <c>SetClipboardData</c> fail.
    /// </param>
    public static bool SetText(string text, IntPtr owner)
    {
        if (!TryOpen(owner))
        {
            return false;
        }

        try
        {
            if (!EmptyClipboard())
            {
                return false;
            }

            if (text.Length == 0)
            {
                return true;
            }

            IntPtr memory = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)(uint)((text.Length + 1) * sizeof(char)));
            if (memory == IntPtr.Zero)
            {
                return false;
            }

            IntPtr pointer = GlobalLock(memory);
            if (pointer == IntPtr.Zero)
            {
                GlobalFree(memory);
                return false;
            }

            try
            {
                Marshal.Copy(text.ToCharArray(), 0, pointer, text.Length);
                Marshal.WriteInt16(pointer, text.Length * sizeof(char), 0);
            }
            finally
            {
                GlobalUnlock(memory);
            }

            if (SetClipboardData(CF_UNICODETEXT, memory) == IntPtr.Zero)
            {
                GlobalFree(memory);
                return false;
            }

            // Ownership of the block has moved to the clipboard - do not free it.
            return true;
        }
        finally
        {
            CloseClipboard();
        }
    }

    private static bool TryOpen(IntPtr owner)
    {
        for (int attempt = 0; attempt < OpenAttempts; attempt++)
        {
            if (OpenClipboard(owner))
            {
                return true;
            }

            Thread.Sleep(OpenRetryDelayMs);
        }

        return false;
    }
}
