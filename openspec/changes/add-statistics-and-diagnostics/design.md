# Design

## Context

The original has a statistics window (words analysed, switches, reverted switches, other undos, a decision breakdown, replacement durations), a statistics log, an incident log, a list of recent word analyses, and a diagnostics export to the clipboard or a file. In its pre-release build the word-analysis log is on by default; we make it opt-in because it is effectively a record of typed text.

## Goals / Non-Goals

**Goals:**
- Useful numbers with zero text in default storage.
- A diagnostics export users can read before sharing.

**Non-Goals:**
- Sending anything over the network (`add-telemetry`).

## Decisions

### D1. Trust levels of local data
| Data | Contains text? | Default | Leaves the machine? |
|---|---|---|---|
| Counters (`stats.json`) | No | On | Only as Usage Digest, if user opts in |
| Incidents (`incidents.jsonl`) | No (types, codes, app exe names) | On | Only in a user-confirmed export |
| Word-analysis log (`analysis.log`) | **Yes** | Off | Only if the user includes it in an export |
| Settings, word lists | Protected/Learned Words | — | Never in exports by default |

### D2. Counters
Engine emits `RecordStat` actions; an aggregator on the worker keeps today's bucket in memory and flushes every 60 s and on exit. 30 daily buckets; durations stored as a fixed histogram (1 ms buckets up to 200 ms) for median without keeping samples.

### D3. Incident log
JSON lines, append-only, rotated at 500 entries. Fields: `time`, `type`, `details` (enum-coded values, exe name, error codes). No window titles (they often contain document names, emails or chat partners).

### D4. Word-analysis log protection
- File created with a DACL granting only the current user SID and SYSTEM (`FileSecurity`), inheritance disabled.
- Daily file rotation; files older than 7 days deleted on start and at midnight.
- Never written for Password Fields (no Keystrokes exist there anyway) or elevated windows.

### D5. Export
Builder produces plain text sections; masking replaces `%USERPROFILE%`, user name, and machine name. The preview is the exact string that will be copied/saved. "Save to file" defaults to Desktop with a timestamped name.

## Risks / Trade-offs

- [Users enable the analysis log and forget it] → settings page shows its size and a "Turn off and delete" button; retention is 7 days.
- [Clipboard export of diagnostics is visible to clipboard-history tools] → export defaults to "Save to file"; clipboard is a secondary button. (This is the only place Rekey writes the clipboard, and only on explicit user action.)
