# Spec Delta

## Purpose

Defines the recognition data Rekey holds for each Language, how that data is loaded and normalised, and which Languages are active given the user's installed Layouts.

## ADDED Requirements

### Requirement: Bundled Language Profiles
The app SHALL ship a Language Profile for English, Russian and Ukrainian and load them at start-up without network access.

#### Scenario: Start offline
- **WHEN** Rekey starts on a machine with no network connection
- **THEN** the English, Russian and Ukrainian Language Profiles are loaded

### Requirement: Language Profile content
Each Language Profile SHALL contain: a Language code and display name, a list of common words ordered by frequency, the complete set of valid one-letter and two-letter words, relative frequencies of letter pairs and letter triples, and the Windows Layout ids that type the Language.

#### Scenario: Short-word lists
- **WHEN** the Russian Language Profile is loaded
- **THEN** "я" is a valid one-letter word and "не" is a valid two-letter word

#### Scenario: Layout ids
- **WHEN** the Ukrainian Language Profile is loaded
- **THEN** it lists the Layout ids for "Ukrainian" and "Ukrainian (Enhanced)"

### Requirement: Active Languages follow installed Layouts
A Language SHALL be active only while at least one of its Layouts is installed for the current user. Words SHALL only be evaluated between active Languages.

#### Scenario: Ukrainian layout not installed
- **WHEN** the user has only English (US) and Russian Layouts installed
- **THEN** English and Russian are active and Ukrainian is inactive

#### Scenario: Layout added while running
- **WHEN** the user installs a Ukrainian Layout while Rekey is running
- **THEN** Ukrainian becomes active without restarting Rekey

### Requirement: Layouts without a Language Profile are left alone
A Layout not listed by any Language Profile SHALL NOT be a source or target of a Correction.

#### Scenario: Unsupported layout
- **WHEN** the active Layout is German and German has no Language Profile
- **THEN** Rekey makes no Correction from or to the German Layout

### Requirement: Normalised word lookup
Word lookup SHALL ignore letter case, SHALL treat Russian "ё" as "е", and SHALL treat the apostrophe variants `'`, `’` and `ʼ` as the same character.

#### Scenario: Case and ё
- **WHEN** the Word "ЕЩЁ" is looked up in the Russian Language Profile
- **THEN** it matches the common word "еще"

#### Scenario: Ukrainian apostrophe
- **WHEN** the Word "п’ять" (typographic apostrophe) is looked up in the Ukrainian Language Profile
- **THEN** it matches "п'ять"

### Requirement: Invalid profile does not stop the app
If a Language Profile cannot be read or fails validation, the app MUST record an Incident, skip that Language, and continue with the remaining Languages.

#### Scenario: Corrupt profile
- **WHEN** the Ukrainian Language Profile is corrupt
- **THEN** Rekey starts with English and Russian active
- **AND** an Incident names the Ukrainian Language Profile

### Requirement: Reproducible profile generation
The profile generator SHALL produce byte-identical Language Profiles from the same source lists and settings, and SHALL record each source and its licence.

#### Scenario: Rebuild
- **WHEN** the generator runs twice on the same inputs
- **THEN** both outputs are byte-identical

#### Scenario: Source record
- **WHEN** the generator finishes
- **THEN** a sources file lists every input list with its origin and licence

#### Scenario: Unlisted source
- **WHEN** a file in the sources folder is missing from the sources file
- **THEN** the generator stops without writing any Language Profile
