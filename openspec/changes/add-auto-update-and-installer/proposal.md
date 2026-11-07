# Proposal

## Why

Users need a normal way to install Rekey, and fixes must reach them without manual downloads. An app that observes every keystroke is a high-value target, so updates must be authentic. The original uses Sparkle with automatic checks and an EdDSA-signed feed.

## What Changes

- Per-user installer (no administrator rights) that installs Rekey, adds a Start menu entry and an uninstaller.
- Automatic update checks in the background, a "Check for updates" tray item, download and apply on restart.
- Updates accepted only over HTTPS, matching the release feed checksum, and carrying a valid Authenticode signature from the Rekey publisher; no downgrades.
- Signed binaries and installer; release pipeline in CI.
- Uninstall removes the app, sign-in entry and, if the user chooses, all Rekey data.

## Capabilities

### New Capabilities

- `auto-update`: How Rekey is installed, how it finds, verifies and applies updates, and how it is uninstalled.

### Modified Capabilities

None.

## Impact

- New dependency: Velopack (runtime + `vpk` CLI in CI).
- CI: release workflow building, signing (`--signParams`), packing and uploading to GitHub Releases (`vpk upload github`).
- Settings: `AutoCheckForUpdates` (on).
- Code-signing certificate needed (procurement outside this change).
- Depends on `bootstrap-solution` and `add-tray-and-settings-ui`.
