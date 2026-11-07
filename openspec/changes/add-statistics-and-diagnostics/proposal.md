# Proposal

## Why

Users and the developer need to see whether Rekey helps or gets in the way: how many Words it checked, how many it corrected, how many Corrections were undone, and what went wrong when it misbehaves. The original has a statistics window, a statistics log, an incident log, a local word-analysis log, and a diagnostics export. Because Rekey observes every keystroke, these features need strict privacy rules.

## What Changes

- Counters: Words analysed, Corrections (auto and manual), Undos, Decision breakdown, replacement duration (min/median/max/average).
- Statistics window opened from the tray.
- Incident log: bounded local record of keyboard failures, recoveries, cancelled Corrections, failed Layout Switches.
- Word-analysis log: optional, off by default, local only, short retention; records the Words and Decisions for troubleshooting.
- Export diagnostics to the clipboard or a file, with a preview of exactly what is exported; never includes typed text unless the user explicitly includes the word-analysis log.

## Capabilities

### New Capabilities

- `statistics-and-diagnostics`: What Rekey counts and logs locally, how users view it, and how diagnostics are exported without exposing typed text.

### Modified Capabilities

None.

## Impact

- Core: counters aggregator; persistence of the `Incident` records defined in `bootstrap-solution`.
- App: statistics window, diagnostics export dialog.
- Storage: `%LOCALAPPDATA%\Rekey\stats.json`, `incidents.jsonl`, `analysis.log` (optional).
- Settings: `StatisticsEnabled` (on), `AnalysisLogEnabled` (off).
- Depends on `add-auto-correction` and `add-tray-and-settings-ui`.
