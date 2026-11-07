# Design

## Context

The original has a menu-bar quick-settings menu, a settings window, a badge near the cursor, a sound picker, a running-apps picker and excluded-apps editor, a hotkey picker, launch at login, and a pause that shows the minutes left. Engine settings already exist from earlier changes; this change adds views.

## Goals / Non-Goals

**Goals:**
- A settings experience that feels native to Windows 11 and stays out of the way.
- One memorable visual element tied to the subject: the keycap.

**Non-Goals:**
- Statistics window and diagnostics pages (`add-statistics-and-diagnostics`).
- Welcome flow (`add-onboarding`).
- UI translation (`add-ui-localization`); strings go into resources from the start.

## Decisions

### D1. Visual direction
- Rekey is a system utility, so its windows blend in: Windows 11 Fluent, Mica backdrop, Segoe UI Variable, system accent color for interactive states. No custom palette in windows.
- The one bold element is the **keycap**: the tray icon and the cursor badge are drawn as a small keyboard key with the Language code ("EN", "RU", "UK") in Segoe UI Variable Display Semibold. The same keycap appears in the About page and the welcome window, so the product has one recognisable mark.
- Keycap tokens:
  - Key face light `#FAFAFA`, edge `#C9CDD3`, legend `#1F2328`.
  - Key face dark `#2D3036`, edge `#4A4F57`, legend `#F2F4F7`.
  - Paused: legend stays, edge becomes amber `#C98A00`. Off: legend at 45% opacity. Paused itself: edge `#C42B1C` with a small dot.
- Copy: sentence case, plain verbs, names users understand ("Turn off in Telegram", not "Exclude process"). No all-caps labels.

### D2. Libraries
- WPF UI (`lepoco/wpfui`) for `FluentWindow`, `NavigationView`, `ToggleSwitch`, Mica. Built-in WPF `ThemeMode` (Fluent) is still experimental (`WPF0001`) and lacks navigation; revisit when it is stable.
- H.NotifyIcon.Wpf (from bootstrap) for the tray icon; icon images are rendered at runtime from the keycap drawing for each DPI (16/20/24/32 px), so the Language code is always crisp.

### D3. Settings window layout
```
┌──────────────────────────────────────────────┐
│ ⌨ Rekey settings                       – □ × │
├──────────────┬───────────────────────────────┤
│ General      │ Correction                    │
│ Correction ◀ │  Correct wrong-layout words   │
│ Hotkey       │  automatically        [ on ]  │
│ Apps & words │  Sensitivity  (•) Balanced    │
│ Diagnostics  │               ( ) Cautious    │
│ About        │               ( ) Aggressive  │
│              │  Correct when I press         │
│              │   [x] Space [x] Enter [ ] Tab │
│              │  Don't correct after I switch │
│              │  layout myself         [ on ] │
│              │  Check text before replacing  │
│              │                        [ on ] │
└──────────────┴───────────────────────────────┘
```
Left-aligned, single column per page, 560×520 default size, resizable. Each Sensitivity option has one line of plain explanation under it.

### D4. MVVM and data flow
`SettingsViewModel` (CommunityToolkit.Mvvm) edits a mutable copy; every property change produces a new immutable `Settings` snapshot sent to the engine thread and to `SettingsStore` (debounced 500 ms). One source of truth: the store; the engine and UI subscribe.

### D5. Cursor indicator
- Borderless, topmost, `WS_EX_NOACTIVATE | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW` window, created once and reused.
- Caret position: `GetGUIThreadInfo(rcCaret)` → screen coords; else UIA caret range bounding rectangle (from text-replacement's UIA helper, with 30 ms budget); else mouse position.
- Shows 900 ms, fades out 150 ms; respects "reduce animations" (`SPI_GETCLIENTAREAANIMATION`) by skipping the fade.

### D6. Sounds
Three short WAVs bundled (key click, soft tick, chime), played with `System.Media.SoundPlayer` preloaded; no sound on Undo.

### D7. Launch at sign-in
`HKCU\Software\Microsoft\Windows\CurrentVersion\Run\Rekey` = quoted path to the Velopack stub (stable across updates). The setting reflects the registry value on start (user may remove it with Task Manager → Startup apps).

## Risks / Trade-offs

- [WPF UI version churn] → pin version in `Directory.Packages.props`; UI tests are manual checklists.
- [Caret position unknown in many apps (Electron, Java)] → mouse-position fallback; the badge is a convenience, not a requirement for correctness.
- [Tray icon hidden in the overflow area by default on Windows 11] → welcome window tells users how to pin it (add-onboarding).
