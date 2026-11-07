# Tasks

## 1. Word lists store (data shape first)

- [ ] 1.1 Define `WordLists` model and `words.json` contract (version 1); verify a round-trip test passes
- [ ] 1.2 Write failing store tests: persist across reload, atomic write, corrupt file → empty lists + `.bak`; implement; verify they pass
- [ ] 1.3 Write failing tests for bounds and LRU eviction of auto entries, refusal when only user entries; implement; verify they pass

## 2. Effect on Decisions

- [ ] 2.1 Write failing engine tests: Protected Word kept; run containing a Protected Word kept; Hotkey still corrects it; implement; verify they pass
- [ ] 2.2 Write failing tests that a Learned Word counts as a common word ("rhby;" → "кринж"); implement scorer integration; verify they pass

## 3. Learning rules

- [ ] 3.1 Write failing tests: one Undo of "ntcn" → Protected (auto) and the next "ntcn" is kept; one Undo of "yt" → not protected; implement; verify they pass
- [ ] 3.2 Write failing tests: Manual Correction to an unknown Rendering → Learned Word; Undo of a Correction to a Learned Word removes it; implement; verify they pass

## 4. Docs

- [ ] 4.1 Describe how learning works for users in `docs/user-guide.md` (section "Teaching Rekey"); verify it matches the spec scenarios
