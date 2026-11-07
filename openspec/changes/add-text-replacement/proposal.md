# Proposal

## Why

Once Rekey decides a Word is wrong, it must change the text on screen safely: in the right order relative to the Trigger Key, without touching the clipboard, without corrupting text when its view of the Word is out of date, and with a quick way back. The original retypes the Word or, where possible, swaps it in place through the Accessibility API, and lets the user undo a Correction with Cmd+Z within five seconds.

## What Changes

- Apply a Correction by erasing the Word and typing its Rendering, then performing the Layout Switch.
- Hold the Trigger Key (and any keys typed during the Correction) and deliver them after the corrected text, in order.
- Where the focused control exposes its text, verify that the text before the caret matches the Word before changing anything, and select the Word instead of erasing it key by key ("verified replacement", on by default).
- Undo: Ctrl+Z within 5 seconds of a Correction, before any other typing, restores the original text and Layout.
- Never use or change the clipboard.

## Capabilities

### New Capabilities

- `text-replacement`: How a Correction changes text on screen, how ordering is preserved, how mismatches are avoided, and how the user undoes a Correction.

### Modified Capabilities

None.

## Impact

- New code: `Rekey.Win32/Input` (SendInput injector with Rekey tag, trigger holding), `Rekey.Win32/Automation` (UI Automation text reader/selector).
- Engine actions: `ReplaceWord`, `HoldKeys`/`ReleaseKeys`, `Undo` handling in Core.
- Settings: `VerifiedReplacement` (on).
- Depends on `add-keyboard-capture-and-layouts`.
