# Tasks

## 1. Word tracking in Core (data shape and rules first)

- [x] 1.1 Extend `InputEvent` with `KeyDown`, `MouseClick`, `FocusChanged(appId, isPassword, isElevated)`, `LayoutChanged`, `LayoutsInstalledChanged`; verify the project builds and existing tests pass
- [x] 1.2 Write failing engine tests for Word boundaries (letters, Backspace, arrows/Home/End/Delete/Escape, Ctrl/Alt/Win combos, mouse click, focus change); implement `WordTracker`; verify they pass
- [x] 1.3 Write failing tests: gap > 2 s ends the Word and drops Deferred Words; implement; verify they pass
- [x] 1.4 Write failing tests: Password Field focus → no Keystrokes kept, no Correction; elevated foreground → no Correction; implement; verify they pass

## 2. Hook thread and event pipe

- [ ] 2.1 Implement the hook thread with `WH_KEYBOARD_LL` + `WH_MOUSE_LL`, message loop, ring buffer, injected-input filter by `dwExtraInfo` tag; verify with a manual test app that typed keys appear in a debug log and Rekey-tagged `SendInput` keys do not
- [x] 2.2 Implement the worker that converts raw events to `InputEvent` using KeyMaps; verify a `Rekey.Win32.Tests` test that synthetic raw events produce the expected `InputEvent` sequence
- [x] 2.3 Add callback duration measurement and the aggregated slow-callback Incident; verify by a test with an injected clock

## 3. Focus, password and elevation

- [ ] 3.1 Implement WinEvent foreground/focus hooks, app id, elevation check; verify manually that switching between Notepad and an elevated cmd logs the right `FocusChanged` values
- [ ] 3.2 Implement Password Field detection via UI Automation `IsPassword` with `ES_PASSWORD` fallback; verify manually in a browser login form and a Win32 password dialog

## 4. Layouts

- [ ] 4.1 Write failing tests for HKL→KLID mapping with a fake registry reader (Preload, Substitutes, variant layouts); implement the Layout list with 2 s change polling; verify the tests pass, and manually that adding a Ukrainian Layout in Windows Settings shows up within 5 s and makes Ukrainian an active Language without restart
- [x] 4.2 Implement KeyMap building with `ToUnicodeEx` (flag 0x4); verify a test that compares built maps with the checked-in fixtures from `add-language-scorer`
- [ ] 4.3 Implement current-Layout tracking for the foreground thread (incl. console via `GetGUIThreadInfo`); verify manually in global and per-window Layout modes
- [ ] 4.4 Write failing tests for switch verification with a fake clock and fake layout reader (lands → adopted; not landed after 500 ms → real Layout adopted + Incident); implement verified Layout Switch with `WM_INPUTLANGCHANGEREQUEST`; verify the tests pass and manually in Notepad, a browser and Windows Terminal
- [x] 4.5 Load the Language Profiles with `LanguageSet.LoadBundled` on the worker at start-up, record its Incidents, and recompute active Languages with `LanguageSet.Activate` on every `LayoutsInstalledChanged`; verify a test with a fake Layout list that only English and Russian are active with English (US) and Russian Layouts installed, and that Ukrainian becomes active after a Ukrainian Layout is added (spec `language-profiles`)

## 5. Watchdog

- [x] 5.1 Add `KeyboardWatchdogEnabled` and `RestartAfterFreezeEnabled` (both on) to `Settings`; verify the settings defaults and round-trip tests pass
- [x] 5.2 Write failing tests (fake clock and fake input-info source): system input newer than hook input by > 1.5 s → reinstall + Incident; two reinstalls in 10 min → SelfPause; manual Resume clears; watchdog off → no checks; implement the watchdog logic; verify they pass
- [ ] 5.3 Wire the watchdog to the real hook thread; verify manually by blocking the callback with a debugger long enough for Windows to drop the hook and observing automatic reinstall
- [x] 5.4 Write failing tests for the freeze watchdog with a fake clock (stalled heartbeat with queued input > 5 s = freeze; second freeze within 1 h → restart request; no second restart within 10 min; restart off → Incident only); implement; verify they pass
- [ ] 5.5 Implement self-restart (`--restarted-after-freeze <pid>`, wait for old PID, restart-time state file); verify manually by freezing the worker with a debug command twice and seeing one restart and one Incident

## 6. Docs

- [ ] 6.1 Document threads, event flow and Incident types in `docs/architecture.md`; verify every Incident type emitted in code is listed
