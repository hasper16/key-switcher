# Proposal

## Why

Crashes and freezes on other people's machines are invisible without reports, and the only way to know whether Rekey helps real users is aggregate counts. The original sends crash/freeze reports (Sentry), a daily usage digest of counters, and a one-time anonymous setup signal. Rekey observes every keystroke, so anything that leaves the machine must be opt-in, minimal and inspectable.

## What Changes

- Crash and freeze reports, sent only with the user's consent, without typed text, window titles, user names, paths or memory dumps.
- Daily Usage Digest of counters only, with no device identifier, sent only with consent.
- One-time anonymous setup signal after the welcome window, sent only with consent.
- Optional "stay in touch" email sign-up, sent only when the user submits it.
- Consent asked in the welcome window, changeable in settings; "Show what was sent" view.

## Capabilities

### New Capabilities

- `telemetry`: What Rekey may send over the network about crashes and usage, under which consent, and what it must never contain.

### Modified Capabilities

None.

## Impact

- New dependency: Sentry .NET SDK.
- New backend endpoint (ours) for the Usage Digest, setup signal and sign-up; out of scope to build here beyond a contract.
- App: consent block in the welcome window and the Diagnostics and privacy settings page.
- Settings: `DiagnosticsConsent`. Local file: `sent.jsonl` with the last 10 sent payloads.
- Depends on `add-statistics-and-diagnostics`, `add-onboarding`, `add-tray-and-settings-ui`.
