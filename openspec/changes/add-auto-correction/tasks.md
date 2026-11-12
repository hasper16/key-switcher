# Tasks

## 1. Settings and gate (data shape first)

- [x] 1.1 Add `AutoCorrectionEnabled`, `TriggerKeys`, `ExcludedApps`, `PausedUntil`, `SkipAfterManualLayoutSwitch` to `Settings` with defaults; verify the settings round-trip and defaults tests pass
- [x] 1.2 Write failing tests for `AutoCorrectionGate` covering every reason (Off, Paused, SelfPaused, ExcludedApp, PasswordField, Elevated, LayoutSwitchedByUser, Applies); implement; verify they pass

## 2. Engine pipeline

- [ ] 2.1 Write failing engine tests: Trigger Key + wrong-layout Word → Correction actions; non-trigger key → nothing; disabled trigger → nothing; every gate reason → nothing; implement the pipeline using the scorer (design D7) and text-replacement actions; verify they pass
- [ ] 2.2 Write failing tests for user Layout Switch detection (a `LayoutChanged` by the user marks the Word and drops Deferred Words; Rekey's own switch does not); implement; verify they pass
- [ ] 2.3 Write failing tests that the pause expires on its own (fake clock) and survives a settings reload; implement; verify they pass
- [ ] 2.4 Write failing tests for the Word counters of design D8 (ended, evaluated, dropped for an unconfirmed key); implement; verify they pass

## 3. Win32 wiring

- [ ] 3.1 Write failing tests for the Trigger Keys the hook holds (none unless the gate applies; the configured ones otherwise); publish `HookPolicy` snapshots from the worker and read them in the hook; verify the tests pass and, with `tools/Rekey.DesktopChecks -- auto-correction`, that in an Excluded App Space is never held
- [ ] 3.2 Write failing tests for `AppId` matching (path case and form normalisation; Aumid wins over the frame host path); implement `AppId` resolution incl. packaged apps via `GetApplicationUserModelId`; verify the tests pass and, with `tools/Rekey.DesktopChecks -- auto-correction`, that excluding the Store "Calculator" app turns Auto Correction off there and not elsewhere
- [ ] 3.3 Report the cause of each Layout change and Rekey's self-pause to the engine, and let the app correct with the engine's own evaluator; verify with `tools/Rekey.DesktopChecks -- auto-correction` that a Word typed after the Windows Layout Switch shortcut is left alone and Rekey's own switch is not taken for the user's

## 4. End-to-end check

- [ ] 4.1 Add the `auto-correction` subset to `tools/Rekey.DesktopChecks` covering the spec scenarios (Space, Enter, numeric keypad Enter, Tab on/off, turned off, Excluded App, Pause that ends, switch Layout mid-word, self-pause) and the counters of design D8 at several typing speeds; verify every line passes on the owner's desktop and record the run in `docs/manual-tests/add-auto-correction.md`
- [ ] 4.2 Run the checklist by hand in `docs/manual-tests/add-auto-correction.md` (the app itself: default Corrections, an Excluded App and a Pause set in the settings file, restart during the Pause); verify every item is ticked
