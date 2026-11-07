# Spec Delta

## Purpose

Defines how Rekey is installed for a Windows user, how it finds, verifies and applies updates without exposing users to tampered builds, and how it is removed.

## ADDED Requirements

### Requirement: Per-user installation
The installer SHALL install Rekey for the current Windows user without administrator rights, add a Start menu entry and an entry in Windows "Installed apps", and start Rekey when done.

#### Scenario: Standard user installs
- **WHEN** a user without administrator rights runs the installer
- **THEN** Rekey is installed, appears in the Start menu and Installed apps, and is running

### Requirement: Automatic update checks
When automatic checks are on (default), Rekey SHALL check for updates at start and every 24 hours (design decision). The tray menu item "Check for updates" SHALL check immediately and report the result.

#### Scenario: No update
- **WHEN** the user selects "Check for updates" and Rekey is current
- **THEN** Rekey says it is up to date

#### Scenario: Update available
- **WHEN** a newer version exists
- **THEN** Rekey downloads it in the background and offers "Restart to update"

### Requirement: Only authentic updates are applied
Rekey SHALL download update feeds and packages only over HTTPS from the project's GitHub Releases, fixed in the app, and SHALL apply a package only if its checksum matches the feed and its executables carry a valid Authenticode signature from the Rekey publisher. Otherwise the update MUST be discarded and an Incident recorded.

#### Scenario: Tampered package
- **WHEN** a downloaded package's checksum does not match the feed
- **THEN** the package is discarded and the current version keeps running

#### Scenario: Wrong signer
- **WHEN** a package's checksum matches but its executable is unsigned or signed by another publisher
- **THEN** the package is discarded and an Incident records the signature failure

### Requirement: No downgrades
Rekey SHALL NOT apply a package whose version is lower than or equal to the running version.

#### Scenario: Older version in feed
- **WHEN** the feed lists a version lower than the running one as latest
- **THEN** Rekey does not install it

### Requirement: Update preserves user data
Applying an update SHALL keep settings, word lists, statistics and launch-at-sign-in state.

#### Scenario: After update
- **WHEN** Rekey restarts into a new version
- **THEN** all settings and Protected Words are unchanged

### Requirement: Clean uninstall
Uninstalling SHALL remove the app files, the Start menu entry and the sign-in entry, and SHALL ask whether to delete Rekey's data (settings, word lists, statistics, logs); data is kept if the user declines.

#### Scenario: Uninstall with data removal
- **WHEN** the user uninstalls and chooses to delete data
- **THEN** `%LOCALAPPDATA%\Rekey` no longer exists and Rekey no longer starts at sign-in

#### Scenario: Uninstall keeping data
- **WHEN** the user uninstalls and declines to delete data
- **THEN** settings and word lists are still in `%LOCALAPPDATA%\Rekey` and a later reinstall uses them
