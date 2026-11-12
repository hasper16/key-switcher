# Proposal

## Why

Once Rekey decides a Word is wrong, it must change the text on screen safely: in the right order relative to the Trigger Key, without touching the clipboard, without corrupting text when its view of the Word is out of date, and with a quick way back. The original retypes the Word or, where possible, swaps it in place through the Accessibility API, and lets the user undo a Correction with Cmd+Z within five seconds.

## What Changes

- Apply a Correction by erasing the Word and typing its Rendering, then performing the Layout Switch.
- Hold the Trigger Key (and any keys typed during the Correction) and deliver them after the corrected text, in order.
- Where the focused control exposes its text, verify that the text before the caret matches the Word before changing anything, and select the Word instead of erasing it key by key ("verified replacement", on by default).
- Undo: Ctrl+Z within 5 seconds of a Correction, before any other typing, restores the original text and Layout.
- Never use or change the clipboard.
- Key Maps include the ISO key (scan code 0x56), so Words typed with it on ISO keyboards can be rendered and replaced.

## Capabilities

### New Capabilities

- `text-replacement`: How a Correction changes text on screen, how ordering is preserved, how mismatches are avoided, and how the user undoes a Correction.

### Modified Capabilities

- `layout-management`: the Key Map covers the ISO key as well as the 47 ANSI printable keys.

## Impact

- New code: `Rekey.Win32/Keyboard/HoldGate` (trigger holding on the hook thread), `Rekey.Win32/Replacement` (SendInput injector with the Rekey tag, UI Automation text reader and selector).
- Engine: `ReplaceText` and `UndoPerformed` actions, `UndoRequested` and `ReplacementFinished` events, a `WordEvaluator` seam that `add-auto-correction` fills.
- Settings: `VerifiedReplacement` (on).
- `Rekey.Win32/KeyMapReader` reads scan code 0x56; key-map test fixtures are regenerated with `tools/Rekey.KeyMapCapture`.
- Depends on `add-keyboard-capture-and-layouts`.
