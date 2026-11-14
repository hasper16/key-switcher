# Design

## Context

See proposal.md for why. Each earlier change ticks its tasks on automated evidence (unit tests, `tools/Rekey.DesktopChecks`) and appends what still needs a person at a Windows desktop to `docs/manual-tests/deferred-by-hand.md`, under a section named after the change (`docs/agents/orchestration.md`). By the time this change starts, that file is the complete list of what nobody has checked by hand.

## Goals / Non-Goals

**Goals:**
- One by-hand pass over the whole app, on one known commit, with a written result per item.
- Every failure ends either fixed (code plus test) or as a revised spec, inside this change.

**Non-Goals:**
- New features, or reworking checks that already run automatically.
- Moving by-hand items into automation (welcome when cheap, but not required to close the change).

## Decisions

- **One pass on a pinned commit, then re-runs only of what a fix touches.** The owner records the commit SHA at the top of the run. After fixes, only the items whose area the fix touched are run again, on the new head, and the file notes that SHA for them. Alternative: re-run everything after each fix; rejected as the cost the owner wants to avoid.
- **Findings live next to the checklist.** A failing item gets a short finding under it in `deferred-by-hand.md` (steps, expected, observed) and a link to the fixing commit. No separate bug tracker; the file is the record. Findings follow the privacy guardrails: invented Words only, no window titles beyond the test apps the checklist names.
- **Fix in place, spec first when the spec is wrong.** A failure caused by code gets a failing test first (ub-mode), then the fix. A failure that shows the requirement itself is wrong goes through `/opsx:update` of this change, adding a delta for that capability before the code. This is why `skip_specs` may be dropped mid-change.
- **Fixes are grouped by capability.** One commit per capability's fixes, so the tasks list can grow one group per affected area once the findings are known.
- **Desktop automated checks still run.** Before the by-hand pass, the orchestrator's usual automated gate (CI plus every `tools/Rekey.DesktopChecks` set on the pinned commit) passes; the by-hand pass starts from a green automated state.

## Risks / Trade-offs

- [Late discovery: a by-hand failure may come from an early change and ripple into later ones] → Fixes go spec first and the full automated suite runs on every fix commit; items touched by a fix are re-run by hand.
- [The fix list is unknown, so the task list is coarse] → Group 2 grows with one task per finding when the pass is done; the orchestrator re-reads `tasks.md` before each round.
- [A large pass is tiring and items may be skipped] → Each item is ticked or carries a finding; an item left blank keeps the change open.
