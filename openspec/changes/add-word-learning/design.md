# Design

## Context

The original keeps a list of words it never switches and a list of learned words, protects words after undos, refuses a multi-word correction that contains a protected word, and does not protect short words. How it counts undos was not recovered.

## Goals / Non-Goals

**Goals:**
- Learning rules in Core, tested with event sequences.
- Lists that can grow without slowing Decisions.

**Non-Goals:**
- Sync across machines; import/export of lists (possible later).
- UI (`add-tray-and-settings-ui` adds the list editors).

## Decisions

### D1. Storage
`words.json`: `{ version: 1, protected: [{ text, source: user|auto, lastUsed }], learned: [{ text, language, source, lastUsed }] }`. Atomic write like settings, debounced 2 s after changes. Normalised with `WordNormaliser`.

### D2. Matching
- Protected: match on the current Rendering (what the user sees), normalised. Stored per rendering text, not per keystrokes, so the same entry covers the word typed with or without Shift.
- Learned: merged into the scorer's `isWord` for its Language as an extra `HashSet` checked after the profile's frozen set, with rank bonus of a mid-frequency word (rank 5,000).

### D3. Learning from Undo
On `UndoPerformed(word)` with length ≥ 3 → add Protected (source auto). The original protects a word after one ⌘Z undo; it probably also counts undos it infers from the user retyping a word (not traced in code), which we do not detect (design decision), so no counts are stored.

### D4. Bounds
10,000 entries per list; eviction picks the oldest `lastUsed` among `source: auto`. If all entries are user-added, adding a new one beyond the limit is refused with a message in the UI.

## Risks / Trade-offs

- [Auto-protection hides a real wrong-layout word forever] → entries are visible and removable; statistics show "Words protected automatically".
- [Learned Word from a mis-typed manual correction] → Undo removes it (spec).
