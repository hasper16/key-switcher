# Design

## Context

The original scores the Rendering for every Layout, rejects gibberish below a ceiling and a minimum threshold, applies separate factors when the two Layouts use different scripts, keeps a set of words valid in both Layouts, defers doubtful words up to a maximum count, and has three sensitivity modes. Its formulas were **not** recovered; everything numeric below is our design decision, tuned by the accuracy harness.

## Goals / Non-Goals

**Goals:**
- A pure, deterministic Decision function testable from key sequences.
- Thresholds tuned by measurement, not guesswork.

**Non-Goals:**
- Capturing keys, executing corrections, Trigger Keys, Excluded Apps, Pause (change `add-auto-correction`).
- Protected Words and Learned Words (change `add-word-learning` modifies this capability).

## Decisions

### D1. Words are Keystrokes, not text
A Word is a list of `Keystroke(scanCode, shift, capsLock)`. Rendering is a lookup in a `KeyMap` per Layout: `(scanCode, shift ^ capsLockApplies) → string`. `KeyMap` is plain data in `Rekey.Core`; the Win32 side builds it once per Layout with `ToUnicodeEx` (change `add-keyboard-capture-and-layouts`). Tests use checked-in KeyMap fixtures for English (US), Russian, Ukrainian, Ukrainian (Enhanced), generated once from a real Windows machine.
- Why: the same keystrokes give different text per Layout; storing characters would lose that.
- Dead keys and AltGr: a Word containing a dead-key or AltGr keystroke is kept (not rendered). Rare in en/ru/uk.

### D2. Scoring
For each Rendering `r` in Language `L`:
- `isWord(r)`: normalised `r` ∈ commonWords(L) ∪ words1(L) ∪ words2(L).
- `ngram(r)`: mean log-frequency of letter bigrams and trigrams of `^r$`, with a floor for unseen n-grams (`log(1e-6)`); normalised by length.
- `rankBonus(r)`: if `isWord`, `1 - log(rank)/log(N)` (frequent words are stronger evidence).
- `score(r) = isWord ? 1 + rankBonus : sigmoid(a·ngram + b)` in `[0,1]` for non-words. `a`, `b` fitted by the harness.
A Rendering is "plausible" when `score ≥ gibberishCeiling` (initial 0.35).

### D3. Decision policy (one function, table-driven by Sensitivity)
```
cur = rendering in current layout
if looksLikeCode(word) or containsDeadKey -> Keep
if isWord(cur) -> Keep
candidates = other active layouts where plausible(r)
if none -> Keep
if count(isWord) >= 2 among all renderings -> Keep (Ambiguous)
best = argmax score
if sameScript(cur, best) and !(differsOnSomeKey && isWord(best)) -> Keep
if len(word) <= 2: Aggressive && isShortWord(best) ? Correct : Defer
switch Sensitivity:
  Cautious:   isWord(best) && margin >= 0.30 ? Correct : Defer
  Balanced:   isWord(best) ? Correct : (score(best) >= 0.80 ? Defer : Keep)
  Aggressive: isWord(best) || score(best) >= 0.80 ? Correct : Keep
```
`margin = score(best) - score(cur)`. All numbers are initial values, recorded in one `DetectionThresholds` record per Sensitivity, so tuning touches one place.
- Cautious deferring a common-word match reproduces the trade-off the original describes for its cautious mode: the first word of a sentence gets fixed later.

### D4. Deferred Words
`DeferredRun` holds up to 3 Words with their best target Layout. On the next Word:
- next Decision is Correct to the same Layout → emit one Correction covering the run plus the new Word (single replace action, one Layout Switch);
- otherwise → drop the run (Words stay as typed).
The run is also dropped on focus change, Layout Switch by the user, or a typing gap > 2 s (observed in the original); these signals arrive as events in change `add-auto-correction`.

### D5. looksLikeCode
Digits, any of `@ / \ _ : #`, or lower→upper case change inside the Word (camelCase). Checked on the current Rendering, so `.` and `,` (letters in Russian) are not treated as punctuation.

### D6. Edge punctuation
Before scoring, leading and trailing characters that are not letters of the Rendering's Language are stripped; the Correction still types the full Rendering, so the punctuation follows the target Layout ("ghbdtn?" becomes "привет," because that key is "," in Russian). `looksLikeCode` runs first, so a Word with digits is kept even when they are at its edge.

### D7. Accuracy harness (`tools/Rekey.Accuracy`)
- Inputs: held-out sentences per language (not used for profile generation).
- Positive set: each sentence converted to the keystrokes it would produce in each other Layout ("wrong-layout" text).
- Negative set: sentences typed correctly, plus a list of mixed-language technical text.
- Runs the engine word by word (with deferral), reports false-correction rate, correction rate, Decision latency p50/p99 per Sensitivity, and the 50 worst examples.
- CI runs it on a small sample and fails if Balanced exceeds the spec limits.

## Risks / Trade-offs

- [Thresholds over-fit to subtitle-style text] → keep a separate technical-text negative set; review the worst-50 list on every threshold change.
- [Brand names, slang, transliteration (e.g. "ok", "lol") get corrected] → Protected Words and Learned Words (later change); Ambiguous rule already keeps words valid in both Languages.
- [Russian↔Ukrainian false corrections] → same-script rule requires a differing key and a common-word target; measured separately in the harness.

## Open Questions

- Exact KeyMap set for Ukrainian variants beyond "Ukrainian" and "Ukrainian (Enhanced)" — add fixtures when a user reports a missing variant; does not change design.
