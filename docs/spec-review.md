# Spec review — 2026-11-07

Review of the 14 OpenSpec changes in `openspec/changes/` against: coverage of the original app's features (from the owner's reverse-engineering notes, kept outside the repository), consistency across changes and with `GLOSSARY.md` and `docs/roadmap.md`, conformance with the code rules in `AGENTS.md` (Google C# style, commenting rule, XML docs, analyzers, ub-mode/TDD task order), testability, and spec purity. Result: `openspec validate --all --strict` passes (14/14).

## Findings and fixes

| # | Area | Change(s) | Finding | Fix |
|---|---|---|---|---|
| 1 | Coverage | keyboard-capture-and-layouts | The original restarts itself after a repeated freeze (at most once per 10 min); no requirement covered it. | Added requirement "Restart after a repeated freeze", design D7 (freeze watchdog, self-restart), tasks 5.4–5.5. |
| 2 | Coverage | keyboard-capture-and-layouts | The original lets users turn its watchdogs off; ours had no statement. | Owner chose toggles (Q2): settings `KeyboardWatchdogEnabled`, `RestartAfterFreezeEnabled`. |
| 3 | Coverage | manual-correction | Conflict check against hotkeys of other apps had no scenario. | Added scenario "Taken by another app"; task 4.2 verifies it. |
| 4 | Consistency | language-scorer | `Sensitivity` setting was never added to `Settings` by any change. | Task 2.2 adds it (Balanced default); proposal lists it. |
| 5 | Consistency | text-replacement | `VerifiedReplacement` setting added in tasks but missing from proposal Impact. | Added to proposal. |
| 6 | Consistency | telemetry | Proposal listed a `LastSentReports` setting no task creates; design stores sent payloads in `sent.jsonl`. | Proposal now names the `sent.jsonl` file instead. |
| 7 | Consistency | auto-update, bootstrap, onboarding | User data was planned inside the Velopack install root, which Velopack deletes on uninstall (Velopack docs, "Uninstalling"), so "data is kept if the user declines" could not hold; a migration task moved data that was never released. | Install root `%LOCALAPPDATA%\key-switcher` (pack id `key-switcher`); user data stays in `%LOCALAPPDATA%\Rekey` from bootstrap on. Migration task replaced by a path-resolver test; added scenario "Uninstall keeping data". |
| 8 | Consistency | onboarding, roadmap | Onboarding's Install option and installed-location check need the Velopack install root, but onboarding came before the installer. | Roadmap reordered: 10 statistics, 11 auto-update, 12 onboarding, 13 telemetry. |
| 9 | Consistency | ui-localization | Proposal had no "Depends on" line. | Added (9–13 as in roadmap). |
| 10 | Consistency | tray, statistics | "Depends on all engine changes" did not match the roadmap. | Named the exact changes. |
| 11 | Consistency | language-profiles, keyboard-capture, text-replacement, auto-update | Incidents are emitted from change 2 on, but the Incident model arrived only in statistics. | `Incident` record and `RecordIncident` action defined in bootstrap D2 / task 2.1; statistics only persists them. |
| 12 | Consistency | GLOSSARY, word-learning | Glossary: Protected Word is "never corrected", spec: Manual Correction corrects it. | Glossary: "never auto-corrects". |
| 13 | Consistency | GLOSSARY, manual-correction, text-replacement | Hotkey revert had no Undo Window and ignored the Enter rule, unlike Ctrl+Z; glossary Undo named only Ctrl+Z. | Hotkey revert follows Undo conditions; new scenario for Enter-triggered Correction; glossary Undo covers Ctrl+Z or Hotkey. |
| 14 | Consistency | keyboard-capture, auto-correction, language-scorer | Word boundaries were undefined for digits/punctuation and for Space/Enter/Tab that are not Trigger Keys; the scorer's code-like rule ("user@example.com") needs them inside a Word; an auto-correction scenario used the comma key, which is "б" in Russian. | Printable keys extend the Word; Space/Enter/Tab end it and only Trigger Keys evaluate (split into two requirements). New scorer requirement "Punctuation at the edges of a Word". Auto-correction scenario uses Tab when it is not a Trigger Key; added "Defaults" scenario. |
| 15 | Consistency | manual-correction | "Correct the last Word" referred to "the Trigger Key" although Manual Correction works when Space is not a trigger. | "at most one Space, Enter or Tab". |
| 16 | Consistency | manual-correction, tray | Conflict example Ctrl+Shift is modifier-only, but a custom Hotkey is "modifiers and one key". | Example changed to Win+Space; design D5 explains which Windows shortcuts can collide. |
| 17 | Testability | ten changes | Numbers not observed in the original were not marked (2 s, 5 s, 500 ms, 10 min, 1 s, 30 days, 500, 7 days, 24 h, 10 requests, length 3, pause presets, defaults). | Marked "(design decision)"; observed values (5 s Undo, 2 s gap, 10 min restart limit) marked "as in the original". |
| 18 | Testability | statistics-and-diagnostics | The clipboard exception (diagnostics Copy) was only in design. | Added scenario "Copy to clipboard". |
| 19 | Testability | tray | Launch at sign-in reflecting Task Manager and "no sound on Undo" were design-only. | Added to spec. |
| 20 | Testability | auto-update | No scenario for keeping data on uninstall. | Added "Uninstall keeping data". |
| 21 | TDD order | keyboard-capture 4.1, 4.4; text-replacement 4.2, 5.1; auto-correction 3.2; manual-correction 1.3; auto-update 2.2 | Logic was verified only manually. | Each task now starts with a failing test against fakes, then manual verification. |
| 22 | Spec purity | app-lifecycle | Spec named the file format ("JSON file"). | "a file", "cannot be parsed"; JSON stays in design D4. |
| 23 | Terms | manual-correction, auto-correction | "Toggle back" / "main toggle" (glossary avoids "toggle"). | "Hotkey reverts" / "main on/off switch". |
| 24 | Duplication | telemetry design | Telemetry ran its own engine-freeze watchdog, duplicating the restart watchdog. | Telemetry reuses keyboard-capture D7 for engine freezes and adds only the UI ping. |
| 25 | Coverage | manual-correction | The original's Space-based Hotkey options were not understood. | Decompiling showed they let users keep their habitual input-source shortcut; added Ctrl+Space and Win+Space (Win+Space takes over the Windows Layout Switch shortcut), design D0, task 1.4. The Fn/Globe option has no Windows counterpart. |
| 26 | Coverage | keyboard-capture, auto-correction | Decompiling showed Return, keypad Enter and Tab always end a Word in the original. | Specs say Enter includes the numeric keypad Enter. |
| 27 | Consistency | word-learning | The original protects a Word after one Undo; our spec guessed two. | One Undo protects a Word of 3+ letters; per-word undo counts dropped. |

## Checked, no change needed

- Trigger Key defaults (Space + Enter on, Tab off) agree in auto-correction spec/design and the settings mock-up.
- Undo Window is 5 s everywhere; Undo after an Enter-triggered Correction is disabled in text-replacement and manual-correction.
- Excluded Apps vs Manual Correction: the Hotkey still works in Excluded Apps; Rekey's own process is never excluded (onboarding D1).
- Clipboard: Corrections and Undo never touch it; the only write is the explicit diagnostics Copy.
- Every settings field is introduced by exactly one change.
- Specs contain no class names or library choices; those stay in design.md.
- No task asks for comments on every member, XML docs on non-public members, or anything the analyzers or Google style forbid.

## Left as is

- Cautious Sensitivity: spec says "corrects only on a common-word match", design adds a score margin tuned by the harness; no conflict.
- The language-profiles scenario "Layout added while running" can only be checked end to end after `add-keyboard-capture-and-layouts`; its task 4.1 verifies it.
- Where the original checks its word-ending characters and whether its trigger settings gate them: not traced (stripped binary). Our rule: punctuation stays inside the Word and is ignored in scoring (owner accepted).

## Owner decisions — 2026-11-07

| Topic | Decision |
|---|---|
| Original's Space-based Hotkey options | Decompile and port (rows 25–26). |
| Watchdog toggles | Add them (row 2). |
| Punctuation inside a Word | Accepted as designed. |
| Install folder | `%LOCALAPPDATA%\key-switcher` ("key-switcher" is the correct spelling). |
| Release host | GitHub Releases of this repository (auto-update design D3, D4). |
| Source code | Public, licence GPL-3.0-or-later (`LICENSE`). CC BY-SA 4.0 data is one-way compatible with GPLv3, so generated Language Profiles can ship inside the app. |
| Reverse-engineering notes | Kept outside the repository; specs describe the original's behavior in our own words only. |

## Still open

- Telemetry services (recommended: Sentry EU for crashes, Aptabase EU for the digest and setup signal, GitHub Watch/Issues instead of email sign-up) — owner deferred.
- Code-signing certificate (recommended for an open-source individual: Certum Open Source Code Signing in the cloud) — owner deferred.
