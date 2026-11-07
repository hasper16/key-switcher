# Spec Delta

## Purpose

Decides, for each Word the user finishes typing, whether it was typed in the wrong Layout, which Layout and Rendering it should become, and when Rekey must wait for more evidence instead of acting.

## ADDED Requirements

### Requirement: Render the Word in every active Layout
For a finished Word, Rekey SHALL compute its Rendering in each Layout of an active Language from the recorded Keystrokes and their Shift and Caps Lock state.

#### Scenario: Cyrillic from Latin keystrokes
- **WHEN** the user types the keys for "ghbdtn" in English (US) and Russian is active
- **THEN** the Russian Rendering is "привет"

#### Scenario: Punctuation keys that are letters elsewhere
- **WHEN** the user types ",tp" in English (US) and Russian is active
- **THEN** the Russian Rendering is "без"

### Requirement: Correct a clear wrong-layout Word
Rekey SHALL decide to correct a Word when its Rendering in another active Layout is a common word of that Layout's Language and its Rendering in the current Layout is not a word of the current Language.

#### Scenario: English typed as Russian
- **WHEN** the current Layout is Russian and the user types "руддщ"
- **THEN** the Decision is to correct it to "hello" in English (US)

#### Scenario: Russian typed as English
- **WHEN** the current Layout is English (US) and the user types "ghbdtn"
- **THEN** the Decision is to correct it to "привет" in Russian

### Requirement: Keep valid words
Rekey SHALL keep a Word whose Rendering in the current Layout is a word of the current Language, whatever its other Renderings are.

#### Scenario: Correctly typed word
- **WHEN** the current Layout is English (US) and the user types "hello"
- **THEN** the Decision is to keep it

### Requirement: Keep Ambiguous Words
When Renderings in two or more Layouts are words of their Languages, Rekey SHALL keep the Word as typed.

#### Scenario: Word valid in both Layouts
- **WHEN** a Word's English (US) Rendering and its Russian Rendering are both common words
- **THEN** the Decision is to keep it

### Requirement: Keep unrecognisable Words
When no Rendering scores as plausible text in its Language, Rekey SHALL keep the Word.

#### Scenario: Random letters
- **WHEN** the user types "qzxv" in English (US) and no Rendering is plausible
- **THEN** the Decision is to keep it

### Requirement: Punctuation at the edges of a Word
Characters at the start or end of a Rendering that are not letters SHALL be rendered from their Keystrokes like letters but SHALL be ignored when the Rendering is scored (design decision).

#### Scenario: Exclamation mark after a wrong-layout Word
- **WHEN** the current Layout is English (US) and the user types "ghbdtn!"
- **THEN** the Decision is to correct it to "привет!" in Russian

### Requirement: Keep Words that look like code, numbers or addresses
Rekey SHALL keep a Word containing digits or any of `@ / \ _ : #`, or a Word with an upper-case letter after a lower-case one.

#### Scenario: Email address
- **WHEN** the user types "user@example.com"
- **THEN** the Decision is to keep it

#### Scenario: Identifier
- **WHEN** the user types "getValue"
- **THEN** the Decision is to keep it

### Requirement: Preserve letter case
A correction SHALL produce the Rendering with the same Shift and Caps Lock state per key, so capitalisation follows what the user typed.

#### Scenario: Capitalised word
- **WHEN** the user types "Ghbdtn" in English (US)
- **THEN** the correction text is "Привет"

#### Scenario: Caps Lock on
- **WHEN** the user types "GHBDTN" with Caps Lock on in English (US)
- **THEN** the correction text is "ПРИВЕТ"

### Requirement: Sensitivity levels
Rekey SHALL support three Sensitivity levels. Cautious corrects only on a common-word match and defers otherwise. Balanced (default) corrects on a common-word match and defers when only letter-frequency evidence exists. Aggressive also corrects on strong letter-frequency evidence.

#### Scenario: Rare word under Cautious
- **WHEN** Sensitivity is Cautious and a wrong-layout Word's target Rendering is not a common word
- **THEN** the Decision is to defer it

#### Scenario: Rare word under Aggressive
- **WHEN** Sensitivity is Aggressive and a wrong-layout Word's target Rendering is not a common word but its letter-frequency score clears the Aggressive threshold
- **THEN** the Decision is to correct it

### Requirement: Short words need confirmation
Outside Aggressive Sensitivity, a Word of one or two letters SHALL NOT be corrected on its own; it SHALL be deferred. Under Aggressive, it SHALL be corrected when its target Rendering is a valid short word and its current Rendering is not.

#### Scenario: Short word under Balanced
- **WHEN** Sensitivity is Balanced and the user types "yt" in English (US)
- **THEN** the Decision is to defer it

#### Scenario: Short word under Aggressive
- **WHEN** Sensitivity is Aggressive and the user types "yt" in English (US)
- **THEN** the Decision is to correct it to "не"

### Requirement: Deferred Words are confirmed by the next Word
A deferred Word SHALL be corrected together with the next Word when that Word is corrected to the same target Layout. It SHALL be kept when the next Word is kept or corrected to another Layout. At most 3 Words SHALL wait at once (design decision); the oldest is kept when a fourth is deferred.

#### Scenario: Confirmed run
- **WHEN** Sensitivity is Balanced and the user types "yt" then "pyf.", both in English (US)
- **THEN** both Words are corrected together to "не знаю"

#### Scenario: Not confirmed
- **WHEN** a Word was deferred and the next Word is kept
- **THEN** the deferred Word is kept as typed

### Requirement: Stricter rules between same-script Layouts
Between two Layouts whose Languages share a script (Russian and Ukrainian), Rekey SHALL correct only when the Word contains a key that produces different letters in the two Layouts, the target Rendering is a common word, and the current Rendering is not a word.

#### Scenario: Ukrainian typed in Russian Layout
- **WHEN** the current Layout is Russian and the user types "привыт"
- **THEN** the Decision is to correct it to "привіт" in Ukrainian

#### Scenario: No differing key
- **WHEN** a Word typed in the Russian Layout uses only keys that give the same letters in Ukrainian
- **THEN** the Decision is to keep it

### Requirement: Decision speed
Rekey SHALL reach a Decision within 5 ms for a Word of up to 30 Keystrokes on a typical laptop CPU (design decision).

#### Scenario: Long word
- **WHEN** a Word of 30 Keystrokes is evaluated with three active Languages
- **THEN** the Decision is returned within 5 ms

### Requirement: Measured accuracy
Default thresholds SHALL be chosen so that, on the project's held-out accuracy corpus under Balanced Sensitivity, at most 0.5% of correctly typed Words are corrected and at least 95% of wrong-layout Words are corrected or confirmed through deferral (design decision).

#### Scenario: Accuracy run
- **WHEN** the accuracy harness runs on the held-out corpus with Balanced Sensitivity
- **THEN** it reports a false-correction rate of at most 0.5% and a correction rate of at least 95%
