# Design

## Context

The original offers double taps of Shift, Control, Option or Command, three Space-based shortcuts and the Fn/Globe key as the hotkey, a hotkey picker with hints, and conflict alerts for shortcuts macOS already uses.

## Goals / Non-Goals

**Goals:**
- Hotkey detection on the hook thread with no added latency for normal typing.
- Reuse rendering, scorer ranking and replacement from earlier changes.

**Non-Goals:**
- Double tap of Win: a single Win tap opens Start, so two taps flash Start open and closed; offering it would need swallowing Win, which breaks Win shortcuts. Not offered.
- The original's Fn/Globe key option: the Fn/Globe key is handled by keyboard firmware and is invisible to Windows, so there is no counterpart.
- Hotkey picker UI (`add-tray-and-settings-ui`).

## Decisions

### D0. Space presets (from decompiling the original)
Decompiling the original showed that its Space-based options make ⌃Space, ⌘Space or ⌥⌘Space the hotkey, so users keep their habitual input-source shortcut (⌃Space is the macOS default) and it also corrects the last Word. On a clash the original names the system action that would also fire and asks the user to change the system shortcut. Windows counterparts: Ctrl+Space and Win+Space (Windows' own Layout Switch shortcut, which cannot be turned off in Settings).
- Win+Space: the hook swallows Space while Win is down and, before Win is released, injects one Rekey-tagged unassigned virtual key (VK 0xE8) so the shell does not open Start on the lone Win release. With nothing to correct, the engine emits a Layout Switch to the next Layout, so the user keeps Windows' behavior.
- Ctrl+Space: swallowed like any custom combo; the picker warns that editors use it for autocomplete and Chinese IMEs for input-mode toggling.

### D1. Detection on the hook thread
A hook-local state machine: modifier down → modifier up with no other key → start 400 ms window → second down/up → fire. Any other key resets. Taps are never swallowed (apps still see Shift taps; harmless). Custom combos are detected in the hook too (not `RegisterHotKey`), because `RegisterHotKey` cannot express modifier-only gestures and we need one mechanism; the hook swallows the combo's final key.

### D2. What to correct
Engine keeps `RecentWord` = the last finished Word if only its Trigger Key followed it and no boundary event happened since. Priority: (1) last Correction within Undo rules → revert; (2) readable non-empty selection → convert selection; (3) current Word or RecentWord → correct; (4) Layout Switch to next Layout in Windows' list order.

### D3. Target Layout for Manual Correction
Rank all other active Layouts by score of their Rendering; pick the best even if not a common word (the user asked explicitly). Ties → next Layout in Windows' order.

### D4. Selection conversion
UIA `TextPattern.GetSelection()` text; map each character back to `(scanCode, shift)` through the current Layout's KeyMap (reverse map), then render in the target Layout. Characters not in the reverse map (digits, symbols present in both) stay as they are. Replacement types over the selection (selection stays active until typing). No clipboard (text-replacement requirement).

### D5. Conflict detection
- Windows Layout Switch shortcut: `HKCU\Keyboard Layout\Toggle` values `Hotkey`, `Language Hotkey`, `Layout Hotkey` (1 = Left Alt+Shift, 2 = Ctrl+Shift, 3 = none, 4 = grave accent) and Win+Space (always on).
- Other apps: probe with `RegisterHotKey` on a hidden window for custom combos; failure means taken; unregister immediately.
- Double taps never collide with these shortcuts (they are chords), so the check applies to custom combos only. A custom combo always has a non-modifier key, so modifier-only Windows toggles (Alt+Shift, Ctrl+Shift) cannot be picked as one; Win+Space and the grave accent can.
- The original guides users to turn the system shortcut off and back on in System Settings; Rekey likewise never changes Windows settings itself (design decision).

## Risks / Trade-offs

- [Double tap of Shift fires during fast typing of two capitals] → "no other key in between" rule; a capital letter keystroke resets the detector.
- [Double tap of Alt: a lone Alt tap activates menu bars in classic apps] → Alt option remains, with a hint in the picker; default is Shift.
- [Reverse map ambiguity for symbols present in several positions] → choose the unshifted position first; covered by tests.
