# Design

## Context

The original swaps the word in place through the macOS Accessibility API where it can and falls back to retyping, cancels a replacement when the user interferes, re-posts the Trigger Key after the corrected text, and lets the user undo with ⌘Z within five seconds. Windows UI Automation has no generic "insert text" call (TextPattern is read/select only; ValuePattern.SetValue replaces the whole value and breaks the app's own undo), so we use UIA to verify and select, and typing to replace.

## Goals / Non-Goals

**Goals:**
- Correct ordering of corrected text and the Trigger Key without shared mutable state between threads.
- No text damage when Rekey's view is stale.

**Non-Goals:**
- Deciding what to correct (scorer) and when (`add-auto-correction`, `add-manual-correction`).
- Learning from Undo (`add-word-learning`) — this change only reports Undo events.

## Decisions

### D1. Holding keys without shared state
- The hook thread owns a `HoldGate` with hook-only fields: `lastWasCharacter` (the last key-down was a character key with no Ctrl, Alt or Windows key held), the held modifiers, `lastHeld` (ring sequence of the last key it held) and `lastKeyDown` (ring sequence of the last non-modifier key-down).
- When a Trigger Key goes down after a character key and the Hook Policy allows holding, the hook writes it to the ring marked as held and swallows it (returns 1). While `lastHeld > released`, the hook holds *every* key event, downs and ups alike, so a held modifier can never be released out of order.
- `released` is written only by the worker: the ring sequence of the last held key it finished. The worker handles each held key in ring order: it feeds the engine, executes the actions, re-injects the key, then publishes `released`. Because the hook keeps holding until every key it held has been re-injected, a key it lets through can never overtake a held one. One writer per variable (`Volatile.Read/Write`).
- A key is held only when the ring has room for it; a key the ring cannot take passes through.
- Safety valve: when a key arrives more than 150 ms after the hold began, the hook stops holding until the worker catches up and counts a hold timeout (Incident `HoldTimeout`, recorded by the worker). Typing is never blocked longer than that. Keys held before the valve are still re-injected, late but not lost. To keep a late Correction from erasing keys the valve let through, the worker starts a Correction or Undo only while its key is less than 50 ms old, and checks again just before `SendInput`, after UI Automation, that it is less than 100 ms old, 50 ms below the valve (design decisions).
- The Hook Policy (whether to hold, the Trigger Keys, the character scan codes of the active Layouts) is an immutable snapshot the worker swaps atomically. In this change the host holds Space and Enter only when a Word Evaluator is installed (D6); `add-auto-correction` derives the policy from its gate and settings.

### D2. Injection
- Every injected input carries `dwExtraInfo = 0x52454B59`, so the hook ignores it.
- A replacement is one `SendInput` call: a key-up for each Shift, Ctrl, Alt or Windows key still down (Ctrl+Backspace would delete a whole word, and the user's Ctrl is still down during Undo), pressed again after the text; the Right that collapses a selection (D3) is wrapped the same way, as Shift+Right would extend it, `n × VK_BACK`, then the Rendering as `KEYEVENTF_UNICODE` down/up pairs.
- `n` = the number of characters the replaced Words show on screen in the current Layout, which equals their Keystroke count, since en/ru/uk produce one character per key and the Word Evaluator gets no Word with a dead key.
- The Layout Switch (from `add-keyboard-capture-and-layouts`) is requested after the text and before held keys are re-injected, so the next typed key lands in the new Layout. Held keys are re-injected by scan code (`KEYEVENTF_SCANCODE`, plus the extended and key-up flags as seen), so Windows maps them through the Layout active when they arrive.

### D3. Verified replacement via UI Automation
- On the worker thread, with its own `IUIAutomation` whose connection and transaction timeouts are 50 ms (design decision), as the Password Field probe already does: get the focused element's `TextPattern`; take the caret range from `GetSelection()` (one degenerate range); clone it, `MoveEndpointByUnit(Start, Character, -len)`; compare `GetText` with the text Rekey expects before the caret (the Rendering of the Words in the current Layout, spaces included for a Deferred run).
- Match → `Select()` that range, then type only the replacement over it, no Backspaces.
- Mismatch → cancel: inject nothing, record Incident `CorrectionCancelled`, and still re-inject held keys.
- No TextPattern, no single degenerate caret range, a failed call, or more than 50 ms spent in total → retype as in D2. A selection that runs out of time cancels instead: the app handles the pending UIA request before any input, so the selection lands before the held keys. Rekey therefore sends Right to collapse it back to the caret, or a re-injected Space would type over the Word. The same collapse follows a selected Word whose key turned stale before the text was typed. No Right is sent after a click or a focus move: it would land in another place, the click has already collapsed the selection, and the held keys go to the other control.
- A mouse click that came after the Trigger Key (the hook notes clicks; it does not hold them) cancels the replacement, since the click reached the app first and may have moved the caret; so does focus now being in another window than when the key was read (the worker reads focus directly before each key, without waiting for its WinEvent, and the focused window again just before `SendInput`). So does `SendInput` taking fewer inputs than sent (as for a window of higher integrity): nothing is switched or made undoable. An app that ran out of time twice is "slow" for the session and goes straight to D2 (the original also remembers slow apps); apps are told apart by `Context.AppId`.
- The decision (`VerifiedReplacement.Prepare`) takes the reader as an interface, so it is unit-tested with a fake reader; only the UIA reader itself needs a desktop.
- `Settings.VerifiedReplacement = false` skips the reader and always retypes.

### D4. Undo
- When a Correction's replacement is applied, the engine keeps `LastCorrection` (typed text, Rendering, trigger text, source and target Layouts, time; `Undoable` unless the trigger was Enter) and the worker publishes the trigger's ring sequence as `undoArmed`.
- Hook side: on Ctrl+Z (no Alt or Windows key), if `lastKeyDown == undoArmed` — nothing but modifiers came after the Trigger Key — the hook holds the key as an Undo request. It checks nothing else; the engine decides.
- Engine: an `UndoRequested` within the Undo Window of an undoable `LastCorrection`, with no other key, click or focus change since, yields `ReplaceText(Rendering + trigger text, typed text + trigger text)`; once applied, `SwitchLayout(source)` and `UndoPerformed(typed, Rendering)` for learning and statistics. Otherwise the request ends the Word like any shortcut.
- Worker: a held Undo request whose replacement was applied is consumed; any other is re-injected, so the application gets its Ctrl+Z.

### D5. ISO key in Key Maps
- `KeyMapReader` (worker thread, `MapVirtualKeyEx` + `ToUnicodeEx`) adds scan code 0x56 to its printable keys, giving 48 entries per Layout. Without it a Word containing that key has no Rendering, so it can neither be detected nor replaced. Both Ukrainian Layouts put "ґ" on that key, so without it "ґ" could not be typed or rendered at all.
- On Layouts where 0x56 repeats another key's characters (English (US) gives "\" on both 0x2B and 0x56), typing a text still picks the lower scan code; Rendering reads whichever key was pressed.

### D6. Word Evaluator seam
- Deciding what to correct belongs to `add-auto-correction`. Until then the engine takes an optional `WordEvaluator` (Keystrokes and Layout of a Word → `Decision`) and calls it for a Word ended by a held, unmodified Trigger Key; the Decision goes through the engine's `DeferredRun`. With no evaluator nothing is held or corrected, so the app behaves as before this change.
- The engine remembers the on-screen text of the Words in the current run (each rendered in the Layout it was typed in, separated by single spaces). A Word ended by anything but an evaluated Space, a click, a focus change, a typing gap or lost input ends the run and resets `DeferredRun`, so a run on screen is always the Words joined by single spaces.
- Replacing text is a round trip, as ADR 0002 requires: the engine returns `ReplaceText(expected, replacement)`; the worker verifies and injects, then reports `ReplacementFinished(applied)`; only then does the engine return the Layout Switch (and, for Undo, `UndoPerformed`). A cancelled replacement switches nothing and arms no Undo.
- The engine renders Words with the Key Maps it receives in `LayoutsInstalledChanged`.

## Risks / Trade-offs

- [Holding Space adds latency to every word end] → bounded by decision time (< 5 ms target) and the 150 ms valve; nothing is held while no Word Evaluator is installed.
- [The last check and `SendInput` race with the valve or a click arriving in between] → accepted: the window is one `SendInput` call wide; closing it would mean holding and replaying mouse input.
- [A mouse click during a hold reaches the app before held keys] → clicks are not held; a click after the Trigger Key cancels its replacement (D3).
- [Apps that react to Backspace specially (autocomplete popups, IDE snippets)] → verified replacement avoids most Backspaces; users can add such apps to Excluded Apps.
- [Some apps ignore `KEYEVENTF_UNICODE`] (rare: some games, remote-desktop clients) → listed as known limitation; suggest Excluded App.
- [Typing over a UIA selection can trigger app-specific "replace selection" behavior (e.g. auto-pairing quotes)] → only letters are typed over selections; held keys come after.
