# Proposal

## Why

Every behavior so far is controlled by settings with no way for users to see or change them. The original lives in the macOS menu bar with a quick-settings menu, a settings window, a cursor badge showing the new layout, an optional switch sound, and launch at login. Windows users need the same from the notification area.

## What Changes

- Tray icon that shows Rekey's state (active, paused, off, paused itself) and the current Language.
- Tray menu: Auto Correction on/off, Pause with presets and Resume, exclude the current app, Settings, Statistics, Check for updates, About, Quit.
- Settings window with sections General, Correction, Hotkey, Apps and words, Diagnostics and privacy, About. Changes apply immediately.
- Cursor indicator: a keycap-style badge near the caret showing the new Language after a Correction or Layout Switch.
- Optional switch sound with a choice of sounds.
- Launch at sign-in.

## Capabilities

### New Capabilities

- `tray-and-settings`: How users see Rekey's state and control it through the tray icon, the tray menu, the settings window, the cursor indicator, sounds and launch at sign-in.

### Modified Capabilities

None.

## Impact

- `Rekey.App`: tray view-model, settings window (MVVM), indicator overlay window, sound player.
- New dependency: WPF UI (lepoco/wpfui) for Fluent controls and navigation.
- Settings fields: `ShowCursorIndicator`, `PlaySwitchSound`, `SwitchSound`, `LaunchAtSignIn`.
- Depends on `add-auto-correction`, `add-manual-correction` and `add-word-learning`; Statistics and Check for updates menu items are wired in `add-statistics-and-diagnostics` and `add-auto-update-and-installer`.
