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
- The hook thread keeps its own `lastWasLetter` flag. When a Trigger Key arrives and `lastWasLetter` is true, the hook swallows it (returns 1), increments its own `holdGeneration`, and enqueues it.
- While `holdGeneration > releasedGeneration`, the hook swallows *every* key and enqueues it. `releasedGeneration` is written only by the worker; `holdGeneration` only by the hook. One writer per variable (`Volatile.Read/Write`).
- The worker evaluates the Word; it then either applies the Correction or not, re-injects all held keys in order via `SendInput` (virtual-key events, Rekey tag), and publishes `releasedGeneration`.
- Safety valve: if the worker does not release within 150 ms, the hook releases on its own and re-injects held keys (Incident `hold-timeout`). Typing is never blocked longer than that.
- Which keys are Trigger Keys is a settings value the hook reads from an immutable snapshot swapped atomically by the worker.

### D2. Injection
- One `SendInput` call per Correction: `n × VK_BACK`, then the Rendering as `KEYEVENTF_UNICODE` down/up pairs, then held keys as normal VK events. All with `dwExtraInfo = 0x52454B59`.
- `n` = number of characters the replaced Words produce on screen in the current Layout (= Keystroke count, since en/ru/uk produce one character per key; dead keys are excluded by the scorer).
- Layout Switch (from `add-keyboard-capture-and-layouts`) happens after the text injection, before held keys are released, so the next typed key is already in the new Layout.

### D3. Verified replacement via UI Automation
- On the worker (UIA STA helper thread): get the focused element's `TextPattern`; take the caret range from `GetSelection()` (must be degenerate); clone it, `MoveEndpointByUnit(Start, Character, -len)`; compare `GetText` with the expected current Rendering (including spaces between Words of a deferred run).
- Match → `Select()` that range, then inject only the Rendering (typing over the selection) — no Backspaces.
- Mismatch → cancel (no injection except releasing held keys), record `correction-cancelled`.
- No TextPattern, or any UIA call exceeds 50 ms → retype as in D2. Per-app memory: an app that timed out twice is marked "slow" for the session and goes straight to D2 (the original also remembers slow apps).

### D4. Undo
- After a Correction the worker writes `lastCorrection` (immutable record: original text, Rendering, trigger text, source/target Layouts, time, `undoable = trigger != Enter`) and bumps `correctionGeneration`.
- Hook-side: on Ctrl+Z, if `correctionGeneration` changed since the last key the hook saw (hook-local counter), the record is `undoable`, and `now - time < 5 s` → swallow and enqueue `UndoRequested`. Any other key between makes the hook's counter advance, so Ctrl+Z passes through.
- Worker: Backspace over Rendering + trigger text, type the original text + trigger text, switch back to the source Layout, emit `UndoPerformed(word, rendering)` for learning and statistics.

### D5. ISO key in Key Maps
- `KeyMapReader` (worker thread, `MapVirtualKeyEx` + `ToUnicodeEx`) adds scan code 0x56 to its printable keys, giving 48 entries per Layout. Without it a Word containing that key has no Rendering, so it can neither be detected nor replaced. Both Ukrainian Layouts put "ґ" on that key, so without it "ґ" could not be typed or rendered at all.
- On Layouts where 0x56 repeats another key's characters (English (US) gives "\" on both 0x2B and 0x56), typing a text still picks the lower scan code; Rendering reads whichever key was pressed.

## Risks / Trade-offs

- [Holding Space adds latency to every word end] → bounded by decision time (< 5 ms target) and the 150 ms valve; measured in the latency benchmark.
- [Apps that react to Backspace specially (autocomplete popups, IDE snippets)] → verified replacement avoids most Backspaces; users can add such apps to Excluded Apps.
- [Some apps ignore `KEYEVENTF_UNICODE`] (rare: some games, remote-desktop clients) → listed as known limitation; suggest Excluded App.
- [Typing over a UIA selection can trigger app-specific "replace selection" behavior (e.g. auto-pairing quotes)] → only letters are typed over selections; held keys come after.
