# app-lifecycle Specification

## Purpose
Defines how Rekey runs as a per-user background process on Windows: start-up without a main window, single-instance guarantee, per-user settings storage, and clean shutdown.

## Requirements

### Requirement: Background start without a main window
The app SHALL start without opening a main window or a taskbar button and SHALL show a notification-area (tray) icon while running.

#### Scenario: Normal start
- **WHEN** the user launches Rekey
- **THEN** a Rekey icon appears in the notification area
- **AND** no window and no taskbar button are shown

### Requirement: Quit from the tray
The tray icon menu SHALL contain a Quit item that ends the process.

#### Scenario: User quits
- **WHEN** the user selects Quit in the tray menu
- **THEN** the tray icon disappears and the Rekey process exits with code 0

### Requirement: Single instance per user session
Only one Rekey process SHALL run per Windows user session. A second launch MUST exit without side effects and leave the running instance untouched.

#### Scenario: Second launch
- **WHEN** Rekey is already running and the user launches it again
- **THEN** the second process exits within 2 seconds (design decision)
- **AND** only one tray icon remains

#### Scenario: Another user session
- **WHEN** Rekey runs in user session A and is launched in user session B
- **THEN** both instances run independently

### Requirement: Per-user settings storage
The app SHALL persist settings per Windows user in a file under `%LOCALAPPDATA%\Rekey`, write it atomically, and keep settings across restarts.

#### Scenario: Settings survive restart
- **WHEN** a setting is changed and Rekey is restarted
- **THEN** the changed value is in effect after restart

#### Scenario: Missing settings file
- **WHEN** Rekey starts and no settings file exists
- **THEN** Rekey starts with default settings and creates the file

### Requirement: Recovery from a corrupt settings file
If the settings file cannot be parsed, the app MUST start with default settings and preserve the unreadable file as a backup instead of deleting it.

#### Scenario: Corrupt settings file
- **WHEN** Rekey starts and the settings file cannot be parsed
- **THEN** Rekey starts with default settings
- **AND** the original file is kept next to it with a `.bak` suffix

### Requirement: Clean shutdown
On Quit, Windows logoff, or shutdown, the app SHALL release every system hook and resource it installed and flush pending settings before exiting.

#### Scenario: Logoff while running
- **WHEN** the user signs out of Windows while Rekey is running
- **THEN** pending settings are written to disk before the process exits
