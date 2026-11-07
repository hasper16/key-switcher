# Tasks

## 1. Consent and schemas (data shape first)

- [ ] 1.1 Add `DiagnosticsConsent` (both off) to `Settings`; verify defaults and round-trip tests pass
- [ ] 1.2 Define Usage Digest and setup-signal schema records; write a failing test that rejects schema properties named like text/word/title/path/user and asserts no stable id field; make it pass; verify
- [ ] 1.3 Add `PathMap` to `Directory.Build.props`; verify a built assembly's stack trace shows repository-relative paths

## 2. Crash and freeze reports

- [ ] 2.1 Integrate Sentry with the D1 options, initialised only with consent; verify a test that no Sentry hub is created without consent
- [ ] 2.2 Write failing tests for the `BeforeSend` allowlist (drops user, extra, breadcrumbs, device name; keeps exception types and frames); implement; verify they pass
- [ ] 2.3 Write a test scanning Core for exception messages built from Word/Rendering values; fix any; verify it passes
- [ ] 2.4 Implement the freeze watchdog (500 ms ping, 2 s threshold); verify with a test that blocks a fake dispatcher for 3 s and expects one freeze event

## 3. Digest, setup signal, sign-up

- [ ] 3.1 Write failing tests: at most one digest per day; offline → retry next day, older dropped; withdrawn consent deletes unsent data; implement the scheduler; verify they pass
- [ ] 3.2 Implement the one-time setup signal on welcome close; verify a test that reopening the welcome does not send again
- [ ] 3.3 Implement the email sign-up request and UI; verify manually that only one request is sent and nothing is stored after success

## 4. Transparency

- [ ] 4.1 Store the last 10 sent payloads and add "Show what was sent" to the Diagnostics and privacy page; verify manually that the displayed digest equals the request body captured by a local proxy
- [ ] 4.2 Add the consent checkbox to the welcome window and both toggles to settings; write `docs/privacy.md` listing every field sent; verify the document matches the schema records
