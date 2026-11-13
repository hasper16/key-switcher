# Design

## Context

All gates of Auto Correction must agree, and the hook thread already decides synchronously whether to hold a Trigger Key (see `add-text-replacement` D1). The original has the same controls: a main on/off switch, Space/Enter/Tab triggers, excluded apps, a timed pause, skipping words after a manual layout change, and a self-pause when its keyboard monitor misbehaves.

## Goals / Non-Goals

**Goals:**
- One decision point for "does Auto Correction apply now?", in Core, unit-tested.
- Hook-side holding only when correction is possible, so excluded and paused contexts get zero added latency.

**Non-Goals:**
- Tray/Settings UI for these options (`add-tray-and-settings-ui`); this change adds the settings fields and engine behavior. Until then the options are changed in the settings file while Rekey is not running.
- Manual Correction (`add-manual-correction`).

## Decisions

### D1. `AutoCorrectionGate` in Core
Pure function `AutoCorrectionGate.Decide(settings, focus, selfPaused, layoutSwitchedByUser, now) -> GateResult` with reasons, checked in this order: `Off`, `Paused`, `SelfPaused`, `ExcludedApp`, `PasswordField`, `Elevated`, `LayoutSwitchedByUser`, `Applies`. An unknown focus counts as `PasswordField`, as it does for keeping Keystrokes. The reason feeds statistics ("Decision breakdown") and the tray tooltip. One decision lives in one place: the engine asks the gate for every Trigger Key and exposes the current answer as `Engine.Gate`; the worker derives the Hook Policy from it (D2).

The engine reads the wall clock for `now` from the `TimeProvider` it is given, so a Pause ends without any event; tests pass a clock they move by hand. Settings reach the engine at construction and through a `SettingsChanged` input event; a self-pause through `SelfPauseChanged`.

### D2. Gate snapshot for the hook
The worker publishes an immutable `HookPolicy` (the Trigger Keys to hold and the character scan codes of the active Layouts) whenever the Trigger Keys to hold change: after every input event the engine handled and on every pass of its loop (at most 250 ms apart, which covers the end of a Pause). The Trigger Keys to hold are the configured ones while `Engine.Gate` is `Applies`, and none otherwise. The hook reads the snapshot with `Volatile.Read`. With no Trigger Key to hold, Space, Enter and Tab pass through untouched. The character scan codes are always published, so the hook's "last key was a character" is already right for the first Word after the gate opens.

The hook's snapshot can lag; the engine asks the gate again for each held Trigger Key, so a key held on a stale snapshot is released at once without evaluation.

### D3. User Layout Switch detection
The worker (its thread) tells the engine why the Layout changed: `LayoutChanged.ByUser`. A change counts as the user's when the worker sees the Layout of the same focused window differ after input or on a loop pass. It is not the user's when Rekey requested it (while a Layout Switch is pending, up to its 500 ms deadline, the worker does not follow the foreground Layout; the landed or adopted Layout is reported as Rekey's own), when it follows a focus report (another window may keep its own Layout), when the focused window is no longer the reported one (its focus report is on the way), or when the list of installed Layouts changed. The original also tells its own switches apart from the user's within a short window.

Engine: a user switch marks the current Word and drops the Deferred Words. The mark lives with the Word and goes when the Word ends (Trigger Key, Space, Enter, Tab, caret keys, shortcuts, click, focus change, typing gap). The Word may still be empty when the user switches, as after Win+Space, which ends the Word before it; the mark then covers the Word typed next, unless a pause of more than 2 seconds after the switch or another Word end comes first. A switch that comes more than 2 seconds after the last key ends the Word typed before it, as the typing gap would at the next key. With `SkipAfterManualLayoutSwitch` on, the gate answers `LayoutSwitchedByUser` for a marked Word.

### D4. App identity
`AppId = { ExePath, Aumid? }` in Core. Win32 side, on the worker thread at each focus report: `QueryFullProcessImageName` for the path and `GetApplicationUserModelId` for the application user model id of a packaged app (absent for unpackaged ones). For a window of `ApplicationFrameHost.exe`, the hosted app is the process of the frame's child window of class `Windows.UI.Core.CoreWindow` (`FindWindowEx`, `GetWindowThreadProcessId`); when none is found the frame host itself is reported. Excluded Apps match on the Aumid when the app has one, else on the path. Paths compare without case, with `/` as `\`, without surrounding quotes or the `\\?\` prefix, and with `.` and `..` segments resolved. A package family name is not kept: the Aumid already contains it and tells apart the apps of one package.

`Settings.ExcludedApps` is an `ExcludedAppList`, a set of such identities written to the settings file as an array of strings.

### D5. Pause
`PausedUntil` (UTC) stored in settings, so it survives restart; the gate compares it with the wall clock. The worker's loop already wakes at least every 250 ms and republishes the Hook Policy then (D2), so no separate timer is needed. Preset durations (15 min, 30 min, 1 h, 3 h, until restart) and the tray's remaining time are UI, defined in `add-tray-and-settings-ui`; "until restart" stores no `PausedUntil`.

### D6. Defaults (design decisions)
`AutoCorrectionEnabled = true`, `TriggerKeys = {Space, Enter}`, `SkipAfterManualLayoutSwitch = true`, `ExcludedApps = []`. Tab is off by default because it drives autocomplete and focus moves in IDEs and forms.

### D7. The Word Evaluator
The engine evaluates a Word with a `WrongLayoutDetector` it builds from the installed Layouts whose Language has a loaded profile (`LayoutsInstalledChanged`), at the Sensitivity in settings. The `WordEvaluator` seam of `add-text-replacement` stays as an optional override, used by tests and by the desktop checks, which correct only the spec's example Words.

### D8. Words lost to unconfirmed keys
`add-keyboard-capture-and-layouts` D3 asked this change to measure how often a Word gets no evaluation because one of its keys could not be confirmed outside a Password Field. The engine counts, for Words ended by a configured Trigger Key while the gate applies: Words ended, Words evaluated, and Words dropped for an unconfirmed key (`Engine.WordCounts`, counters only). The desktop checks type Words at several speeds and print the counters.

## Risks / Trade-offs

- [Enter as default trigger in chat apps sends text immediately; a wrong Correction is already sent] → Undo is disabled after Enter (see text-replacement), so false-correction rate matters most; Balanced thresholds are tuned for ≤ 0.5%.
- [Gate snapshot lags focus change by one worker hop (~1 ms)] → a Trigger Key in that window is held unnecessarily and released at once; no behavior change visible.
