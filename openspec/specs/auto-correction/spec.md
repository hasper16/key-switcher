# auto-correction Specification

## Purpose
Defines when Rekey corrects Words on its own: which keys end a Word for evaluation, the on/off switch, and the places and times where Auto Correction stays off.

## Requirements

### Requirement: Auto Correction on Trigger Keys
When Auto Correction is on and a Trigger Key ends a non-empty Word, Rekey SHALL evaluate the Word and apply the resulting Decision.

#### Scenario: Space ends a wrong-layout Word
- **WHEN** Auto Correction is on and the user types "ghbdtn" followed by Space in English (US)
- **THEN** the text becomes "привет " and the Layout becomes Russian

#### Scenario: Non-trigger key
- **WHEN** Tab is not a Trigger Key and the user types "ghbdtn" followed by Tab
- **THEN** no evaluation happens and the text is unchanged

### Requirement: On by default with a global switch
Auto Correction SHALL be on after installation. When the user turns it off, Rekey SHALL make no Auto Correction until it is turned on again.

#### Scenario: Turned off
- **WHEN** the user turns Auto Correction off and types "ghbdtn" followed by Space
- **THEN** the text stays "ghbdtn "

### Requirement: Configurable Trigger Keys
The user SHALL be able to choose any combination of Space, Enter and Tab as Trigger Keys; Enter covers both the main Enter key and the numeric keypad Enter, as in the original. By default Space and Enter are Trigger Keys and Tab is not (design decision).

#### Scenario: Defaults
- **WHEN** Rekey is freshly installed
- **THEN** Space and Enter are Trigger Keys and Tab is not

#### Scenario: Tab enabled
- **WHEN** the user enables Tab as a Trigger Key and types "ghbdtn" followed by Tab
- **THEN** the Word is corrected and the Tab is delivered after it

#### Scenario: Numeric keypad Enter
- **WHEN** Enter is a Trigger Key and the user types "ghbdtn" followed by Enter on the numeric keypad
- **THEN** the Word is corrected and the Enter is delivered after it

#### Scenario: Enter disabled
- **WHEN** Enter is not a Trigger Key and the user types "ghbdtn" followed by Enter
- **THEN** the text is unchanged

### Requirement: Excluded Apps
Auto Correction SHALL be off while the foreground application is an Excluded App. Manual Correction SHALL still work there.

#### Scenario: Typing in an excluded terminal
- **WHEN** Windows Terminal is an Excluded App and the user types "ghbdtn" followed by Space in it
- **THEN** the text stays "ghbdtn "

#### Scenario: Manual Correction in an Excluded App
- **WHEN** the foreground app is an Excluded App and the user performs the Hotkey after "ghbdtn"
- **THEN** the Word is corrected to "привет"

### Requirement: Excluded Apps are identified by application
An Excluded App SHALL be matched by its executable path, or for packaged Windows apps by their application identity, independent of window title.

#### Scenario: Packaged app
- **WHEN** a Microsoft Store app is excluded
- **THEN** Auto Correction is off in every window of that app and on in other apps hosted by the same frame process

### Requirement: Pause
The user SHALL be able to pause Auto Correction for a chosen period. While paused, no Auto Correction happens. When the period ends, Auto Correction resumes on its own. A pause SHALL survive a restart of Rekey.

#### Scenario: Pause for 30 minutes
- **WHEN** the user pauses Auto Correction for 30 minutes
- **THEN** no Auto Correction happens for 30 minutes
- **AND** after 30 minutes Auto Correction works again without user action

#### Scenario: Restart during pause
- **WHEN** Rekey is restarted 10 minutes into a 30-minute pause
- **THEN** Auto Correction stays paused for the remaining 20 minutes

### Requirement: No correction right after the user switches Layout
When the setting "Don't correct after I switch Layout" is on (default), a Word during which the user switched Layout themselves SHALL NOT be auto-corrected. A switch made before the Word's first Keystroke counts for that Word, unless the Word ends first, as by a click, a focus change or a pause of more than 2 seconds after the switch (design decision). A Layout Switch made by Rekey is not the user's.

#### Scenario: User switched mid-word
- **WHEN** the setting is on and the user switches Layout with the Windows shortcut while typing a Word, then presses Space
- **THEN** that Word is not corrected

#### Scenario: User switched just before the Word
- **WHEN** the setting is on and the user switches Layout with the Windows shortcut, types "ghbdtn" at once and presses Space, then types "ghbdtn" and Space again
- **THEN** the first Word is not corrected and the second one is

#### Scenario: Rekey switched
- **WHEN** a Correction switched the Layout to Russian and the user types "руддщ" followed by Space
- **THEN** the Word is evaluated as usual and becomes "hello "

#### Scenario: Setting off
- **WHEN** the setting is off and the user switches Layout while typing a Word, then presses Space
- **THEN** the Word is evaluated as usual

### Requirement: Safety gates
Auto Correction MUST NOT happen in a Password Field, in a window with higher privileges than Rekey, or while Rekey has paused itself after keyboard failures.

#### Scenario: Self-paused
- **WHEN** Rekey has paused itself after repeated keyboard failures
- **THEN** no Auto Correction happens until it resumes
