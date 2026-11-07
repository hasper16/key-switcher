# Spec Delta

## Purpose

Defines what Rekey may send over the network about crashes and usage, the consent required before anything is sent, and the data that must never leave the user's computer.

## ADDED Requirements

### Requirement: Nothing is sent without consent
Rekey MUST NOT send crash reports, freeze reports, Usage Digests or the setup signal until the user has agreed. The welcome window SHALL ask, with the choice unticked by default, and the user SHALL be able to change it at any time in settings.

#### Scenario: Fresh install, no answer
- **WHEN** Rekey is installed and the user closes the welcome window without ticking diagnostics
- **THEN** Rekey makes no telemetry network request

#### Scenario: Consent withdrawn
- **WHEN** the user turns diagnostics off in settings
- **THEN** no further telemetry is sent and any unsent reports are deleted

### Requirement: Typed text never leaves the computer
No network request made by Rekey SHALL contain typed text, Words, Renderings, Protected Words, Learned Words, clipboard content, window titles, the Windows user name, file paths inside the user profile, or memory dumps.

#### Scenario: Crash while a Word is buffered
- **WHEN** Rekey crashes while the current Word is "secret"
- **THEN** the crash report contains no "secret" and no memory dump

### Requirement: Crash and freeze reports
With consent, Rekey SHALL report unhandled exceptions and freezes of its UI or engine lasting longer than 2 seconds, including exception type, stack trace, Rekey version, Windows version and the Incident types of the last 10 minutes (both periods are design decisions).

#### Scenario: Engine freeze
- **WHEN** consent is given and the engine thread does not respond for 3 seconds
- **THEN** a freeze report with the engine stack and versions is sent

### Requirement: Daily Usage Digest
With consent, Rekey SHALL send at most one Usage Digest per day containing only counters, Rekey and Windows versions, the UI language, the Language codes of installed Layouts, and on/off states of main settings. It SHALL carry no stable identifier of the user or device.

#### Scenario: Digest content
- **WHEN** a Usage Digest is sent
- **THEN** it contains counts such as Words analysed and Corrections, and no field that identifies the device across days

#### Scenario: Offline
- **WHEN** the digest cannot be sent today
- **THEN** Rekey tries again the next day with that day's counters and discards older unsent digests

### Requirement: One-time setup signal
With consent, Rekey SHALL send one anonymous setup signal when the welcome window closes, containing versions, UI language, number of installed Layouts and covered Languages, the main settings chosen, and how far the user got in the welcome window.

#### Scenario: Sent once
- **WHEN** the user closes the welcome window with consent and later reopens and closes it again
- **THEN** only one setup signal has been sent

### Requirement: Optional email sign-up
Rekey SHALL offer an optional field where the user can enter an email address to hear about new versions or about problems they reported. Rekey SHALL send the address only when the user submits it, only for the ticked purposes, and SHALL NOT link it to any report.

#### Scenario: Sign up
- **WHEN** the user enters an email, ticks "New versions" and submits
- **THEN** one request with the email and that purpose is sent
- **AND** no crash report or digest contains the email

### Requirement: Show what was sent
Settings SHALL show the content of the last 10 telemetry requests (design decision) exactly as sent.

#### Scenario: Inspect last digest
- **WHEN** the user opens "Show what was sent"
- **THEN** the last Usage Digest is shown in full

### Requirement: Secure transport
Telemetry SHALL be sent only over HTTPS with standard certificate validation to endpoints fixed in the app.

#### Scenario: Invalid certificate
- **WHEN** the telemetry endpoint presents an invalid certificate
- **THEN** nothing is sent and the report is retried later or discarded per its rules
