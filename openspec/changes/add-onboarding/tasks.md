# Tasks

## 1. First-run logic

- [ ] 1.1 Add `WelcomeCompleted` to `Settings`; write failing tests that the welcome opens only when it is false and closing sets it; implement; verify they pass
- [ ] 1.2 Write failing tests for the location classifier (temp, zip segment, Downloads, installed, debug build); implement; verify they pass

## 2. Welcome window

- [ ] 2.1 Build the welcome window (keycap, description, privacy statement, choices, tray tip, Done) with WPF UI; verify manually in light and dark mode and with keyboard only
- [ ] 2.2 Implement the try-it field with success confirmation via `CorrectionApplied`; verify manually that "ghbdtn" + Space shows "привет " and the confirmation
- [ ] 2.3 Implement the single-Layout explanation with a link to Windows language settings (`ms-settings:regionlanguage`); verify manually with only one Layout installed
- [ ] 2.4 Add "Welcome and setup" to the tray menu; verify manually it reopens the window
- [ ] 2.5 Show a one-time toast after the first real Correction; verify manually it appears once

## 3. Location warnings

- [ ] 3.1 Show the temporary-location warning with an Install option at start-up; verify manually by running from inside a ZIP in Explorer
- [ ] 3.2 Implement the executable-disappearance watcher and notice; verify manually by renaming the running executable's folder copy (notice within 60 s)
