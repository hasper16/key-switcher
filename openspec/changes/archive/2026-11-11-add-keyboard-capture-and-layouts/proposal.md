# Proposal

## Why

The Decision logic needs a live stream of Keystrokes and accurate knowledge of the user's Layouts. The original app gets these from a CGEventTap and the TIS input-source API, and spends a whole module on recovering when macOS silently disables the tap. Windows has the same failure mode: a slow low-level hook is silently removed. This change gives Rekey a reliable view of typing and Layouts.

## What Changes

- Record Keystrokes system-wide and track the current Word: letters extend it, Backspace shortens it, navigation keys, shortcuts, mouse clicks, focus changes and a typing gap over 2 seconds end it.
- Never record typing in Password Fields.
- Mark Rekey's own injected input so it is never treated as user typing.
- List installed Layouts, follow the active Layout of the foreground window, build a KeyMap per Layout, and perform a verified Layout Switch.
- Detect when Windows drops the keyboard hook, reinstall it, record an Incident, and pause Auto Correction after repeated failures.
- Restart Rekey after a repeated freeze of its own input processing, at most once per 10 minutes.
- Settings: `KeyboardWatchdogEnabled`, `RestartAfterFreezeEnabled` (both on).

## Capabilities

### New Capabilities

- `keyboard-capture`: How Rekey observes the user's typing, delimits Words, protects Password Fields, and stays attached to the keyboard.
- `layout-management`: How Rekey knows the installed and active Layouts and switches between them reliably.

### Modified Capabilities

None.

## Impact

- New code: `Rekey.Win32/Keyboard` (low-level keyboard and mouse hooks, hook thread, watchdog), `Rekey.Win32/Layouts` (layout list, KeyMap builder, switcher), `Rekey.Win32/Focus` (foreground app, Password Field detection via UI Automation).
- Engine input events (`InputEvent`) gain focus-change, layout-change, gap and reset events.
- Depends on `bootstrap-solution` and `add-language-scorer` (KeyMap type).
