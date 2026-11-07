# Design

## Context

All gates of Auto Correction must agree, and the hook thread already decides synchronously whether to hold a Trigger Key (see `add-text-replacement` D1). The original has the same controls: a main on/off switch, Space/Enter/Tab triggers, excluded apps, a timed pause, skipping words after a manual layout change, and a self-pause when its keyboard monitor misbehaves.

## Goals / Non-Goals

**Goals:**
- One decision point for "does Auto Correction apply now?", in Core, unit-tested.
- Hook-side holding only when correction is possible, so excluded and paused contexts get zero added latency.

**Non-Goals:**
- Tray/Settings UI for these options (`add-tray-and-settings-ui`); this change adds the settings fields and engine behavior.
- Manual Correction (`add-manual-correction`).

## Decisions

### D1. `AutoCorrectionGate` in Core
Pure function `Applies(settings, context, now) -> GateResult` with reasons: `Off`, `Paused`, `SelfPaused`, `ExcludedApp`, `PasswordField`, `Elevated`, `LayoutSwitchedByUser`, `Applies`. The reason feeds statistics ("Decision breakdown") and the tray tooltip. One decision lives in one place.

### D2. Gate snapshot for the hook
The worker publishes an immutable `HookPolicy { bool holdTriggers; TriggerKeySet keys; }` whenever any input to the gate changes (focus, settings, pause timer, self-pause). The hook reads it with `Volatile.Read`. When `holdTriggers` is false, Trigger Keys pass through untouched.

### D3. User Layout Switch detection
The worker knows every Layout Switch it requested (by generation). A `LayoutChanged` event not matching a Rekey request within 500 ms is a user switch → mark the current Word `switchedByUser` and drop Deferred Words. The original also tells its own switches apart from the user's within a short window.

### D4. App identity
`AppId = { ExePath (case-insensitive, normalised), PackageFamilyName? , Aumid? }`. For `ApplicationFrameHost.exe` windows, resolve the hosted app via `GetApplicationUserModelId` on the child core window's process. Excluded Apps match on Aumid when present, else on ExePath.

### D5. Pause
`PausedUntil` (UTC) stored in settings, so it survives restart. A single `PeriodicTimer` on the worker (1 s) re-evaluates the gate; tray shows remaining time. Preset durations (15 min, 30 min, 1 h, 3 h, until restart) are UI, defined in `add-tray-and-settings-ui`; "until restart" stores no `PausedUntil`.

### D6. Defaults (design decisions)
`AutoCorrectionEnabled = true`, `TriggerKeys = {Space, Enter}`, `SkipAfterManualLayoutSwitch = true`, `ExcludedApps = []`. Tab is off by default because it drives autocomplete and focus moves in IDEs and forms.

## Risks / Trade-offs

- [Enter as default trigger in chat apps sends text immediately; a wrong Correction is already sent] → Undo is disabled after Enter (see text-replacement), so false-correction rate matters most; Balanced thresholds are tuned for ≤ 0.5%.
- [Gate snapshot lags focus change by one worker hop (~1 ms)] → a Trigger Key in that window is held unnecessarily and released at once; no behavior change visible.
