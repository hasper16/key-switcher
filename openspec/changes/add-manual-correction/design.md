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
- Only Space or Tab may follow a RecentWord. After Enter the line may have been submitted, and retyping it would submit again, so Enter ends the RecentWord (as Enter ends the Undo of a Correction).
- Modifier keys neither end nor extend the typing gap: the Hotkey's own Shift or Ctrl taps must not end a Word the user noticed late. A modifier still counts for Undo (Ctrl+Z).
- A Word with a Layout Switch by the user inside it, a click, a focus change, lost input or an applied Correction leave nothing to correct: the text on screen is no longer known to be the Keystrokes in one Layout.

### D2a. Hook plumbing
The hook's `HoldGate` asks a `HotkeyDetector` about every key. A fired Hotkey is held like a Trigger Key, so keys typed after it wait until the Manual Correction is on screen. A double tap's second release goes on to the application afterwards; a combo's key and its repeats and release are swallowed. When Win is down at that moment, the worker injects the masking key before it lets the held keys (Win's release among them) go on.

### D3. Target Layout for Manual Correction
Rank all other active Layouts by score of their Rendering; pick the best even if not a common word (the user asked explicitly). Ties → next Layout in Windows' order.

### D4. Selection conversion
UIA `TextPattern.GetSelection()` text; map each character back to `(scanCode, shift)` through the current Layout's KeyMap (reverse map), then render in the target Layout. Characters not in the reverse map (digits, symbols present in both) stay as they are. Replacement types over the selection (selection stays active until typing). No clipboard (text-replacement requirement).
- Target: the other Layout whose conversion scores best on average over its words; a Layout that changes nothing is skipped, and with none left the Hotkey only switches Layout.
- A selection with a line break, or longer than 500 characters, is not typed over: the Hotkey only switches Layout. Typed line breaks can submit forms, and a long retype is slow and easy to interrupt.
- The selection is read only outside Password Fields and within the 50 ms UI Automation budget; the typing then goes through the same staleness checks as a replacement.
- "Selection not readable" means a control that exposes text but whose selection could not be read: several selections, a UI Automation error, or no answer within the budget. The Hotkey then only switches Layout. A control that exposes no text at all counts as having no selection, so the Word path still runs there (consoles, for one); selecting with the mouse or keys has already ended the Word, so that path finds nothing to correct.
- Before typing over the selection, the host checks that focus is still on the control the selection was read from.
- Typed text with spaces goes out in parts, each later part after a 50 ms pause. Desktop checks showed Notepad garbling or dropping what follows a space when it arrives in the same burst of injected input, with or without Rekey; key by key it took everything. The same split applies to every replacement, so a run of Deferred Words corrected at once is safe too. A selection of many Words therefore takes longer to type, and keys typed meanwhile may pass the hook's 150 ms valve.
- When Win is held at the Hotkey, the worker injects the masking key before it types anything, because the typing releases and re-presses held modifiers.

### D5. Conflict detection
- Windows Layout Switch shortcut: `HKCU\Keyboard Layout\Toggle` values `Hotkey`, `Language Hotkey`, `Layout Hotkey` (1 = Left Alt+Shift, 2 = Ctrl+Shift, 3 = none, 4 = grave accent) and Win+Space (always on).
- Other apps: probe with `RegisterHotKey` on a hidden window for custom combos; failure means taken; unregister immediately.
- Double taps never collide with these shortcuts (they are chords), so the check applies to custom combos only. A custom combo always has a non-modifier key, so modifier-only Windows toggles (Alt+Shift, Ctrl+Shift) cannot be picked as one; Win+Space and the grave accent can.
- The original guides users to turn the system shortcut off and back on in System Settings; Rekey likewise never changes Windows settings itself (design decision).

## Risks / Trade-offs

- [Double tap of Shift fires during fast typing of two capitals] → "no other key in between" rule; a capital letter keystroke resets the detector.
- [Double tap of Alt: a lone Alt tap activates menu bars in classic apps] → Alt option remains, with a hint in the picker; default is Shift.
- [Reverse map ambiguity for symbols present in several positions] → choose the unshifted position first; covered by tests.
