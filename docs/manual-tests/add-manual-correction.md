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
| 3.2 | "ghbdtn vbh" selected with Ctrl+A becomes "привет мир" in the test window, in Notepad (with the Shift taps and with Ctrl+Alt+K) and in the browser test page, and "ghbdtn" alone becomes "привет" in Notepad; a failing line also says how many characters the worker read as selected, whether the rest of the text is still as typed, and the kinds of the characters that differ |
| 4.2 | Win+Space as a custom combination is reported as the Windows Layout Switch shortcut; Ctrl+Alt+K is free, and is reported as taken while another thread holds it as a registered hotkey |

Last run: 15 of 15 at f4ec2f6 on the owner's desktop (run 7 of the day; English (US), Russian and Ukrainian Layouts; default browser as installed); language list unchanged, nothing left running. As f4ec2f6 changed the replacement path the earlier changes share, the same run repeated their sets on that commit: `auto-correction` 27 of 27, and `text-replacement` 19 of 20 with every product check passing (Undo and VS Code included); its one failure was the tool's own cleanup of the VS Code test folder, held by a VS Code still running. Run 6 at 00602b0 had passed 16 of 16 before the last fix, which made a stopped multi-part typing press the held modifiers again and stop on a focus move.

Earlier runs that day found two product faults, both fixed before the last run: Start opened after a Win+Space correction, because typing pressed Win again after the masking key (fixed in 50012e3); and Notepad garbled the second Word of a converted selection. Control lines typing "привет мир" over a Notepad selection without Rekey showed the cause: Notepad garbles or drops what follows a space in one burst of injected input, and takes it whole key by key. Rekey now types text with spaces in parts (1ded4ab, design D4), and the control lines were removed once the cause was recorded. The run before the last also had two lines stopped by another window taking the foreground (a click did not bring the test window to the front; the test window was not in front after Win+Space in an empty box); they passed in the next run.

## By hand

Deferred by the owner's decision of 2026-11-14 to the final acceptance pass after all roadmap changes: see the `add-manual-correction` section of [deferred-by-hand.md](deferred-by-hand.md).
