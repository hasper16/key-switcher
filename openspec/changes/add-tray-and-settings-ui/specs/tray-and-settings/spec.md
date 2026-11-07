# Spec Delta

## Purpose

Defines how users see what Rekey is doing and control it: the tray icon and menu, the settings window, the cursor indicator, the switch sound, and launch at sign-in.

## ADDED Requirements

### Requirement: Tray icon shows state and Language
The tray icon SHALL show the current Language code of the active Layout and SHALL look distinct for each state: active, paused, off, and paused itself after keyboard failures. Its tooltip SHALL state the state in words, including remaining pause time.

#### Scenario: Paused
- **WHEN** Auto Correction is paused for 30 minutes
- **THEN** the tray icon shows the paused look and the tooltip says how many minutes remain

#### Scenario: Layout changes
- **WHEN** the user switches from English (US) to Russian
- **THEN** the tray icon shows "RU"

### Requirement: Tray menu
Clicking the tray icon SHALL open a menu with: Auto Correction on/off, Pause (15 minutes, 30 minutes, 1 hour, 3 hours, until restart; presets are a design decision) or Resume, an item to exclude or include the current app by name, Settings, Statistics, Check for updates, About, and Quit.

#### Scenario: Exclude current app from the tray
- **WHEN** the user is typing in Telegram and selects "Turn off in Telegram" from the tray menu
- **THEN** Telegram becomes an Excluded App
- **AND** the same item reads "Turn on in Telegram" next time

### Requirement: Settings window
The settings window SHALL offer every user setting of Rekey grouped into General, Correction, Hotkey, Apps and words, Diagnostics and privacy, and About. Changes SHALL take effect immediately without a Save button.

#### Scenario: Change Sensitivity
- **WHEN** the user selects Cautious in the Correction section
- **THEN** the next Word is evaluated with Cautious Sensitivity

#### Scenario: Reopen settings
- **WHEN** the settings window is already open and the user selects Settings again
- **THEN** the existing window comes to the front instead of a second window

### Requirement: Excluded Apps editor
The Apps and words section SHALL let the user add Excluded Apps from running applications or by choosing an executable, and remove them.

#### Scenario: Add from running apps
- **WHEN** the user picks Windows Terminal from the running apps list
- **THEN** Windows Terminal is listed as an Excluded App with its name and icon

### Requirement: Hotkey picker with conflict warning
The Hotkey section SHALL let the user choose a Hotkey option or record a custom combination, and SHALL show the conflict warning from Manual Correction before applying a conflicting combination.

#### Scenario: Record a custom Hotkey
- **WHEN** the user records Ctrl+Alt+Space and it is free
- **THEN** Ctrl+Alt+Space becomes the Hotkey

#### Scenario: Record a conflicting Hotkey
- **WHEN** the user records Win+Space
- **THEN** the conflict warning is shown before the Hotkey changes

### Requirement: Cursor indicator
When enabled (default on, design decision), after each Correction or Layout Switch made by Rekey the app SHALL show a small badge with the new Language code next to the text caret for about one second (design decision), without taking focus. When the caret position is unknown, the badge SHALL appear next to the mouse pointer.

#### Scenario: After a Correction
- **WHEN** a Correction switches to Russian
- **THEN** a badge reading "RU" appears next to the caret and disappears after about one second
- **AND** typing continues in the same window without interruption

### Requirement: Switch sound
When enabled (default off, design decision), Rekey SHALL play the chosen sound after each Correction and not after an Undo. The user SHALL be able to choose among at least three sounds (design decision) and preview them.

#### Scenario: Sound on
- **WHEN** the switch sound is on and a Correction happens
- **THEN** the chosen sound plays once

### Requirement: Launch at sign-in
The user SHALL be able to turn launch at sign-in on or off. When on, Rekey SHALL start automatically after the user signs in to Windows.

#### Scenario: Sign in
- **WHEN** launch at sign-in is on and the user signs in
- **THEN** Rekey is running and its tray icon is visible

#### Scenario: Removed in Task Manager
- **WHEN** the user disables Rekey under Startup apps in Task Manager
- **THEN** the settings window shows launch at sign-in as off

### Requirement: Accessible and theme-aware UI
All windows SHALL follow the Windows light/dark theme, SHALL be fully usable with the keyboard alone, and SHALL expose names to screen readers for every control.

#### Scenario: Dark mode
- **WHEN** Windows uses dark mode
- **THEN** the settings window and the tray menu use dark colors

#### Scenario: Keyboard only
- **WHEN** the user navigates the settings window with Tab and arrow keys only
- **THEN** every control can be reached and changed, and focus is always visible
