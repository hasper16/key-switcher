# Tasks

## 1. Counters

- [ ] 1.1 Define the daily counters model and `stats.json` contract (30 buckets, duration histogram); verify a round-trip test passes
- [ ] 1.2 Write failing tests: Corrections/Undos/Decisions increment the right counters; turning statistics off deletes them; median from histogram; implement the aggregator; verify they pass

## 2. Incidents

- [ ] 2.1 Write failing tests for the Incident log (append, 500-entry rotation, no title field); implement; verify they pass
- [ ] 2.2 Route every Incident type from earlier changes into the log; verify a test that each `IncidentType` value is persisted

## 3. Word-analysis log

- [ ] 3.1 Add `AnalysisLogEnabled` (off) and `StatisticsEnabled` (on) to `Settings`; verify defaults tests pass
- [ ] 3.2 Write failing tests: off → no file; on → entries written; 7-day retention; turning off deletes files; implement; verify they pass
- [ ] 3.3 Apply the user-only DACL on file creation; verify with a test that reads the ACL and finds only the user and SYSTEM

## 4. UI and export

- [ ] 4.1 Build the statistics window (today / 30 days, undo share, Decision breakdown, durations); verify manually with seeded data
- [ ] 4.2 Write failing tests for the export builder: default excludes words, word lists and titles; masking of profile path and user name; analysis log only when included; implement; verify they pass
- [ ] 4.3 Build the export dialog with preview, Save to file and Copy; verify manually that the saved file equals the preview
