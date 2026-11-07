# Proposal

## Why

Keyboard capture, the Decision logic and text replacement exist as separate pieces. Users need them joined into Auto Correction: when a Trigger Key ends a wrong-layout Word, Rekey corrects it, unless the user is somewhere Rekey should stay out of. The original exposes this as the main on/off switch plus Trigger Keys, Excluded Apps, Pause, and a setting that skips words after a manual layout change.

## What Changes

- Auto Correction on by default, with a global on/off setting.
- Configurable Trigger Keys: Space, Enter, Tab.
- Excluded Apps: Auto Correction is off in listed applications.
- Pause: Auto Correction off until a chosen time; resumes on its own.
- "Don't correct after I switch Layout": a Word is not corrected when the user switched Layout while typing it.
- One place (the engine) decides whether Auto Correction applies, combining these gates with Password Fields, elevated windows and self-pause.

## Capabilities

### New Capabilities

- `auto-correction`: When Rekey corrects Words on its own, which keys trigger it, and where and when it stays off.

### Modified Capabilities

None.

## Impact

- Core: gate logic in the engine, new `Settings` fields (`AutoCorrectionEnabled`, `TriggerKeys`, `ExcludedApps`, `PausedUntil`, `SkipAfterManualLayoutSwitch`).
- Win32: app identity for packaged (Store) apps.
- Depends on `add-language-scorer`, `add-keyboard-capture-and-layouts`, `add-text-replacement`.
