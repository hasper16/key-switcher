# Proposal

## Why

A tray app that silently rewrites typing is confusing on first contact: users do not know it is running, what it does, or that their text stays on the machine. The original opens a welcome window on first run that explains Rekey, asks for permissions, lets the user try a Correction, and warns when it runs from a temporary location where it will not keep working.

## What Changes

- Welcome window on first run: what Rekey does, that typed text never leaves the computer, a try-it field, the main choices (Auto Correction, Hotkey, launch at sign-in), and how to pin the tray icon.
- Warning when Rekey runs from a temporary or download location instead of its installed location.
- Notice when the app's files were moved or deleted while it runs.
- The welcome window can be reopened from the tray.

## Capabilities

### New Capabilities

- `onboarding`: What a new user sees on first run, how they confirm Rekey works, and how Rekey warns about running from a location where it cannot keep working.

### Modified Capabilities

None.

## Impact

- `Rekey.App`: welcome window and view-model, try-it field, location checks.
- Settings: `WelcomeCompleted`.
- Depends on `add-tray-and-settings-ui` and `add-auto-update-and-installer` (install root and the Install option); the privacy block is filled in by `add-telemetry`.
