# Tasks

## 1. ISO key in Key Maps

- [x] 1.1 Write failing tests: `KeyMapReader.Read` of the United Kingdom Layout (00000809) has 48 entries and the ISO key (0x56) gives "\" and "|"; every key-map fixture has 48 entries; move the "unknown key" tests to a scan code no Layout lists; add 0x56 to `KeyMapReader.PrintableScanCodes`, regenerate the fixtures with `tools/Rekey.KeyMapCapture` and add a United Kingdom fixture; verify the tests pass

## 2. Correction and Undo model in Core

- [ ] 2.1 Define `ReplaceWord`, `ReleaseHeld`, `LastCorrection`, `UndoPerformed` actions/records; verify the project builds
- [ ] 2.2 Write failing engine tests: a Correction yields `ReplaceWord(erase n, text)` + `SwitchLayout` + `ReleaseHeld` in that order; a deferred run erases the whole run incl. spaces; implement; verify they pass
- [ ] 2.3 Write failing engine tests for Undo: within 5 s → restore actions + `UndoPerformed`; after 5 s, after another key, or after an Enter-triggered Correction → no Undo; implement; verify they pass

## 3. Key holding on the hook thread

- [ ] 3.1 Write failing tests for the hold state machine (hook-local `lastWasLetter`, `holdGeneration`/`releasedGeneration`, 150 ms valve) with a fake clock; implement; verify they pass
- [ ] 3.2 Wire the hold logic into the hook callback with the Trigger Key snapshot; verify manually that "ghbdtn" + Space in Notepad shows "привет " with the space after the word

## 4. Injection

- [ ] 4.1 Implement the `SendInput` injector (Backspaces, Unicode text, held VK keys, Rekey tag) in a single call; verify manually in Notepad, Chrome, VS Code and Windows Terminal
- [ ] 4.2 Verify manually that Enter as Trigger Key in a chat box (e.g. Telegram Desktop) sends the corrected text, and that the clipboard is unchanged after Corrections and Undos

## 5. Verified replacement

- [ ] 5.1 Implement the UIA reader on an STA helper thread (caret range, text before caret, 50 ms timeout, slow-app memory); verify a manual check in Notepad and Chrome logs the expected text before caret
- [ ] 5.2 Write failing tests for the verification decision with a fake text reader (match → select-and-type; mismatch → cancel + Incident; no text exposed or timeout → retype); implement select-and-type and cancel-on-mismatch; verify the tests pass and manually with an editor whose autocomplete alters the word (no change made)
- [ ] 5.3 Add the `VerifiedReplacement` setting (default on) to `Settings`; verify a test that off forces retype path

## 6. Undo end to end

- [ ] 6.1 Write failing tests for the hook-side Ctrl+Z state machine with a fake clock (`correctionGeneration` unchanged, other key in between, Enter-triggered, older than 5 s → pass through; otherwise swallow); implement; verify the tests pass and manually: Correction then Ctrl+Z within 5 s restores "ghbdtn " and English (US); after 6 s Ctrl+Z reaches the app
