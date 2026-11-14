# Spec Delta

## Purpose

Lets Rekey adapt to the individual user: words the user wants left alone become Protected Words, and words the user confirms become Learned Words that Rekey recognises in their Language.

## ADDED Requirements

### Requirement: Protected Words are never auto-corrected
Rekey SHALL NOT auto-correct a Word whose Rendering in the current Layout matches a Protected Word, ignoring case. A deferred run containing a Protected Word SHALL NOT be corrected.

#### Scenario: Protected brand name
- **WHEN** "ntcn" is a Protected Word and the user types "ntcn" followed by Space in English (US)
- **THEN** the text stays "ntcn "

#### Scenario: Run with a Protected Word
- **WHEN** a deferred run would correct "yt ntcn" and "ntcn" is a Protected Word
- **THEN** neither Word is corrected

### Requirement: Manual Correction ignores protection
The Hotkey SHALL correct a Protected Word when the user asks for it explicitly.

#### Scenario: User insists
- **WHEN** "ntcn" is a Protected Word and the user performs the Hotkey after typing it
- **THEN** the Word is corrected

### Requirement: Protection learned from repeated Undos
When the user undoes a Correction, Rekey SHALL add the Word to the Protected Words, as the original does after one Undo. Words shorter than 3 letters SHALL NOT be protected automatically (the original skips short words; the length is a design decision).

#### Scenario: Undo protects the Word
- **WHEN** the user undoes the Correction of "ntcn"
- **THEN** "ntcn" becomes a Protected Word
- **AND** the next "ntcn" followed by Space is not corrected

#### Scenario: Short word
- **WHEN** the user undoes the Correction of "yt"
- **THEN** "yt" does not become a Protected Word

### Requirement: Learned Words from Manual Correction
When the user manually corrects a Word whose new Rendering is not a word of the target Language, Rekey SHALL add that Rendering as a Learned Word of that Language. A Learned Word SHALL count as a common word of its Language in later Decisions.

#### Scenario: New slang word
- **WHEN** the user manually corrects "rhby;" to "кринж" and "кринж" is not in the Russian Language Profile
- **THEN** "кринж" becomes a Learned Word of Russian
- **AND** the next time "rhby;" is typed in English (US), Auto Correction corrects it to "кринж"

### Requirement: Undoing a learned correction removes the Learned Word
When the user undoes a Correction whose target Rendering is a Learned Word, Rekey SHALL remove that Learned Word.

#### Scenario: Learned by mistake
- **WHEN** "кринж" is a Learned Word and the user undoes a Correction to "кринж"
- **THEN** "кринж" is no longer a Learned Word

### Requirement: Users manage both lists
The user SHALL be able to view, add and remove Protected Words and Learned Words in settings. Changes SHALL take effect for the next Word.

#### Scenario: Remove protection
- **WHEN** the user removes "ntcn" from Protected Words
- **THEN** the next "ntcn" followed by Space is evaluated as usual

### Requirement: Lists persist and stay bounded
Protected Words and Learned Words SHALL persist across restarts. Each list SHALL hold up to 10,000 entries (design decision); when full, the least recently used automatic entry is dropped first and user-added entries are never dropped.

#### Scenario: Restart
- **WHEN** Rekey restarts
- **THEN** all Protected Words and Learned Words are still present
