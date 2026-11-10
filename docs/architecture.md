# Architecture

How Rekey sees typing and Layouts at run time. Decisions behind it: ADR 0002 (the engine returns actions) and the design of change `add-keyboard-capture-and-layouts`.

## Threads

| Thread | Owner | Runs | Writes (sole writer) |
|---|---|---|---|
| UI | `Rekey.App.App` | WPF, tray icon, settings | settings snapshot, tray state |
| Hook (`Rekey hook`, above normal) | `InputHook` | `WH_KEYBOARD_LL` and `WH_MOUSE_LL` callbacks, a `GetMessage` loop | ring tail, last-input tick, slow-callback count |
| Worker (`Rekey worker`) | `InputWorker` | the `Engine`, input translation, WinEvent focus hooks, Layout list and switching, hook watchdog | ring head, heartbeat, everything the engine holds |
| Freeze guard (`Rekey freeze guard`) | `FreezeGuard` | checks the worker every 500 ms | its own freeze state |

Every value shared between threads has one writer. The hook callbacks copy, signal and return: no allocation, no locks, no logging. Other threads talk to the worker by posting thread messages (`SwitchLayout`, `ResumeSelfPause`, `FreezeForCheck`).

## Event flow

```
WH_KEYBOARD_LL / WH_MOUSE_LL ──copy──▶ InputRing ──signal──▶ worker
  (skips keys tagged InjectedInput.Tag)                       │
                                                              ▼
WinEvent foreground/focus ──▶ FocusTracker ──▶ FocusChanged ─▶ Engine.Handle ──▶ EngineActions
LayoutList poll (2 s) ──────────────────────▶ LayoutsInstalledChanged ─▶        (RecordIncident,
Foreground thread Layout (after input/focus) ▶ LayoutChanged ─────────▶         SwitchLayout)
RawInputTranslator: RawInput ──────────────▶ KeyDown / MouseClick ────▶
```

- `RawInputTranslator` tracks Shift, Ctrl, Alt, Win, AltGr and Caps Lock from key-downs and key-ups, and marks a key as a character key when any installed Layout's Key Map types text with it.
- The `Engine` keeps the current Word (`WordTracker`), the Deferred Words, the focused control, the current Layout and the active Languages. In a Password Field, an elevated window, or while focus is unknown (before the first `FocusChanged`, after `FocusLost`) it keeps no Keystrokes. A control counts as a Password Field when its Edit style says so, when UI Automation says so, or when UI Automation cannot answer (unavailable, failed or timed out after 200 ms).
- Focus events carry the time Windows stamped on them. The engine drops any key stamped at or before the latest focus change, so keys that wait in the ring while focus moves on are never kept under the newer focus; after a dropped key it keeps nothing more until that Word ends, so no Word misses a Keystroke. If a focus hook cannot be installed, the tracker reports only `FocusLost`. The worker also dispatches waiting messages before each key it reads, so a focus change usually reaches the engine before the keys typed after it.
- `LayoutSwitcher` posts `WM_INPUTLANGCHANGEREQUEST` to the focused window and polls its Layout every 25 ms for up to 500 ms.
- The worker waits with `MsgWaitForMultipleObjectsEx` on the hook signal and its own queue, at most 250 ms (25 ms while a switch is pending), then runs its timed checks: slow-callback report, switch check, Layout poll, hook watchdog.

## Incident types

Every `IncidentType` value, the component that records it, and the design name.

| `IncidentType` | Recorded by | Design name | When |
|---|---|---|---|
| `LanguageProfileSkipped` | worker start-up (`LanguageSet.LoadBundled`) | — | a bundled Language Profile failed to load |
| `SlowHookCallback` | `SlowCallbackMeter` | slow callback | a hook callback took over 1 ms; at most one per 10 minutes |
| `SwitchNotLanded` | `LayoutSwitcher` | `switch-not-landed` | a requested Layout Switch did not land within 500 ms |
| `HookReinstalled` | `HookWatchdog` | `hook-reinstalled` | the session saw input the hooks missed for over 1.5 s |
| `SelfPaused` | `HookWatchdog` | self-pause | two hook failures within 10 minutes |
| `InputFrozen` | `FreezeWatchdog` | freeze | the worker stalled over 5 s with input waiting, without a restart |
| `FreezeRestart` | `FreezeWatchdog` | `freeze-restart` | a second freeze within an hour restarted Rekey |

Incidents carry enum details only, so typed text cannot reach them.
