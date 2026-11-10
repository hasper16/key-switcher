# Manual checks: add-keyboard-capture-and-layouts

Word rules, Layout mapping, switch verification and both watchdogs are covered by automated tests. These checks need a person at a Windows desktop. Each item names the task it verifies; tick the item and the task together.

The input probe prints what Rekey's worker sees, never the typed characters (a `KeyDown` line shows only its kind and modifiers):

```bash
dotnet run --project tools/Rekey.InputProbe
```

Stop it with Ctrl+C in its window. Use invented text when typing.

## Hooks and injected input (task 2.1)

- [ ] **Typed keys reach the worker, Rekey's own keys do not.** Run `dotnet run --project tools/Rekey.InputProbe -- --inject`, then click into Notepad within 5 seconds.
  Expected: Notepad shows `aa`. After "Injecting a tagged key" no `KeyDown` line appears; after "Injecting an untagged key" one `KeyDown { …, Kind = Character, … }` line appears. Typing a few letters in Notepad prints one `KeyDown` line per key; clicking prints `MouseClick`.

## Focus, Password Fields and elevation (tasks 3.1, 3.2)

- [ ] **Focus and elevation (3.1).** With the probe running as a normal user, open Notepad, then a Command Prompt started with "Run as administrator", then Notepad again.
  Expected: Notepad gives `FocusChanged { AppId = C:\Windows\…\notepad.exe, IsPassword = False, IsElevated = False }`; the elevated console gives `IsElevated = True`; back in Notepad `IsElevated = False`. No `INCIDENT` line appears while typing in the elevated console.
- [ ] **Browser password field (3.2).** Open any login page in Edge or Chrome, click the user-name box, then the password box.
  Expected: user name gives `IsPassword = False`, password gives `IsPassword = True`.
- [ ] **Password field in the same window, typed at once (3.3).** On a browser login page click the user-name box, then press Tab and immediately type a few letters into the password box. Repeat ten times, also with Shift+Tab back and forth and by clicking from one box into the other.
  Expected: every `KeyDown` line typed in the password box ends with `Word keys = 0`, both before and after the `FocusChanged … IsPassword = True` line appears. Typing in the user-name box builds a Word as usual (`Word keys` counts up).
- [ ] **Win32 password dialog (3.2).** Open a classic dialog with a masked box (for example Control Panel → User Accounts → Credential Manager → Add a Windows credential), click the password box.
  Expected: `IsPassword = True`; the other boxes give `IsPassword = False`.

- [ ] **Modifiers and Caps Lock after missed key-ups (3.1).** With the probe running, press Win+L and unlock; then Alt+Tab into the elevated console, toggle Caps Lock there, and Alt+Tab back to Notepad; type a letter.
  Expected: the letter's `KeyDown` line shows `Modifiers = None` (or `Shift` while Shift is held), not `Windows` or `Alt`, and its `CapsLock` value matches the Caps Lock light.

## Layouts (tasks 4.1, 4.3, 4.4)

- [ ] **Layout added while running (4.1).** With the probe running and no Ukrainian Layout installed, add Ukrainian in Settings → Time & language → Language & region.
  Expected: within 5 seconds a `LayoutsInstalledChanged` line lists `00000422` (or the Ukrainian variant you added). Remove it afterwards: another line drops it.
- [ ] **Current Layout, global mode (4.3).** In Settings → Time & language → Typing → Advanced keyboard settings, leave "Let me use a different input method for each app window" off. In Notepad press Win+Space to switch Layouts.
  Expected: each switch prints `LayoutChanged <KLID>` with the newly selected Layout.
- [ ] **Current Layout, per-window mode and console (4.3).** Turn that option on. Set Russian in one Notepad window and English (US) in another; switch between the windows, then into Windows Terminal or a classic console and switch its Layout there.
  Expected: moving focus prints `LayoutChanged 00000419` / `LayoutChanged 00000409` to match each window; switching inside the console prints the console's new Layout.
- [ ] **Verified switch (4.4).** Run `dotnet run --project tools/Rekey.InputProbe -- --switch 00000419` with English (US) active, and within 5 seconds focus Notepad; repeat for a browser text box and Windows Terminal.
  Expected: each window switches to Russian and the probe prints `LayoutChanged 00000419` with no `INCIDENT SwitchNotLanded`. If a window ignores the request, the probe prints `INCIDENT SwitchNotLanded` and `LayoutChanged` with the window's real Layout instead.

## Watchdogs (tasks 5.3, 5.5)

Build Debug (`dotnet build src/Rekey.App`) and watch Incidents with Sysinternals DebugView (they go to `Trace`, prefixed `Rekey Incident`).

- [ ] **Dropped hook is reinstalled (5.3).** Start `tools/Rekey.InputProbe` under Visual Studio's debugger, set a breakpoint in `InputHook.OnKeyboard`, focus Notepad and hold Shift, press a letter so the breakpoint hits, release Shift, wait 15 seconds, then continue and type letters in Notepad for a few seconds.
  Expected: Windows drops the hook while it is stuck; within 2 seconds of typing again the probe prints `INCIDENT HookReinstalled`; the next `KeyDown` lines show `Modifiers = None` (the missed Shift release is resynced) and a `CapsLock` value that matches the light; typing keeps printing `KeyDown` lines. Doing it twice within 10 minutes also prints `INCIDENT SelfPaused`.
- [ ] **Self-pause in the tray (5.3).** Repeat the two drops above with the Debug App (`src/Rekey.App/bin/Debug/net10.0-windows/Rekey.App.exe`) under the debugger, watching Incidents in DebugView.
  Expected: after the second drop the tray tooltip says Rekey paused itself and the tray menu shows Resume; choosing Resume hides the item and restores the normal tooltip.
- [ ] **Restart after a repeated freeze (5.5).** Run the Debug App normally. From the tray menu choose "Debug: freeze input for 7 s" and keep typing in Notepad for those 7 seconds; wait for typing to work again; do it once more.
  Expected: the first freeze shows `InputFrozen`; the second shows `FreezeRestart`, the tray icon disappears and comes back within a few seconds (a new Rekey process), and `%LOCALAPPDATA%\Rekey\restart-state.txt` holds the restart time. Repeating both freezes within the next 10 minutes shows `InputFrozen` twice and no restart.
