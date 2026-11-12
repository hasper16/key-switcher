# Spec Delta

## Purpose

Defines how a Correction changes the text on screen, keeps the user's keys in order, avoids damaging text Rekey has lost track of, and can be undone.

## ADDED Requirements

### Requirement: Replace the Word with its Rendering
A Correction SHALL replace exactly the characters of the corrected Word(s) with the target Rendering and then switch to the target Layout. In a window where typed text is not known to arrive, a Correction MUST leave the Word untouched rather than erase it.

#### Scenario: Single Word
- **WHEN** a Correction changes "ghbdtn" to "привет" in Notepad
- **THEN** Notepad shows "привет" where "ghbdtn" was
- **AND** the current Layout is Russian

#### Scenario: Deferred run
- **WHEN** a Correction covers "yt pyf." with target "не знаю"
- **THEN** the text shows "не знаю" where "yt pyf." was

#### Scenario: Console
- **WHEN** a Correction changes "ghbdtn" to "привет" at a command prompt in Windows Terminal or a classic console window
- **THEN** the prompt line shows "привет" where "ghbdtn" was, followed by the Space

#### Scenario: Window that does not take typed text
- **WHEN** a Correction would apply in a window where typed text is not known to arrive
- **THEN** Rekey erases nothing and leaves the Word as typed

### Requirement: Key order is preserved
The Trigger Key that caused a Correction, and any keys the user presses while the Correction is being applied, SHALL reach the application after the corrected text and in the order they were pressed.

#### Scenario: Space after correction
- **WHEN** the user types "ghbdtn" followed by Space
- **THEN** the text reads "привет " with the space after the corrected word

#### Scenario: Enter in a chat box
- **WHEN** the user types "ghbdtn" followed by Enter in a chat box where Enter sends the message
- **THEN** the sent message reads "привет"

#### Scenario: Fast typing during a Correction
- **WHEN** the user keeps typing "rfr" while a Correction is being applied
- **THEN** "rfr" appears after the corrected text, in order

### Requirement: Clipboard is never used
A Correction or Undo MUST NOT read or change the clipboard.

#### Scenario: Clipboard preserved
- **WHEN** the clipboard holds "secret" and a Correction happens
- **THEN** the clipboard still holds "secret"

### Requirement: Verified replacement
When the focused control exposes its text, Rekey SHALL check that the text immediately before the caret equals the Word's current Rendering before changing it. On mismatch, the Correction MUST be cancelled, the text left untouched, and the cancellation recorded. Verified replacement SHALL be on by default and can be turned off in settings.

#### Scenario: Matching text
- **WHEN** the focused editor exposes its text and it ends with "ghbdtn" before the caret
- **THEN** the Correction proceeds

#### Scenario: Autocomplete changed the text
- **WHEN** an editor auto-completed "ghbdtn" to "ghbdtnxyz" before the caret
- **THEN** Rekey makes no change and records a cancelled Correction

#### Scenario: Control does not expose text
- **WHEN** the focused control does not expose its text
- **THEN** Rekey replaces the Word by erasing and retyping it

### Requirement: Undo within 5 seconds
If the user presses Ctrl+Z within the Undo Window (5 seconds, as in the original) of a Correction and before any other key, Rekey SHALL restore the text as originally typed and the previous Layout, and SHALL NOT pass that Ctrl+Z to the application.

#### Scenario: Immediate undo
- **WHEN** a Correction changed "ghbdtn " to "привет " and the user presses Ctrl+Z 2 seconds later
- **THEN** the text reads "ghbdtn " again and the Layout is English (US)

#### Scenario: Undo window passed
- **WHEN** the user presses Ctrl+Z 6 seconds after a Correction
- **THEN** Ctrl+Z goes to the application unchanged

#### Scenario: Correction triggered by Enter
- **WHEN** a Correction was triggered by Enter and the user presses Ctrl+Z within 5 seconds
- **THEN** Ctrl+Z goes to the application unchanged, because the line may already have been submitted

#### Scenario: Other typing in between
- **WHEN** after a Correction the user types another letter and then presses Ctrl+Z
- **THEN** Ctrl+Z goes to the application unchanged

### Requirement: Undo is reported
Each Undo SHALL be reported to the learning and statistics features with the original Word and the Rendering it was corrected to.

#### Scenario: Undo recorded
- **WHEN** the user undoes the Correction "ghbdtn" → "привет"
- **THEN** an Undo of "ghbdtn"/"привет" is recorded
