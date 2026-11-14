# Desktop checks: add-manual-correction

The Hotkey setting, the double-tap and combination state machines, the engine's priorities (revert, selection, Word, Layout Switch), the target among three Layouts, the selection conversion and the reading of the Windows Toggle values are covered by unit tests in `Rekey.Core.Tests` and `Rekey.Win32.Tests`. What needs a real Windows desktop is checked by a tool, plus a short checklist by hand for the app itself. Tick a task in `tasks.md` once all of its checks pass on the commit under review.

## Automated: `tools/Rekey.DesktopChecks manual-correction`

Needs the English (US) and Russian Layouts installed. Close every running Rekey and input probe, then run from the repository root (Debug build, the default):

```bash
dotnet run --project tools/Rekey.DesktopChecks -- manual-correction
```

Do not touch the mouse or keyboard for about 3 minutes. The tool runs Rekey's worker in its own process with no Word Evaluator of its own, so Words are evaluated as in the app, and changes the Hotkey and Auto Correction settings in memory while it runs; the settings file is not touched. It types the spec's example Words into its own test window, into Notepad on an empty file in the temp folder and into the local test page in the default browser, performs the Hotkey with injected keys, and reads the text back through UI Automation. It prints one ✅ or ❌ line per check, a summary, and exit code 1 when any check fails. Output carries booleans, counts, kinds and codes only, never text. At the end it restores Caps Lock and the mouse position, empties and closes the Notepad file, closes the test tab and its windows. It never reads or writes the clipboard: it compares the clipboard's sequence number before and after.

| Task | What the tool checks |
|---|---|
| 1.2 | With Auto Correction off, a double tap of Shift after "ghbdtn" gives "привет" and the Layout Russian; after "yt " it gives "не " |
| 1.3 | With Ctrl+Alt+K as the Hotkey, "ghbdtn" in Notepad becomes "привет" and nothing else: the K never reaches Notepad |
| 1.4 | With Win+Space as the Hotkey, "ghbdtn" becomes "привет", the Layout is Russian and stays Russian (no second switch), and the test window is still in front (Start did not open); in an empty box Win+Space only switches to the Layout after English (US) in Windows' order, again with the test window in front |
| 2.1 | With Auto Correction on, "ghbdtn" + Space is corrected and a double tap of Shift right after it reverts the text to "ghbdtn ", records one Undo and switches back to English (US) |
| 3.2 | "ghbdtn vbh" selected with Ctrl+A becomes "привет мир" in the test window, in Notepad and in the browser test page; a failing line also says how many characters the worker read as selected and whether the rest of the text is still as typed |
| 4.2 | Win+Space as a custom combination is reported as the Windows Layout Switch shortcut; Ctrl+Alt+K is free, and is reported as taken while another thread holds it as a registered hotkey |

Last run: not yet run.

## By hand

The app has no settings window before `add-tray-and-settings-ui`, so the Hotkey is changed in `%LOCALAPPDATA%\Rekey\settings.json` while Rekey is not running (Rekey writes the file on exit). Use a Debug build of `src/Rekey.App` and English (US) as the starting Layout. These checks use the physical keyboard, which the tool cannot.

- [ ] **Double tap of Shift (1.2, 2.1).** Start Rekey with `"autoCorrectionEnabled": false`. In Notepad type "ghbdtn" and tap Shift twice quickly. Switch back to English (US), press Enter for a new line, then type "Ghbdtn" holding Shift for the capital and tap Shift once.
  Expected: the first Word becomes "привет" and the Layout Russian; the second is left as typed (the Shift held for "G" does not count as a tap).
- [ ] **Win+Space with the physical keys (1.4).** Quit Rekey, set `"hotkey": "winSpace"`, start Rekey. In Notepad, in English (US), type "ghbdtn" and press Win+Space; then on a new empty line press Win+Space once more; then press Win alone.
  Expected: "ghbdtn" becomes "привет" and the Layout is Russian, with no Windows layout pop-up switching a second time and no Start menu; on the empty line the Layout becomes English (US) again and Start stays closed; Win alone opens Start as usual.
- [ ] **Selection a control does not expose (3.2).** Quit Rekey, set `"hotkey": "doubleShift"`, start Rekey. Open the Run dialog (Win+R), type "ghbdtn", select it with Shift+Home and tap Shift twice.
  Expected: if the field exposes its selection, it becomes "привет"; if it does not, the text stays "ghbdtn" and only the Layout switches. Report which happened.
- [ ] **Password Field (2.3).** On any sign-in page, type "ghbdtn" in the password box and tap Shift twice.
  Expected: nothing happens: the box keeps its characters and the Layout stays English (US).
