# Tasks

## 1. Keystrokes and rendering (data shape first)

- [x] 1.1 Define `Keystroke`, `KeyMap`, `LayoutId` records; verify a JSON round-trip test of a KeyMap fixture passes
- [x] 1.2 Capture KeyMap fixtures for English (US), Russian, Ukrainian, Ukrainian (Enhanced) on a Windows machine (small console using `ToUnicodeEx`), commit under `tests/fixtures/keymaps/`; verify each fixture has all 47 printable keys in both shift states
- [x] 1.3 Write failing renderer tests from the spec ("ghbdtn"→"привет", ",tp"→"без", "Ghbdtn"→"Привет", Caps Lock "GHBDTN"→"ПРИВЕТ", dead key → not rendered); implement; verify they pass

## 2. Scoring

- [x] 2.1 Write failing tests for `isWord`, n-gram score and rank bonus on a tiny fixture profile; implement `RenderingScorer`; verify they pass
- [x] 2.2 Add the `Sensitivity` setting (Cautious, Balanced default, Aggressive) to `Settings` and a `DetectionThresholds` record with initial values per Sensitivity; verify the settings defaults/round-trip tests pass and a test that each Sensitivity resolves to its own record

## 3. Decision policy

- [x] 3.1 Write failing tests for Keep rules: valid current word, code-like words ("user@example.com", "getValue"), no plausible rendering ("qzxv"), Ambiguous Word; implement; verify they pass
- [x] 3.2 Write failing tests for Correct rules: "руддщ"→"hello", "ghbdtn"→"привет"; implement; verify they pass
- [x] 3.3 Write failing tests for Sensitivity differences (Cautious defers rare word, Aggressive corrects it, Balanced defers n-gram-only evidence); implement table-driven policy; verify they pass
- [x] 3.4 Write failing tests for short words ("yt" deferred under Balanced, corrected to "не" under Aggressive); implement; verify they pass
- [x] 3.5 Write failing tests for same-script rule ("привыт"→"привіт"; no differing key → keep); implement; verify they pass
- [x] 3.6 Write failing tests for edge punctuation ("ghbdtn!" → "привет!"; "ghbdtn1" kept as code-like); implement; verify they pass
- [x] 3.7 Write failing tests for the rules revised after the first harness run (Ambiguous Words defer, including a valid current word; edge punctuation never makes a Word code-like; split contractions are words; short Renderings are words only through the short-word lists); implement; verify they pass

## 4. Deferred Words

- [x] 4.1 Write failing tests: "yt" + "pyf." → one Correction "не знаю"; deferred + kept next word → nothing; fourth deferral drops the oldest; implement `DeferredRun`; verify they pass
- [x] 4.2 Expose `Reset()` for focus change / user Layout Switch / typing gap, with a test that a reset run produces no Correction; verify it passes
- [x] 4.3 Write failing tests for confirmation by any Layout the deferred Word reads in, typed as it reads there; implement; verify they pass

## 5. Accuracy harness and tuning

- [x] 5.1 Create `tools/Rekey.Accuracy`; collect held-out sentence sets per language and a technical-text negative set under `data/accuracy/` with sources noted; verify the tool runs and prints all metrics
- [x] 5.2 Tune `DetectionThresholds` until Balanced meets ≤ 0.5% false corrections and ≥ 95% corrections; record the final report in `docs/accuracy.md`; verify the report matches a fresh run
- [x] 5.3 Add a latency benchmark test (30-keystroke word, 3 languages, p99 < 5 ms) and a CI step running the harness on a sample with spec limits; verify CI fails when a threshold is deliberately broken and passes when restored
