namespace LangFix;

/// <summary>
/// Translates text between the US-English and the Windows Hebrew (SI-1452) keyboard layouts,
/// i.e. re-interprets the physical keys that produced the text.
/// </summary>
public static class LayoutConverter
{
    // Physical key (US layout character) -> character produced by the Windows Hebrew layout.
    // Hebrew letters are written as escapes so the source file stays readable (no bidi reordering).
    private static readonly (char Latin, char Hebrew)[] KeyPairs =
    {
        ('q', '/'),
        ('w', '\''),
        ('e', '\u05E7'), // qof
        ('r', '\u05E8'), // resh
        ('t', '\u05D0'), // alef
        ('y', '\u05D8'), // tet
        ('u', '\u05D5'), // vav
        ('i', '\u05DF'), // final nun
        ('o', '\u05DD'), // final mem
        ('p', '\u05E4'), // pe
        ('a', '\u05E9'), // shin
        ('s', '\u05D3'), // dalet
        ('d', '\u05D2'), // gimel
        ('f', '\u05DB'), // kaf
        ('g', '\u05E2'), // ayin
        ('h', '\u05D9'), // yod
        ('j', '\u05D7'), // het
        ('k', '\u05DC'), // lamed
        ('l', '\u05DA'), // final kaf
        (';', '\u05E3'), // final pe
        ('\'', ','),
        ('z', '\u05D6'), // zayin
        ('x', '\u05E1'), // samekh
        ('c', '\u05D1'), // bet
        ('v', '\u05D4'), // he
        ('b', '\u05E0'), // nun
        ('n', '\u05DE'), // mem
        ('m', '\u05E6'), // tsadi
        (',', '\u05EA'), // tav
        ('.', '\u05E5'), // final tsadi
        ('/', '.'),
    };

    private static readonly Dictionary<char, char> LatinToHebrew = BuildLatinToHebrew();
    private static readonly Dictionary<char, char> HebrewToLatin = BuildHebrewToLatin();

    public enum Direction
    {
        Auto,
        LatinToHebrew,
        HebrewToLatin,
    }

    /// <summary>Converts <paramref name="text"/>, auto-detecting which way to go.</summary>
    public static string Convert(string text) => Convert(text, Direction.Auto);

    public static string Convert(string text, Direction direction)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        if (direction == Direction.Auto)
        {
            direction = Detect(text);
        }

        Dictionary<char, char> map = direction == Direction.HebrewToLatin ? HebrewToLatin : LatinToHebrew;

        char[] result = text.ToCharArray();
        for (int i = 0; i < result.Length; i++)
        {
            if (map.TryGetValue(result[i], out char mapped))
            {
                result[i] = mapped;
            }
        }

        return new string(result);
    }

    /// <summary>
    /// Picks a direction by comparing how many Hebrew letters vs. Latin letters the text holds.
    /// Ties go to Hebrew-to-Latin because Hebrew letters can only come from a Hebrew layout.
    /// </summary>
    public static Direction Detect(string text)
    {
        int hebrew = 0;
        int latin = 0;

        foreach (char c in text)
        {
            if (c is >= '\u05D0' and <= '\u05EA')
            {
                hebrew++;
            }
            else if (char.IsAsciiLetter(c))
            {
                latin++;
            }
        }

        return hebrew > 0 && hebrew >= latin ? Direction.HebrewToLatin : Direction.LatinToHebrew;
    }

    private static Dictionary<char, char> BuildLatinToHebrew()
    {
        var map = new Dictionary<char, char>(KeyPairs.Length * 2);
        foreach ((char latin, char hebrew) in KeyPairs)
        {
            map[latin] = hebrew;
            char upper = char.ToUpperInvariant(latin);
            if (upper != latin)
            {
                // Hebrew is caseless: Shift+key yields the same letter.
                map[upper] = hebrew;
            }
        }

        return map;
    }

    private static Dictionary<char, char> BuildHebrewToLatin()
    {
        var map = new Dictionary<char, char>(KeyPairs.Length);
        foreach ((char latin, char hebrew) in KeyPairs)
        {
            map[hebrew] = latin;
        }

        return map;
    }
}
