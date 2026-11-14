# manual-correction Specification

## Purpose
Lets the user ask Rekey to correct the last Word or a selected text on demand with a Hotkey, including where Auto Correction is off, without clashing with system shortcuts.

## Requirements

### Requirement: Hotkey choices
The user SHALL be able to choose the Hotkey from: double tap of Shift (default), double tap of Ctrl, double tap of Alt, Ctrl+Space, Win+Space, or a custom combination of modifiers and one key. Ctrl+Space and Win+Space are the Windows counterparts of the original's ⌃Space, ⌘Space and ⌥⌘Space options.

#### Scenario: Default Hotkey
- **WHEN** Rekey is freshly installed
- **THEN** the Hotkey is a double tap of Shift

### Requirement: Win+Space replaces the Windows Layout Switch shortcut
When the Hotkey is Win+Space, Windows SHALL NOT also switch Layout on that press, and when there is nothing to correct the Hotkey SHALL switch to the next installed Layout, as Windows would.

#### Scenario: Win+Space after a wrong-layout Word
- **WHEN** the Hotkey is Win+Space and the user types "ghbdtn" in English (US) and presses Win+Space
- **THEN** the text becomes "привет", the Layout is Russian, and Windows makes no second Layout Switch

#### Scenario: Win+Space with nothing to correct
- **WHEN** the Hotkey is Win+Space and the user presses it in an empty text field with English (US) active and Russian installed
- **THEN** the Layout becomes Russian and the Start menu does not open

### Requirement: Double tap recognition
A double tap SHALL count only when the chosen modifier is pressed and released twice within 400 ms (design decision) with no other key in between, and the modifier taps SHALL still reach the application.

#### Scenario: Two quick taps
- **WHEN** the user taps Shift twice within 300 ms
- **THEN** the Hotkey is performed

#### Scenario: Shift used for a capital letter
- **WHEN** the user presses Shift, types "G", releases Shift, then taps Shift once
- **THEN** the Hotkey is not performed

### Requirement: Correct the last Word
When the Hotkey is performed after a Word (with at most one Space or Tab typed after it), Rekey SHALL correct that Word to its best Rendering in another active Layout and switch to that Layout. A Word followed by Enter is not corrected, because the line may already have been submitted (as for Undo).

#### Scenario: Doubtful word
- **WHEN** the user types "yt" followed by Space in English (US) and performs the Hotkey
- **THEN** the text becomes "не " and the Layout is Russian

#### Scenario: More than two Layouts
- **WHEN** English, Russian and Ukrainian are active and the user performs the Hotkey after "ghbdsn" typed in English (US)
- **THEN** the Word becomes its best-scoring Rendering, "привіт" in Ukrainian

#### Scenario: Word followed by Enter
- **WHEN** the user types "yt" followed by Enter in English (US) and performs the Hotkey
- **THEN** no text changes and the Layout becomes Russian

#### Scenario: Pause before the Hotkey
- **WHEN** the user types "ghbdtn", waits 5 seconds and performs the Hotkey
- **THEN** the text becomes "привет" and the Layout is Russian

### Requirement: Correct the selected text
When text is selected and the focused control exposes the selection, the Hotkey SHALL replace the selection with its Rendering in the best other active Layout, character by character, and switch to that Layout.

#### Scenario: Selected sentence
- **WHEN** the user selects "ghbdtn vbh" and performs the Hotkey
- **THEN** the selection is replaced with "привет мир"

#### Scenario: Selection not readable
- **WHEN** text is selected but the control does not expose the selection
- **THEN** Rekey performs only a Layout Switch and leaves the text unchanged

### Requirement: Nothing to correct
When there is no current Word, no recent Word and no readable selection, the Hotkey SHALL perform a Layout Switch to the next installed Layout.

#### Scenario: Empty field
- **WHEN** the user performs the Hotkey in an empty text field with English (US) active and Russian installed
- **THEN** the Layout becomes Russian and no text changes

### Requirement: Hotkey after a Correction reverts it
When the Hotkey is performed after a Correction under the same conditions as an Undo with Ctrl+Z (within the Undo Window, before any other key, not after an Enter-triggered Correction), Rekey SHALL revert that Correction as an Undo. Otherwise the Hotkey acts as described above.

#### Scenario: Hotkey reverts
- **WHEN** Auto Correction changed "ghbdtn " to "привет " and the user performs the Hotkey
- **THEN** the text reads "ghbdtn " and the Layout is English (US)
- **AND** an Undo is recorded

#### Scenario: After an Enter-triggered Correction
- **WHEN** a Correction was triggered by Enter and the user performs the Hotkey within 5 seconds
- **THEN** the Correction is not reverted

### Requirement: Works where Auto Correction is off
The Hotkey SHALL work when Auto Correction is off, paused or excluded for the app, and MUST NOT work in Password Fields.

#### Scenario: Password Field
- **WHEN** the user performs the Hotkey in a Password Field
- **THEN** nothing happens

### Requirement: Conflict warning
When the chosen Hotkey matches the Windows Layout Switch shortcut or a hotkey another application has registered, Rekey SHALL say which action it is taking over and offer to pick a different Hotkey. Rekey SHALL NOT change Windows settings itself (as in the original, which asks the user to change the system shortcut).

#### Scenario: Same as Windows shortcut
- **WHEN** the user picks Win+Space as a custom Hotkey
- **THEN** Rekey explains that the Hotkey will replace the Windows Layout Switch shortcut and keeps the previous Hotkey until the user confirms or picks another

#### Scenario: Taken by another app
- **WHEN** the user picks a custom Hotkey that another running application has registered
- **THEN** Rekey shows that the combination is already in use and keeps the previous Hotkey until the user confirms or picks another
