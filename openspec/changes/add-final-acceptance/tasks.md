# Tasks

## 1. Acceptance pass

- [ ] 1.1 Pin the commit of `main` that contains changes 1–14 and run CI plus every `tools/Rekey.DesktopChecks` set on it; verify all pass and note the SHA at the top of `docs/manual-tests/deferred-by-hand.md`
- [ ] 1.2 Owner runs every unchecked item of `docs/manual-tests/deferred-by-hand.md` on that SHA; verify each item is either ticked or carries a finding (steps, expected, observed)
- [ ] 1.3 Add one task per finding to group 2, named by capability; verify every finding in the file has a matching task here

## 2. Fixes

- [ ] 2.1 For each finding: when the spec is wrong, revise it with `/opsx:update` first; then write a failing test, fix, and verify the test and the full automated suite pass (one commit per capability)

## 3. Re-check and close

- [ ] 3.1 Owner re-runs by hand the items touched by group 2 on the new head; verify each is ticked with that SHA noted
- [ ] 3.2 Tick the deferred by-hand tasks of the archived changes, each with a pointer to the run in `deferred-by-hand.md`; verify no by-hand task in `openspec/changes/archive/*/tasks.md` is left unticked

## Workflow follow-up

- Archive the change after its PR is merged; if group 2 added delta specs, the archive syncs them to `openspec/specs/`.
