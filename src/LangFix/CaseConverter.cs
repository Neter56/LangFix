using System.Diagnostics.CodeAnalysis;

namespace LangFix;

/// <summary>
/// Undoes text typed with Caps Lock stuck on: every letter's case is flipped, so
/// "tHIS SENTENCE" becomes "This sentence". Applies to English text only.
/// </summary>
public static class CaseConverter
{
    /// <summary>Flips the case of every letter.</summary>
    public static string SwapCase(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        char[] result = text.ToCharArray();
        for (int i = 0; i < result.Length; i++)
        {
            char c = result[i];
            if (char.IsUpper(c))
            {
                result[i] = char.ToLowerInvariant(c);
            }
            else if (char.IsLower(c))
            {
                result[i] = char.ToUpperInvariant(c);
            }
        }

        return new string(result);
    }

    /// <summary>
    /// True when the text holds at least one Latin letter and no letters from another script,
    /// which is what makes a case flip meaningful.
    /// </summary>
    public static bool IsEnglish(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        bool hasLatin = false;
        foreach (char c in text)
        {
            if (char.IsAsciiLetter(c))
            {
                hasLatin = true;
            }
            else if (char.IsLetter(c))
            {
                return false;
            }
        }

        return hasLatin;
    }

    /// <summary>Returns false - and leaves the text alone - when it is not English.</summary>
    public static bool TryFix(string? text, [NotNullWhen(true)] out string? fixedText)
    {
        if (!IsEnglish(text))
        {
            fixedText = null;
            return false;
        }

        fixedText = SwapCase(text!);
        return true;
    }
}
