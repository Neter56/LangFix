# Release notes

All notable changes to LangFix are listed here. Versions follow [semantic versioning](https://semver.org).

## 1.1.0

**Caps Lock fix**

- New second hotkey — `Shift+F10` by default — flips the case of the selected text, so a sentence
  typed with Caps Lock stuck on (`tHIS SENTENCE WRITTEN WITH CAPS LOCK ON`) becomes
  `This sentence written with caps lock on`.
- The fix only applies to **English** text. If the selection contains letters from another script
  (Hebrew, for example) or no Latin letters at all, nothing is changed and a *Left unchanged*
  notification explains why.
- The layout conversion hotkey (`F10`) is untouched and keeps working exactly as before.
- Settings dialog now has a second hotkey picker (**Fix CAPS LOCK**) with its own **Reset**
  button, and refuses to save two identical chords.
- New tray menu item **Fix CAPS LOCK in selection** for running the fix without the hotkey.
- New `CapsHotKey` key in `%APPDATA%\LangFix\settings.json`.
- `--selftest` now covers the case fixer as well as the layout mapping.

**Installer**

- LangFix now ships as an MSI. It installs to `C:\Program Files\LangFix`, adds a Start menu
  entry and registers the app to start when you sign in.
- The .NET runtime is bundled, so there is no prerequisite to install first.

## 1.0.1

**Settings GUI**

- New **Settings…** dialog in the tray menu — edit every option without touching JSON.
  - Hotkey picker: click the box and press the chord you want (`Ctrl`, `Alt`, `Shift`, `Win` plus
    any key, including `F10`, `Tab` and `Pause`), or press **Reset** to go back to `F10`.
  - Checkboxes for *paste the result over the selection*, *restore the previous clipboard
    contents*, *show a tray notification after each conversion* and *start with Windows*.
  - **Open settings.json** link for the cases where hand-editing is still preferable.
- The dialog reads `%APPDATA%\LangFix\settings.json` when it opens, so changes made outside the
  app are shown, and writes the same file on **Save**. The new hotkey is registered immediately.
- The global hotkey is released while the dialog is open, so pressing a chord assigns it instead
  of converting the selection behind the dialog.
- The tray menu item **Edit settings…** became **Settings…**; **Reload settings** is still there
  for changes made in an external editor.

## 1.0.0

First released version.

- System-wide hotkey (`F10` by default) converts the selected text between the **US English** and
  the **Windows Hebrew (SI-1452)** keyboard layouts, in place.
- Direction is auto-detected per selection, so mixed text such as `Eמעךןדי בישרשבאקרד` is fixed
  correctly.
- Clipboard round-trip (`Ctrl+C` → convert → `Ctrl+V`) runs on a worker thread, with the previous
  clipboard contents restored afterwards.
- Tray icon with *Convert selection now*, *Paste result automatically*, *Start with Windows*,
  settings and *Exit*.
- Settings stored in `%APPDATA%\LangFix\settings.json`.
- `--selftest` command-line switch verifies the layout mapping.
- Application icon embedded in `LangFix.exe`.
