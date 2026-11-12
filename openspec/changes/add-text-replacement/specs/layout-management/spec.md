# Spec Delta

## MODIFIED Requirements

### Requirement: Key-to-character map per Layout
For every installed Layout, Rekey SHALL know which character each printable key produces with and without Shift. The printable keys are the 47 keys of a standard ANSI keyboard plus the ISO key between Left Shift and Z (scan code 0x56), which ISO keyboards have and many European Layouts use for characters such as "<" and ">".

#### Scenario: Russian map
- **WHEN** Rekey builds the map for the Russian Layout
- **THEN** the key that gives "f" in English (US) gives "а", and with Shift gives "А"

#### Scenario: ISO key
- **WHEN** Rekey builds the map for the United Kingdom Layout
- **THEN** the ISO key gives "\", and with Shift gives "|"
