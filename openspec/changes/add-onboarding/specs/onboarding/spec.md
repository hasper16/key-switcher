# Spec Delta

## Purpose

Defines the first-run experience: explaining what Rekey does and how it treats typed text, letting the user see a Correction work, and warning when Rekey runs from a place where it will not keep working.

## ADDED Requirements

### Requirement: Welcome window on first run
On the first start for a Windows user, Rekey SHALL open a welcome window that explains in plain words what Rekey does, states that typed text never leaves the computer, and shows how to undo a Correction.

#### Scenario: First start
- **WHEN** Rekey starts for the first time for this user
- **THEN** the welcome window opens

#### Scenario: Later starts
- **WHEN** the user completed or closed the welcome window before and Rekey starts again
- **THEN** no welcome window opens

### Requirement: Try it
The welcome window SHALL contain a text field where the user can type a word in the wrong Layout and see Rekey correct it, with a short instruction naming their installed Layouts.

#### Scenario: Successful try
- **WHEN** the user types "ghbdtn" followed by Space in the try-it field with English (US) active
- **THEN** the field shows "привет "
- **AND** the welcome window confirms that Rekey works

#### Scenario: Only one Layout installed
- **WHEN** the user has only one Layout installed
- **THEN** the welcome window explains that Rekey needs at least two Layouts and links to Windows language settings

### Requirement: Main choices up front
The welcome window SHALL let the user set Auto Correction on or off, choose the Hotkey, and turn launch at sign-in on or off (on by default, design decision), and SHALL show how to keep the tray icon visible.

#### Scenario: Launch at sign-in default
- **WHEN** the user closes the welcome window without changing anything
- **THEN** launch at sign-in is on

### Requirement: Reopen from tray
The user SHALL be able to reopen the welcome window from the tray menu at any time.

#### Scenario: Reopen
- **WHEN** the user selects "Welcome and setup" in the tray menu
- **THEN** the welcome window opens with current settings

### Requirement: Warn about temporary locations
When Rekey runs from a temporary folder, a ZIP preview, or the Downloads folder rather than its installed location, it SHALL warn that it will stop working when that copy is removed and SHALL offer to install it properly.

#### Scenario: Run from a ZIP preview
- **WHEN** the user starts Rekey directly from inside a ZIP file opened in Explorer
- **THEN** Rekey shows the warning with an Install option

### Requirement: Notice when the app files disappear
When the app's own executable is moved or deleted while Rekey runs, Rekey SHALL tell the user that it will stop after the next restart and how to reinstall.

#### Scenario: Cleanup tool removed the app
- **WHEN** the Rekey executable is deleted while Rekey runs
- **THEN** Rekey shows a notice within one minute (design decision) explaining how to reinstall
