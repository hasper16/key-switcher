# layout-management Specification

## Purpose
Defines how Rekey knows which Layouts the user has, which Layout is active in the window being typed into, how each Layout maps keys to characters, and how Rekey switches Layouts reliably.

## Requirements

### Requirement: Installed Layouts are known and kept current
Rekey SHALL know the list of Layouts installed for the user and SHALL update it within 5 seconds (design decision) when the user adds or removes a Layout.

#### Scenario: Layout added
- **WHEN** the user adds the Ukrainian Layout in Windows Settings while Rekey runs
- **THEN** within 5 seconds Rekey includes Ukrainian in its Layout list

### Requirement: Active Layout of the foreground window
Rekey SHALL treat the Layout of the foreground window's input thread as the current Layout and SHALL update it when the user switches Layout or focus moves.

#### Scenario: User switches with the system shortcut
- **WHEN** the user presses the Windows Layout Switch shortcut
- **THEN** Rekey's current Layout becomes the newly selected Layout

#### Scenario: Per-window layouts
- **WHEN** Windows is set to keep a separate Layout per app window and the user moves focus to a window using Russian
- **THEN** Rekey's current Layout becomes Russian

### Requirement: Key-to-character map per Layout
For every installed Layout, Rekey SHALL know which character each printable key produces with and without Shift.

#### Scenario: Russian map
- **WHEN** Rekey builds the map for the Russian Layout
- **THEN** the key that gives "f" in English (US) gives "а", and with Shift gives "А"

### Requirement: Verified Layout Switch
When Rekey switches the Layout of the foreground window, it SHALL confirm that the switch took effect within 500 ms (design decision). If it did not, Rekey SHALL adopt the Layout actually active and record an Incident.

#### Scenario: Switch succeeds
- **WHEN** Rekey switches the foreground window from English (US) to Russian
- **THEN** within 500 ms the window's Layout is Russian and Rekey's current Layout is Russian

#### Scenario: Switch does not land
- **WHEN** Rekey requests a Layout Switch and the window's Layout is unchanged after 500 ms
- **THEN** Rekey's current Layout is set to the window's real Layout
- **AND** an Incident records the failed switch
