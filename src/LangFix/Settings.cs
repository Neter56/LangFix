using System.Text.Json;
using System.Text.Json.Serialization;

namespace LangFix;

public sealed class Settings
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    /// <summary>Hotkey chord, e.g. "F10", "Ctrl+Shift+X", "Win+H".</summary>
    public string HotKey { get; set; } = "F10";

    /// <summary>Hotkey that flips the case of English text typed with Caps Lock on.</summary>
    public string CapsHotKey { get; set; } = "Shift+F10";

    /// <summary>Paste the converted text back over the selection. When false, it is only put on the clipboard.</summary>
    public bool AutoPaste { get; set; } = true;

    /// <summary>Restore whatever text was on the clipboard before the conversion.</summary>
    public bool RestoreClipboard { get; set; } = true;

    /// <summary>Show a tray balloon after each conversion.</summary>
    public bool ShowNotifications { get; set; } = false;

    public static string FilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "LangFix",
        "settings.json");

    public static Settings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                return JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath)) ?? new Settings();
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // Fall through to defaults; a broken file should not stop the app from starting.
        }

        var settings = new Settings();
        settings.Save();
        return settings;
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Persisting settings is best-effort.
        }
    }

    public HotKeyDefinition GetHotKey() =>
        HotKeyDefinition.TryParse(HotKey, out HotKeyDefinition parsed) ? parsed : HotKeyDefinition.Default;

    public HotKeyDefinition GetCapsHotKey() =>
        HotKeyDefinition.TryParse(CapsHotKey, out HotKeyDefinition parsed) ? parsed : HotKeyDefinition.CapsDefault;
}
