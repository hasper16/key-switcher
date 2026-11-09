# wrong-layout-detection Specification

## Purpose
Decides, for each Word the user finishes typing, whether it was typed in the wrong Layout, which Layout and Rendering it should become, and when Rekey must wait for more evidence instead of acting.

## Requirements

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
Rekey SHALL keep a Word whose Rendering in the current Layout is a word of the current Language when no Rendering in another active Layout is a word of its Language.

#### Scenario: Correctly typed word
- **WHEN** the current Layout is English (US) and the user types "hello"
- **THEN** the Decision is to keep it

### Requirement: Ambiguous Words wait for the next Word
When Renderings in two or more Layouts, the current one included, are different words of their Languages, Rekey SHALL defer the Word towards each other Layout whose Rendering is a word and SHALL NOT correct it on its own (design decision: the next Word shows which Layout the user is typing in).

#### Scenario: Word valid in both Layouts
- **WHEN** a Word's English (US) Rendering and its Russian Rendering are both common words and the next Word is kept
- **THEN** the Word is kept as typed

#### Scenario: Word valid in two other Layouts
- **WHEN** the current Layout is English (US), the Word's Russian and Ukrainian Renderings are different words, and the next Word is corrected to Ukrainian
- **THEN** both Words are corrected to their Ukrainian Renderings

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
Rekey SHALL keep a Word containing digits, a Word containing any of `@ / \ _ : #` with another character on each side, or a Word with an upper-case letter that follows a lower-case one and is not the last character. Punctuation at the edges of a Word does not make it look like code (design decision: the Russian period, comma and quote keys type `/`, `?` and `@` in English (US)).

#### Scenario: Email address
- **WHEN** the user types "user@example.com"
- **THEN** the Decision is to keep it

#### Scenario: Identifier
- **WHEN** the user types "getValue"
- **THEN** the Decision is to keep it

#### Scenario: Sentence end typed in the wrong Layout
- **WHEN** the current Layout is English (US) and the user types "ltkf/"
- **THEN** the Decision is to correct it to "дела." in Russian

### Requirement: Preserve letter case
A correction SHALL produce the Rendering with the same Shift and Caps Lock state per key, so capitalisation follows what the user typed.

#### Scenario: Capitalised word
- **WHEN** the user types "Ghbdtn" in English (US)
- **THEN** the correction text is "Привет"

#### Scenario: Caps Lock on
- **WHEN** the user types "GHBDTN" with Caps Lock on in English (US)
- **THEN** the correction text is "ПРИВЕТ"

### Requirement: Sensitivity levels
Rekey SHALL support three Sensitivity levels; they apply only when no Keep rule applies. Cautious corrects only on a common-word match and defers otherwise. Balanced (default) corrects on a common-word match and defers when only letter-frequency evidence exists. Aggressive also corrects on strong letter-frequency evidence.

#### Scenario: Rare word under Cautious
- **WHEN** Sensitivity is Cautious and a wrong-layout Word's target Rendering is plausible text but not a common word
- **THEN** the Decision is to defer it

#### Scenario: Rare word under Aggressive
- **WHEN** Sensitivity is Aggressive and a wrong-layout Word's target Rendering is not a common word but its letter-frequency score clears the Aggressive threshold
- **THEN** the Decision is to correct it

### Requirement: Short words need confirmation
Outside Aggressive Sensitivity, when no Keep condition applies, a Word of one or two letters SHALL NOT be corrected on its own; it SHALL be deferred. Under Aggressive, it SHALL be corrected when its target Rendering is a valid short word and its current Rendering is not.

#### Scenario: Short word under Balanced
- **WHEN** Sensitivity is Balanced and the user types "yt" in English (US)
- **THEN** the Decision is to defer it

#### Scenario: Short word under Aggressive
- **WHEN** Sensitivity is Aggressive and the user types "yt" in English (US)
- **THEN** the Decision is to correct it to "не"

### Requirement: Deferred Words are confirmed by the next Word
The deferred Words waiting together SHALL be corrected with the next Word when that Word is corrected to a Layout in which every waiting Word reads as plausible text; each is then typed as it reads in that Layout. They SHALL be kept when the next Word is kept or corrected to a Layout in which any of them does not read as plausible text. A newly deferred Word that shares no such Layout with the waiting Words SHALL leave them as typed and start a new run. At most 3 Words SHALL wait at once (design decision); the oldest is kept when a fourth is deferred.

#### Scenario: Confirmed run
- **WHEN** Sensitivity is Balanced and the user types "yt" then "pyf.", both in English (US)
- **THEN** both Words are corrected together to "не знаю"

#### Scenario: Not confirmed
- **WHEN** a Word was deferred and the next Word is kept
- **THEN** the deferred Word is kept as typed

#### Scenario: Mixed run
- **WHEN** one deferred Word reads as plausible text only in Russian, a second reads as plausible text in Russian and Ukrainian, and the next Word is corrected to Ukrainian
- **THEN** both deferred Words are kept as typed and only the next Word is corrected

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
Default thresholds SHALL be chosen so that, on the project's held-out accuracy corpus under Balanced Sensitivity, at most 0.5% of correctly typed Words are corrected and at least 95% of wrong-layout Words end up as the intended text, directly or through deferral (design decision). A wrong-layout Word counts only when its Rendering in the Layout it was typed in differs from the intended text.

#### Scenario: Accuracy run
- **WHEN** the accuracy harness runs on the held-out corpus with Balanced Sensitivity
- **THEN** it reports a false-correction rate of at most 0.5% and a correction rate of at least 95%
