# LangFix

A small, native Windows 11 tray utility that fixes text typed with the wrong keyboard layout —
a modern replacement for the unmaintained [LangOver](https://langover.com).

Select the mistyped text, press **F10**, and it is replaced in place:

```
Eמעךןדי בישרשבאקרד   ->   English characters
akuo                 ->   שלום
```

Caught Caps Lock too late? Select the text and press **Shift+F10**:

```
tHIS SENTENCE WRITTEN WITH CAPS LOCK ON   ->   This sentence written with caps lock on
```

## Install

Download **`LangFix-1.1.0-x64.msi`** from the [latest release](https://github.com/Neter56/LangFix/releases/latest)
and run it. Windows will ask for administrator approval because the app is installed for every
user on the machine.

The installer:

- puts `LangFix.exe` in `C:\Program Files\LangFix`,
- adds a **Start menu** entry,
- registers LangFix to **start automatically when you sign in**,
- offers to start it straight away on the last page.

Nothing else is needed — the runtime is bundled, so there is no .NET prerequisite.

Once it is running, look for the LangFix icon in the notification area. Auto-start can be turned
off at any time from the tray menu (**Start with Windows**) or in **Settings…**.

To remove it, use **Settings → Apps → Installed apps → LangFix → Uninstall**. Your preferences in
`%APPDATA%\LangFix` are left in place.

Silent install, for deploying it across several machines:

```powershell
msiexec /i LangFix-1.1.0-x64.msi /qn
```

## Hotkeys

| Hotkey | Action |
|---|---|
| `F10` | Convert the selection between the English and Hebrew keyboard layouts |
| `Shift+F10` | Flip the case of the selection (undo a stuck Caps Lock) — English text only |

Both chords can be changed in the settings window.

## How it works

1. The app registers two system-wide hotkeys (default `F10` and `Shift+F10`) via `RegisterHotKey`.
2. On press it injects `Ctrl+C`, waits for the clipboard sequence number to change, and reads the selection.
3. `F10` re-maps each character between the **US English** and the **Windows Hebrew (SI-1452)** layouts.
   The direction is auto-detected by counting Hebrew vs. Latin letters, so mixed text like
   `Eמעךןדי` keeps the characters that were already correct.
   `Shift+F10` instead swaps upper case and lower case, and refuses to touch a selection that
   contains letters outside the Latin alphabet.
4. The converted text is put on the clipboard, `Ctrl+V` is injected, and the previous clipboard
   contents are restored.

## Build from source

Only needed to develop LangFix — to just use it, install the MSI above.

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download).

```powershell
cd src\LangFix
dotnet build -c Release
```

Run it without installing:

```powershell
dotnet publish src\LangFix\LangFix.csproj -c Release -o publish
.\publish\LangFix.exe
```

Rebuild the installer itself into `artifacts\` (needs the WiX 5 CLI,
`dotnet tool install --global wix`):

```powershell
pwsh -File tools\build-installer.ps1
```

## Usage

- The app lives in the notification area. Right-click the icon for the menu.
- **Convert selection now** — same as the `F10` hotkey (useful for testing).
- **Fix CAPS LOCK in selection** — same as the `Shift+F10` hotkey.
- **Paste result automatically** — off means the converted text is only placed on the clipboard.
- **Start with Windows** — writes `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.
- **Settings…** — opens a small window that reads and writes `%APPDATA%\LangFix\settings.json`.
  Click either hotkey box and press the chord you want; **Save** applies it immediately.
- **Reload settings** — re-reads the file after editing it in an external editor.

### settings.json

```json
{
  "HotKey": "F10",
  "CapsHotKey": "Shift+F10",
  "AutoPaste": true,
  "RestoreClipboard": true,
  "ShowNotifications": false
}
```

`HotKey` and `CapsHotKey` accept a chord built from `Ctrl`, `Alt`, `Shift`, `Win` plus a key name
from the .NET `Keys` enum — for example `F10`, `Ctrl+Shift+X`, `Win+H`, `Pause`. The two must
differ.

## Release notes

See [RELEASE-NOTES.md](RELEASE-NOTES.md).

## Notes and limitations

- Runs as `asInvoker`. Injected keystrokes cannot reach windows of **elevated** processes; run
  LangFix elevated too if you need it inside an admin console.
- Only text is backed up and restored on the clipboard; other formats (images, files) are lost
  during a conversion. Set `"RestoreClipboard": false` if you prefer the result to stay.
- `F10` is the menu-activation key in classic Win32 apps. Because the hotkey is registered
  globally, LangFix swallows it everywhere — pick a different chord in `settings.json` if that
  bothers you.
- Punctuation that differs between the layouts (`,` `.` `/` `'` `;`) is converted too, because it
  was also produced by the wrong layout.

## Verifying the mapping

```powershell
& "$env:ProgramFiles\LangFix\LangFix.exe" --selftest
& "$env:ProgramFiles\LangFix\LangFix.exe" --selftest "Eמעךןדי בישרשבאקרד"
```

With text supplied, the layout conversion and the Caps Lock fix are both printed.

## Layout table

| Key | Hebrew | Key | Hebrew | Key | Hebrew |
|---|---|---|---|---|---|
| q | / | a | ש | z | ז |
| w | ' | s | ד | x | ס |
| e | ק | d | ג | c | ב |
| r | ר | f | כ | v | ה |
| t | א | g | ע | b | נ |
| y | ט | h | י | n | מ |
| u | ו | j | ח | m | צ |
| i | ן | k | ל | , | ת |
| o | ם | l | ך | . | ץ |
| p | פ | ; | ף | / | . |
|   |   | ' | , |   |   |

## License

[MIT](LICENSE)

