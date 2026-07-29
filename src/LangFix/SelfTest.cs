using System.Text;

namespace LangFix;

/// <summary>
/// Headless verification of <see cref="LayoutConverter"/>: <c>LangFix.exe --selftest [text...]</c>.
/// Output is written to the console the process was launched from.
/// </summary>
internal static class SelfTest
{
    private static readonly (string Input, string Expected)[] Cases =
    {
        ("E\u05DE\u05E2\u05DA\u05DF\u05D3\u05D9 \u05D1\u05D9\u05E9\u05E8\u05E9\u05D1\u05D0\u05E7\u05E8\u05D3", "English characters"),
        ("\u05E9\u05DC\u05D5\u05DD", "akuo"),
        ("hello", "\u05D9\u05E7\u05DA\u05DA\u05DD"),
        ("", ""),
    };

    // The Caps Lock fixer: null means "left alone because the text is not English".
    private static readonly (string Input, string? Expected)[] CaseCases =
    {
        ("tHIS SENTENCE WRITTEN WITH CAPS LOCK ON", "This sentence written with caps lock on"),
        ("hELLO wORLD 123!", "Hello World 123!"),
        ("MiXeD", "mIxEd"),
        ("\u05E9\u05DC\u05D5\u05DD", null),
        ("hELLO \u05E9\u05DC\u05D5\u05DD", null),
        ("12345", null),
        ("", null),
    };

    public static int Run(string[] args)
    {
        NativeMethods.AttachConsole(NativeMethods.ATTACH_PARENT_PROCESS);
        Console.OutputEncoding = Encoding.UTF8;

        if (args.Length > 0)
        {
            string input = string.Join(' ', args);
            Console.WriteLine(LayoutConverter.Convert(input));
            Console.WriteLine(CaseConverter.TryFix(input, out string? cased) ? cased : "(not English - case left alone)");
            return 0;
        }

        int failures = 0;
        foreach ((string input, string expected) in Cases)
        {
            string actual = LayoutConverter.Convert(input);
            bool ok = actual == expected;
            if (!ok)
            {
                failures++;
            }

            Console.WriteLine($"{(ok ? "PASS" : "FAIL")} | layout | in='{input}' | out='{actual}' | expected='{expected}'");
        }

        foreach ((string input, string? expected) in CaseCases)
        {
            string? actual = CaseConverter.TryFix(input, out string? fixedText) ? fixedText : null;
            bool ok = actual == expected;
            if (!ok)
            {
                failures++;
            }

            Console.WriteLine($"{(ok ? "PASS" : "FAIL")} | caps | in='{input}' | out='{actual ?? "<unchanged>"}' | expected='{expected ?? "<unchanged>"}'");
        }

        Console.WriteLine(failures == 0 ? "All self-tests passed." : $"{failures} self-test(s) failed.");
        return failures == 0 ? 0 : 1;
    }
}
