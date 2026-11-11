# Desktop checks: add-keyboard-capture-and-layouts

Word rules, Layout mapping, switch verification and both watchdogs are covered by unit tests. What needs a real Windows desktop is checked by a tool, plus three short checks by hand. Tick a task in `tasks.md` once all of its checks pass on the commit under review.

## Automated: `tools/Rekey.DesktopChecks`

Close every running Rekey and input probe, then run from the repository root (Debug build, the default):

```bash
dotnet run --project tools/Rekey.DesktopChecks
```

Do not touch the mouse or keyboard for about 5 minutes. The tool opens its own test windows, a local test page (`tools/Rekey.DesktopChecks/check-page.html`, invented fields) in the default browser and a console, types invented text, and prints one ✅ or ❌ line per check, a summary, and exit code 1 when any check fails. Output carries counts, kinds and codes only, never typed text. At the end it restores what it changed: the language list, the per-window input setting, Caps Lock, the mouse position and Rekey's settings and restart-state files; it closes its windows, the test page tab and the console.

| Task | What the tool checks |
|---|---|
| 2.1 | No Incident at start-up; a tagged injected key is ignored; an untagged one is a `Character`; typed keys build the Word; a click reaches the worker |
| 3.2 | A classic masked Win32 edit is a Password Field and keeps nothing; an ordinary edit is not one |
| 3.3 | On the test page, ten rounds by click and ten by Tab/Shift+Tab, typing at once: the user-name box builds Words, the password box keeps `Word keys = 0` |
| 4.1 | Adding a spare language (German, French or Italian) shows its Layout within 5 s; removing it drops it; the language list is back as before |
| 4.3 | Per-window mode: two test windows and a console keep their own Layouts; global mode: the Layout follows to the next window |
| 4.4 | A verified switch lands in a test window, a browser text box and a console, with no `SwitchNotLanded` |
| 5.3 | A hook stalled past Windows' limit is dropped and reinstalled; Shift released and Caps Lock toggled while it was gone are resynced; a second drop pauses Rekey; Resume ends the pause |
| 5.5 | Two freezes record `InputFrozen`, then `FreezeRestart`; the Debug app started with `--check-freeze` restarts itself and saves the restart time |

Last run: 30 of 30 checks passed at commit a11ca63 (owner's desktop, Firefox as default browser); the language list was restored. Tasks 2.1, 3.2, 3.3, 4.1, 4.3, 4.4, 5.5 and 6.2 are ticked on that run; 3.1 and 5.3 also need the checks by hand below.

If the browser does not expose the page's boxes to UI Automation, the 3.3 lines fail with that reason; make Edge or Chrome the default browser and run again.

## By hand

Use invented text.

- [ ] **Elevated window (3.1).** Run `dotnet run --project tools/Rekey.InputProbe` (one probe only). Open Notepad, then a Command Prompt with "Run as administrator", then Notepad again; type a few letters in each.
  Expected: Notepad gives `IsElevated = False`, the elevated console `IsElevated = True`, Notepad again `IsElevated = False`; no `INCIDENT` line while typing in the elevated console.
- [ ] **Modifiers after Win+L (3.1).** With the probe running, press Win+L, unlock, click into Notepad and type a letter.
  Expected: the letter's `KeyDown` line shows `Modifiers = None`.
- [ ] **Self-pause in the tray (5.3).** Stop the probe. Run `src/Rekey.App/bin/Debug/net10.0-windows/Rekey.App.exe --check-self-pause`, click into Notepad and keep typing for 15 seconds.
  Expected: the tray tooltip says Rekey paused itself and the tray menu shows Resume; choosing Resume hides the item and restores the tooltip. Quit Rekey from the tray afterwards.
