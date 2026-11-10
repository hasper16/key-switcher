# Spec Delta

## Purpose

Defines how Rekey observes the user's typing across all applications, where Words begin and end, which input it must never record, and how it stays attached to the keyboard when Windows interferes.

## ADDED Requirements

### Requirement: Observe typing without disturbing it
While running, Rekey SHALL observe every Keystroke in the user's session and SHALL pass each one on to the focused application unchanged, unless another requirement explicitly holds it.

#### Scenario: Normal typing
- **WHEN** the user types "hello" in any application
- **THEN** the application receives "hello" exactly as typed
- **AND** Rekey's current Word is "hello"

### Requirement: Keys that build a Word
Keys that produce a character in any active Layout (letters, digits, punctuation) SHALL extend the current Word, except Space, Enter and Tab. Backspace SHALL remove its last Keystroke.

#### Scenario: Backspace
- **WHEN** the user types "ghbdtnn" and presses Backspace once
- **THEN** the current Word is "ghbdtn"

#### Scenario: Punctuation inside a Word
- **WHEN** the user types "ghbdtn!" in English (US)
- **THEN** the current Word is "ghbdtn!"

### Requirement: Keys and events that end a Word
Space, Enter (on the main keyboard or the numeric keypad) and Tab SHALL end the Word whatever character they produce, as in the original; only a Trigger Key makes Rekey evaluate it (spec `auto-correction`). Arrow keys, Home, End, Page Up, Page Down, Delete, Escape, any key combined with Ctrl, Alt or Win, a mouse click, or a focus change SHALL end it without evaluation.

#### Scenario: Space that is not a Trigger Key
- **WHEN** Space is not a Trigger Key and the user types "ghb", Space, then "dtn"
- **THEN** the current Word is "dtn" and "ghb" was not evaluated

#### Scenario: Caret moved
- **WHEN** the user types "ghb", presses Left Arrow, then types "dtn"
- **THEN** the current Word is "dtn"

#### Scenario: Mouse click
- **WHEN** the user types "ghb" and clicks elsewhere in the text
- **THEN** the current Word is empty

### Requirement: Typing gap ends the Word
A pause of more than 2 seconds between Keystrokes SHALL end the current Word and drop any Deferred Words.

#### Scenario: Pause mid-word
- **WHEN** the user types "ghb", waits 3 seconds, then types "dtn"
- **THEN** the current Word is "dtn" and no Deferred Word remains

### Requirement: Password Fields are never recorded
Rekey MUST NOT record Keystrokes, keep a Word, or make any Correction while the focused control is a Password Field, except for the known limit stated under "Each Keystroke is confirmed outside a Password Field" (a page script moving focus there and back within milliseconds).

#### Scenario: Typing a password
- **WHEN** the user types in a password box
- **THEN** Rekey keeps no Keystrokes from that typing and makes no Correction

### Requirement: Each Keystroke is confirmed outside a Password Field
Rekey SHALL keep a Keystroke only after confirming that the focused control is not a Password Field while nothing typed or clicked after that Keystroke is waiting to be processed (design decision), so a focus move inside one window (Tab, Shift+Tab, a click, the page itself) is covered before Windows reports it. When Rekey cannot confirm this, it SHALL keep nothing more of that Word, which then gets no Correction.

#### Scenario: Focus moves to a password box inside the same window
- **WHEN** the user presses Tab from a user-name box into a password box on the same web page and types at once
- **THEN** Rekey keeps none of the Keystrokes typed in the password box, even before Windows reports the focus change

#### Scenario: Rekey falls behind the typing
- **WHEN** more input is already waiting while Rekey processes a Keystroke
- **THEN** Rekey keeps nothing more of that Word and makes no Correction of it

#### Scenario: Known limit — a script moves focus there and back at once
- **WHEN** a page script moves focus into a Password Field and back to the same control within milliseconds of a Keystroke, before Windows reports either move
- **THEN** that Keystroke may reach the Word until the next focus notice clears it (accepted by the owner: Rekey sees focus only after the Keystroke)

### Requirement: Own input is ignored
Input that Rekey injects itself SHALL NOT be recorded as user typing.

#### Scenario: After a Correction
- **WHEN** Rekey retypes a Word as part of a Correction
- **THEN** the retyped characters do not appear in the current Word or in statistics as user typing

### Requirement: Keyboard observation recovers automatically
While the keyboard watchdog is on (default, as in the original), when Windows stops delivering Keystrokes to Rekey while the user is typing, Rekey SHALL detect it within 2 seconds (design decision) of the next user input, reattach, and record an Incident. The user SHALL be able to turn the keyboard watchdog off; then Rekey does not check for a dropped hook.

#### Scenario: Hook dropped by Windows
- **WHEN** Windows stops delivering Keystrokes to Rekey and the user keeps typing
- **THEN** Rekey reattaches within 2 seconds of detecting missing input
- **AND** an Incident records the drop and the recovery

#### Scenario: Watchdog turned off
- **WHEN** the keyboard watchdog is off and Windows stops delivering Keystrokes to Rekey
- **THEN** Rekey does not reattach on its own and records no Incident

### Requirement: Self-pause after repeated failures
If keyboard observation fails twice within 10 minutes, Rekey SHALL pause Auto Correction, tell the user through the tray, and retry automatically after 10 minutes (both periods are design decisions; the original shows the same self-pause message). The user SHALL be able to resume manually at any time.

#### Scenario: Two failures in a row
- **WHEN** keyboard observation fails twice within 10 minutes
- **THEN** Auto Correction is paused and the tray shows that it paused itself
- **AND** Rekey retries after 10 minutes

#### Scenario: Manual resume
- **WHEN** Auto Correction paused itself and the user chooses Resume in the tray
- **THEN** Rekey reattaches immediately and Auto Correction resumes

### Requirement: Restart after a repeated freeze
When Rekey's own processing of input stops responding for more than 5 seconds a second time within one hour (design decisions), Rekey SHALL record an Incident and, while "Restart after a repeated freeze" is on (default, as in the original), restart itself, at most once per 10 minutes (as in the original).

#### Scenario: Second freeze
- **WHEN** input processing froze once and freezes again for more than 5 seconds 20 minutes later
- **THEN** Rekey restarts, its tray icon is back, and an Incident records the restart

#### Scenario: Restart limit
- **WHEN** Rekey restarted itself 4 minutes ago and input processing freezes twice more
- **THEN** Rekey does not restart again before 10 minutes have passed since the last restart

#### Scenario: Restart turned off
- **WHEN** "Restart after a repeated freeze" is off and input processing freezes a second time within one hour
- **THEN** Rekey records an Incident and keeps running without restarting

### Requirement: Elevated windows
When the foreground window belongs to a process with higher privileges than Rekey, Rekey SHALL make no Correction there and SHALL NOT treat the missing input as a failure.

#### Scenario: Administrator console
- **WHEN** the user types in a console running as administrator while Rekey runs as a normal user
- **THEN** Rekey makes no Correction and records no Incident
