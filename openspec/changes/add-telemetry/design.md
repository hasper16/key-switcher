# Design

## Context

The original sends crash and hang reports through Sentry, a daily usage digest that it describes as counts only, and a one-time setup signal to its developer's endpoint, and offers an optional email sign-up. It enables diagnostics by default in pre-release builds; we make everything opt-in. Rekey observes every keystroke, so the threat model treats any outbound byte as a potential leak of typed text.

## Goals / Non-Goals

**Goals:**
- An allowlist-based pipeline: only fields we enumerate can be sent.
- Memory dumps impossible by construction.

**Non-Goals:**
- Building the backend; this change defines the request contract and uses our own endpoint (not the original's).

## Decisions

### D1. Sentry configuration
- `SendDefaultPii = false`, `AttachStacktrace = true`, `MaxBreadcrumbs = 0` (breadcrumbs can capture UI events), `AutoSessionTracking = false`, no screenshots, no view hierarchy.
- Native crash minidumps disabled; only managed exceptions are captured. The engine never stores text in exception messages (analyzer rule: no `Word`/`Rendering` values in exception message construction — enforced by a test that greps exception messages in Core).
- `BeforeSend` rebuilds the event from an allowlist: exception types, frames (module, function, line; no local variables), release, OS version, Incident type list. Everything else is dropped, including `user`, `request`, `extra`, `contexts.device.name`, `server_name`.
- Hang detection: engine freezes come from the freeze watchdog of `add-keyboard-capture-and-layouts` (D7), which reports any stall over 2 s; this change adds a UI-dispatcher ping every 500 ms. > 2 s no reply → capture a freeze event with the stalled thread's managed stack (`Thread`-level stack via `System.Diagnostics` on the watchdog) — no dump.

### D2. Digest and setup signal
- JSON documents with a versioned schema in `Rekey.Core/Telemetry/Schemas`; serializers generated from records, so adding a field requires a code change reviewed against this design.
- No stable id; each request carries a random `requestId` (UUID v4) for server de-duplication only.
- Scheduled once a day at a random minute after the first hour of activity to avoid a burst at boot.
- Last 10 sent payloads stored locally (`sent.jsonl`) for "Show what was sent".

### D3. Consent
`DiagnosticsConsent { CrashReports, UsageDigest }` both false by default; welcome window shows one checkbox that sets both; settings show them separately. Sentry is initialised only when `CrashReports` is true and disposed when turned off.

### D4. Email sign-up
Separate endpoint, separate request, submitted only on button press; no client-side storage of the email after a successful send.

## Risks / Trade-offs

- [A future developer adds a text field to a report] → allowlist serializers + a unit test that fails if any telemetry schema has a property named like `text`, `word`, `title`, `path`, `user`; code review checklist in `docs/privacy.md`.
- [Stack traces include file paths of the build machine] → build with `/pathmap` (`PathMap`) so paths are repository-relative.
- [Opt-in yields few reports] → accepted; privacy is a core promise of a keystroke-observing app.
