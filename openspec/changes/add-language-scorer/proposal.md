# Proposal

## Why

Rekey's core job is deciding, for each Word, whether it was typed in the wrong Layout and what it should be. The original app does this with per-layout scores, a gibberish ceiling, cross-script factors, Ambiguous Words and Deferred Words, tuned by three Sensitivity levels. Without this decision logic, nothing else in Rekey has a purpose.

## What Changes

- Add the Decision logic: given a Word's Keystrokes, the current Layout and the active Languages, render the Word in every active Layout, score each Rendering, and return correct / keep / defer.
- Support three Sensitivity levels (Cautious, Balanced default, Aggressive) with the behavior described in the original's UI.
- Handle Ambiguous Words, short words (one or two letters), punctuation keys that are letters in another Layout (`,` → `б`), punctuation at the edges of a Word, letter case, and same-script pairs (Russian ↔ Ukrainian).
- Add the `Sensitivity` setting.
- Add Deferred Words: an uncertain Word waits for the next Word to confirm the direction; a confirmed run is corrected together.
- Add an accuracy harness that measures false corrections on correctly typed text and missed corrections on wrong-layout text, used to tune thresholds.

## Capabilities

### New Capabilities

- `wrong-layout-detection`: How Rekey decides that a Word was typed in the wrong Layout, what it should become, and when to wait for more evidence.

### Modified Capabilities

None.

## Impact

- New code: `Rekey.Core/Detection` (key maps, renderer, scorer, decision policy, deferred-word tracking).
- New tool: `tools/Rekey.Accuracy` (corpus evaluation) and test corpora under `data/accuracy/`.
- Depends on `add-language-profiles`. Consumed by `add-auto-correction` and `add-manual-correction`.
