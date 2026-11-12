# Rekey for Windows

Tray app that fixes a word typed in the wrong keyboard layout ("ghbdtn" → "привет") and switches the layout. Windows analog of the macOS app Rekey. C# / .NET 10, WPF.

## Context to read

- `GLOSSARY.md` — domain terms. Use them verbatim in code, specs, tests and commits.
- `docs/roadmap.md` — order of OpenSpec changes and their dependencies. Pick the next work here.
- `docs/adr/` — architecture decisions. Read before changing module structure, threading or dependencies.
- `.private/reverse-engineering/rekey-macos.md` — the owner's notes on the original app's observed behavior. Local only (git-ignored, absent in clones); read it when present and you write or question a requirement. Never copy its quotes, identifiers or decompiled details into tracked files: describe the original's behavior in our own words.

## Workflow: spec first

- Every behavior change lives in an OpenSpec change: `/opsx:propose` → `/opsx:apply` → `/opsx:archive`. Artifact rules are in `openspec/config.yaml`.
- Work outside a change is limited to typos, formatting, comments and dependency bumps that keep behavior.
- When code shows a spec is wrong, revise the change first (`/opsx:update`), then code to the revised spec.
- A task is done when the verification written in its line passes; tick it in `tasks.md` in the same commit.

## Code

- Load the `ub-mode` skill before writing code and follow it on every edit: failing test first, smallest change, then refactor.
- `Rekey.Core` is pure .NET; Windows types live in `Rekey.Win32` and `Rekey.App`. The engine takes events and returns actions; adapters execute them (ADR 0002).
- The keyboard hook callback copies the event into the queue and returns. Allocation, locks, logging and I/O run on the worker thread.
- Every variable shared between threads has exactly one writer.
- Win32 calls go through CsWin32 (`NativeMethods.txt`).
- Style is the Google C# Style Guide (https://google.github.io/styleguide/csharpguide.html): 2-space indent, 100-column limit, braces on the same line and always present, `_camelCase` for non-public fields, members grouped as nested types → static/const/readonly fields → fields and properties → constructors → methods, public before private in each group. Formatting and naming are encoded in `.editorconfig`; StyleCop deviations and their reasons are in the bootstrap design (D9).
- Comments explain why, not what: write one only where logic is non-obvious, tricky, or needs business context, and keep it short and plain. Self-explanatory code stays uncommented. This overrides the Google guide's "comment almost every method".
- Public types and members carry XML docs; internal ones only when complex. Before writing XML docs, read `docs/conventions/xml-docs.md` for wording and tags.
- Analyzers are the reviewers: SonarAnalyzer.CSharp (SonarLint rules), StyleCop.Analyzers (Checkstyle equivalent) and .NET code style run in every build with warnings as errors. Fix the code to satisfy them; a rule is disabled only in `.editorconfig` with a one-line reason, and an in-code suppression carries a `Justification`.

## Privacy guardrails

Rekey sees every keystroke. Typed text exists only in the engine's memory:

- Logs, exceptions, Incidents, telemetry, diagnostics and test output carry counters, types and codes. Words, Renderings and window titles appear only in the opt-in word-analysis log (spec `statistics-and-diagnostics`).
- Corrections and Undo work through key injection; the clipboard is untouched.
- Tests and fixtures use invented words.
- The original app's dictionaries, UI texts and server addresses are reference material, never copied (ADR 0003).

## Git

- Changes are implemented one at a time under an orchestrator session. Before working as the orchestrator or as an implementer, read `docs/agents/orchestration.md`.
- One branch per OpenSpec change: `change/<change-name>`; its archive goes on `chore/archive-<change-name>`. Only the orchestrator merges into `main`, through the PR, once every required check passes and every review thread is resolved. Implementers never merge and never start the next change.
- Commit messages follow Conventional Commits 1.0.0 (https://www.conventionalcommits.org/en/v1.0.0/#specification), in English, one commit per finished task group: `<type>(<scope>)!: <description>`, blank line, body, footers. Types: feat, fix, build, chore, ci, docs, style, refactor, perf, test, revert. Lowercase type and scope; header up to 100 characters; breaking changes via `!` or a `BREAKING CHANGE:` footer. Scopes name a module or area: core, win32, app, profiles, detection, capture, layouts, replacement, ui, telemetry, update, l10n, specs, tools, deps. `.githooks/commit-msg` rejects other formats.
- Implementers push their own `change/*` and `chore/archive-*` branches; any other push happens only when the user asks. Force-push, history rewrites, hard resets and branch deletion are the user's to run (blocked by `.claude/hooks/`).
- Secrets (`.env`, certificates, keys, tokens) stay local or in CI secrets. `.githooks/pre-commit` blocks them and checks formatting and Core tests; after cloning run `git config core.hooksPath .githooks`. When the hook fails, fix the cause and commit again.

## Language

- Reply to the user in Russian.
- Under orchestration (`docs/agents/orchestration.md`), only the orchestrator talks to the user in Russian. Implementers think, report and reply in English, including to the user, to save tokens.
- Code, comments, specs, commits and agent-facing docs are in English.
