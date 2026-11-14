# Tasks

## 1. Settings fields and view-models

- [x] 1.1 Add `ShowCursorIndicator` (on), `PlaySwitchSound` (off), `SwitchSound`, `LaunchAtSignIn` to `Settings`; verify defaults and round-trip tests pass
- [x] 1.2 Write failing view-model tests: changing a property publishes a new `Settings` snapshot; debounced save happens once for rapid changes; implement `SettingsViewModel`; verify they pass
- [x] 1.3 Write failing tests for `TrayViewModel` state text (active, paused with minutes left, off, paused itself) and the exclude/include item label for the current app; implement; verify they pass

## 2. Keycap mark and tray

- [x] 2.1 Implement the keycap renderer (light/dark, paused/off/self-paused variants, 16–32 px); verify visually with a test page rendering all variants at 100/150/200% scale (`KeycapTests` checks sizes, states and colors and writes `keycap-sheet.png`; the look by hand is deferred to `docs/manual-tests/deferred-by-hand.md`)
- [ ] 2.2 Implement the tray icon and menu (Auto Correction, Pause presets/Resume, exclude current app, Settings, Statistics, Check for updates, About, Quit); verify manually each item works and the icon shows "RU" after switching to Russian (the "RU" part is the desktop-check set `tray-and-settings`; the menu items by hand are deferred to `docs/manual-tests/deferred-by-hand.md`)

## 3. Settings window

- [ ] 3.1 Add WPF UI; create `FluentWindow` with `NavigationView` and the six sections; single-instance window activation; verify manually that a second "Settings" brings the window forward
- [ ] 3.2 Build General and Correction pages bound to the view-model; verify manually that changing Sensitivity affects the next Word
- [ ] 3.3 Build the Hotkey page with option list, custom recorder and conflict dialog; verify manually with Win+Space (conflict) and Ctrl+Alt+Space (free)
- [ ] 3.4 Build the Apps and words page: Excluded Apps (running apps picker with icons, browse for .exe), Protected Words and Learned Words editors; verify manually add/remove for each list
- [ ] 3.5 Check keyboard-only navigation, visible focus, screen-reader names (Narrator) and dark mode on every page; record results in `docs/manual-tests/settings-accessibility.md`; verify all items pass

## 4. Indicator, sound, sign-in

- [ ] 4.1 Implement the cursor indicator window (no-activate, caret position chain, 900 ms + fade, reduced-motion); verify manually in Notepad (caret) and VS Code (mouse fallback) that typing is not interrupted
- [ ] 4.2 Bundle three sounds, implement preview and playback after Corrections; verify manually that sound plays once per Correction and not on Undo
- [ ] 4.3 Implement launch at sign-in via the `Run` key; verify manually by signing out and in
