# Orchestrated implementation of OpenSpec changes

One **orchestrator** session drives the changes in `docs/roadmap.md` strictly in order, one at a time. For each change it starts one **implementer** session and keeps that same session for the whole life of the change: implementation, review fixes and archiving. Only the orchestrator merges into `main`.

## Orchestrator

The orchestrator coordinates; it does not author. It never edits files, commits or pushes. Its tools are session management (`start_session`, `SendMessage`, `list_sessions`), read-only `gh` / `git` queries, and `gh pr merge`.

Loop, for each change in roadmap order:

1. **Start** one implementer: `start_session` with `use_worktree: true`, name `impl-<change>`, a brief naming the change and pointing to this document. Record the session id.
2. **Wait** for the implementer's report. The app wakes the orchestrator when the session ends a turn; do not poll.
3. **Check readiness** of the PR the implementer reports (all must hold):
   - every required check passes: `gh pr checks <N> --required`;
   - every review thread is resolved, and no reviewer's latest review is `CHANGES_REQUESTED` (check each reviewer, not only the PR-wide `reviewDecision`):
     ```bash
     gh api graphql --paginate -F n=<N> -f query='query($n:Int!,$endCursor:String){repository(owner:"hasper16",name:"key-switcher"){pullRequest(number:$n){mergeable headRefOid latestReviews(first:100){totalCount nodes{author{login} state}} reviewThreads(first:100,after:$endCursor){totalCount pageInfo{hasNextPage endCursor} nodes{isResolved path comments(first:1){nodes{author{login} body}}}}}}}'
     ```
     `--paginate` follows `reviewThreads` pages; if `latestReviews.totalCount` exceeds 100, page it the same way before deciding.
   - the PR is mergeable (no conflicts, branch up to date with `main`).
   Anything open goes back to **the same implementer** via `SendMessage`, listing the unresolved threads or failing checks. Never fix it yourself and never hand it to another session.
4. **Manual-check gate**: if the PR adds or changes `docs/manual-tests/<change>.md` with unchecked items, record the current head SHA (`headRefOid`) first, then stop and ask the user (in Russian) to run that checklist on that commit, naming the SHA. Merge only after the user confirms the checklist for that SHA; then tell the implementer to tick the verified tasks, wait for the new commit and re-run step 3. A confirmation covers only the recorded commit. When the head moves after a confirmation, first record the new head SHA, then use that same pinned SHA for everything that follows: if `git diff --name-only <confirmed-sha>..<new-sha>` lists anything outside `docs/` and `openspec/`, name `<new-sha>` to the user and ask them to re-run the affected items on it. The new SHA becomes the one to merge.
5. **Merge** the exact head you verified in step 3: `gh pr merge <N> --merge --match-head-commit <verified-sha>`. If GitHub refuses because the head moved, re-run steps 3–4 for the new head. Never `--admin`, never bypass protection.
6. **Archive**: tell the same implementer to run `/opsx:archive <change>` on branch `chore/archive-<change>`, open a PR, and report. Run steps 2–5 for that PR.
7. **Next**: only after the archive PR is merged, start the next change. The implementer session is left idle (the user archives sessions).

Escalate to the user (and pause the loop) when: a manual checklist needs running; an implementer raises a spec question; one PR needs more than 3 rounds of review fixes; CI fails for a reason the implementer cannot fix; a merge is refused by GitHub. Report progress to the user in one or two lines after each merge.

## Implementer

- Work only in your worktree, on branch `change/<change>` (archive: `chore/archive-<change>`).
- Load `ub-mode`, run `/opsx:apply <change>`, follow `AGENTS.md`. Conventional Commits, no `Co-Authored-By` trailers.
- Push your branch and open a PR against `main`; turn on Auto-fix for it so CI failures and review comments wake you.
- Resolve **every** review thread (CodeRabbit and humans): fix the code, or reply with the reason it stays as is; then mark the thread resolved. Keep the branch up to date with `main`.
- Tasks that need a person at a Windows desktop (UI checks, sign-out, UAC, real apps): write the steps in `docs/manual-tests/<change>.md` as a checklist and leave those tasks unticked.
- Report to the orchestrator in one line: `ready: PR #<N>` (or `blocked: <reason>`, or `question: <spec question>`).
- Never merge a PR, never start, stop or message other sessions, never start the next change.
