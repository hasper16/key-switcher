# Proposal

## Why

No word list covers every name, brand, slang word or abbreviation a user types. Without a way to teach Rekey, the same wrong Correction repeats and users turn the app off. The original keeps a list of words it never switches, adds to it automatically after an Undo, and keeps learned words.

## What Changes

- Protected Words: words Rekey never auto-corrects; the user adds and removes them.
- Automatic protection: after an Undo of a Correction, the Word becomes a Protected Word (as in the original).
- Learned Words: when the user manually corrects a Word, its new Rendering is learned as a valid word of the target Language, so it is recognised next time.
- Both lists persist per user and are editable in settings.

## Capabilities

### New Capabilities

- `word-learning`: How Rekey learns from the user which words to leave alone and which words are valid, and how those lists affect Corrections.

### Modified Capabilities

None.

## Impact

- Core: word lists store, learning rules, scorer input extended with Protected and Learned Words.
- Storage: `%LOCALAPPDATA%\Rekey\words.json` (separate from settings so lists can grow without rewriting settings).
- Depends on `add-language-scorer`, `add-text-replacement` (Undo events), `add-manual-correction`.
