# Proposal

## Why

Auto Correction deliberately leaves doubtful Words alone, and some users keep it off. They still need a fast way to say "this word is in the wrong Layout, fix it". The original offers a Hotkey (double tap of Shift, Control, Option or Command), warns about clashes with system shortcuts, and lets the Hotkey work everywhere except password fields.

## What Changes

- Hotkey options: double tap of Shift (default), Ctrl or Alt, Ctrl+Space, Win+Space (takes over the Windows Layout Switch shortcut), or a custom key combination. The Space options are the counterparts of the original's ⌃Space, ⌘Space and ⌥⌘Space options.
- Manual Correction of the last Word, of a selected text, or — with nothing to correct — a plain Layout Switch.
- Pressing the Hotkey right after a Correction reverts it.
- Conflict check against the Windows Layout Switch shortcuts and hotkeys already registered by other apps.

## Capabilities

### New Capabilities

- `manual-correction`: How the user asks Rekey to correct a Word or selection on demand, which gestures are available, and how conflicts with system shortcuts are handled.

### Modified Capabilities

None.

## Impact

- Core: double-tap detector, Manual Correction actions (reuses rendering and replacement).
- Win32: selected-text reading via UI Automation, custom hotkey via the keyboard hook, reading Windows Layout Switch shortcut settings from the registry.
- Settings: `Hotkey`.
- Depends on `add-text-replacement`; UI for choosing the Hotkey is in `add-tray-and-settings-ui`.
