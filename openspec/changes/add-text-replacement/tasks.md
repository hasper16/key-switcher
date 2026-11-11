# Tasks

## 1. ISO key in Key Maps

- [x] 1.1 Write failing tests: `KeyMapReader.Read` of the United Kingdom Layout (00000809) has 48 entries and the ISO key (0x56) gives "\" and "|"; every key-map fixture has 48 entries; move the "unknown key" tests to a scan code no Layout lists; add 0x56 to `KeyMapReader.PrintableScanCodes`, regenerate the fixtures with `tools/Rekey.KeyMapCapture` and add a United Kingdom fixture; verify the tests pass

## 2. Correction and Undo model in Core

- [x] 2.1 Define the data shapes: `KeyDown.Held`, `UndoRequested`, `ReplacementFinished`, Key Maps in `LayoutsInstalledChanged`, `ReplaceText` and `UndoPerformed` actions (text hidden from their text form), `WordEvaluator`, Incident types `CorrectionCancelled` and `HoldTimeout`; verify the solution builds and a test shows the actions print no text
- [x] 2.2 Write failing engine tests with a stub Word Evaluator: a held Trigger Key ending a Word to correct yields `ReplaceText(typed, Rendering)`; `ReplacementFinished(applied)` then yields `SwitchLayout(target)`, a cancelled one nothing; a Deferred run replaces the whole run incl. spaces; an unheld or Ctrl-modified trigger, a click or a Backspace past the Word start evaluates or replaces nothing across it; implement; verify they pass
- [x] 2.3 Write failing engine tests for Undo: within 5 s → `ReplaceText(Rendering + " ", typed + " ")`, then `SwitchLayout(source)` + `UndoPerformed`; after 5 s, after another key, after a click, after an Enter-triggered Correction or after a cancelled replacement → no Undo; implement; verify they pass

## 3. Key holding on the hook thread

- [x] 3.1 Write failing tests for `HoldGate` with a fake clock: a Trigger Key after a character key is held, after a non-character or with Ctrl it is not; every key event is held until the worker releases the last held sequence; a key after 150 ms opens the valve and counts a timeout; a full ring never holds; no policy, no hold; implement; verify they pass
- [ ] 3.2 Wire `HoldGate` into the hook callback and the worker (Hook Policy snapshot, held flag in the ring, re-injection after the engine's actions, release, freshness check, `HoldTimeout` Incident); verify with the desktop checks that "ghbdtn" + Space in a test window shows "привет " with the space after the word, and that keys typed during the Correction follow it in order

## 4. Injection

- [ ] 4.1 Write failing tests for the input builder: key-ups for held Ctrl, Alt and Windows keys, n Backspaces, Unicode down/up pairs, the Rekey tag on every input, a held key re-injected by scan code with its extended and key-up flags; implement the `SendInput` injector; verify the tests pass and the desktop checks in Notepad, a browser text box and a console
- [ ] 4.2 Verify with the desktop checks that Enter as Trigger Key in the test page's chat box sends the corrected text, and that the clipboard sequence number is unchanged after Corrections and Undos; verify by hand in VS Code and Telegram Desktop (`docs/manual-tests/add-text-replacement.md`)

## 5. Verified replacement

- [ ] 5.1 Write failing tests for slow-app memory (two timeouts in one app mark it slow, other apps unaffected); implement the UIA reader on the worker (caret range, text before caret, select, 50 ms budget); verify the tests pass and the desktop checks report the verified path in Notepad and the browser text box
- [ ] 5.2 Write failing tests for `VerifiedReplacement.Prepare` with a fake reader (match → select-and-type; mismatch → cancel; nothing exposed or out of time → retype); implement select-and-type and cancel-on-mismatch with the `CorrectionCancelled` Incident; verify the tests pass and the desktop check where the text before the caret differs from the Word (keys the hook ignores) changes nothing; verify by hand with an editor whose autocomplete alters the Word
- [ ] 5.3 Add the `VerifiedReplacement` setting (default on) to `Settings` and pass it to the worker; verify a test that off forces the retype path

## 6. Undo end to end

- [ ] 6.1 Write failing `HoldGate` tests for Ctrl+Z (held as an Undo request only right after the armed Trigger Key; after another key, with Alt, or with nothing armed it passes); implement the hook and worker sides (arm on applied Correction, consume an applied Undo, re-inject a refused one); verify the tests pass and the desktop checks: Correction then Ctrl+Z within 5 s restores "ghbdtn " and English (US); after 6 s, after a letter, or after an Enter-triggered Correction Ctrl+Z reaches the app

## 7. Desktop checks

- [ ] 7.1 Add a `text-replacement` run to `tools/Rekey.DesktopChecks` covering 3.2, 4.1, 4.2, 5.1, 5.2 and 6.1, restoring every setting and closing every window it opens; document it in `docs/manual-tests/add-text-replacement.md` with the checks that need a person; verify the run passes on the owner's desktop
