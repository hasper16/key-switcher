# Tasks

## 1. Settings and gate (data shape first)

- [ ] 1.1 Add `AutoCorrectionEnabled`, `TriggerKeys`, `ExcludedApps`, `PausedUntil`, `SkipAfterManualLayoutSwitch` to `Settings` with defaults; verify the settings round-trip and defaults tests pass
- [ ] 1.2 Write failing tests for `AutoCorrectionGate` covering every reason (Off, Paused, SelfPaused, ExcludedApp, PasswordField, Elevated, LayoutSwitchedByUser, Applies); implement; verify they pass

## 2. Engine pipeline

- [ ] 2.1 Write failing engine tests: Trigger Key + wrong-layout Word → Correction actions; non-trigger key → nothing; disabled trigger → nothing; implement the pipeline using the scorer and text-replacement actions; verify they pass
- [ ] 2.2 Write failing tests for user Layout Switch detection (unmatched `LayoutChanged` marks the Word and drops Deferred Words; Rekey's own switch does not); implement; verify they pass
- [ ] 2.3 Write failing tests that the pause expires on its own (fake clock) and survives a settings reload; implement; verify they pass

## 3. Win32 wiring

- [ ] 3.1 Publish `HookPolicy` snapshots from the worker and read them in the hook; verify manually that in an Excluded App Space is never held (no added latency in a debug timing log)
- [ ] 3.2 Write failing tests for `AppId` matching (path case and form normalisation; Aumid wins over the frame host path); implement `AppId` resolution incl. packaged apps via `GetApplicationUserModelId`; verify the tests pass and manually that excluding the Store "Notepad" or "Calculator" app does not affect other apps

## 4. End-to-end check

- [ ] 4.1 Run the manual checklist from the spec scenarios (Space, Enter, Tab on/off, Excluded App, Pause 1 min, restart during pause, switch Layout mid-word) and record results in `docs/manual-tests/auto-correction.md`; verify every scenario is marked pass
