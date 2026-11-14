# Tasks

## 1. Hotkey model and detection

- [x] 1.1 Add `Hotkey` (DoubleShift default, DoubleCtrl, DoubleAlt, CtrlSpace, WinSpace, Custom(modifiers, key)) to `Settings`; verify settings defaults and round-trip tests pass
- [x] 1.2 Write failing tests for the double-tap state machine (two taps < 400 ms fire; capital letter in between resets; slow taps do not fire) with a fake clock; implement; verify they pass
- [x] 1.3 Write failing tests for the custom combo state machine (exact modifiers + key fires and swallows the key; extra modifier or other key does not); implement it in the hook; verify the tests pass and manually that the key does not reach Notepad

- [ ] 1.4 Write failing tests for the Win+Space state machine (Space swallowed while Win is down; masking key injected before Win up; Win alone and other Win shortcuts untouched); implement; verify the tests pass and manually that Win+Space corrects "ghbdtn", switches to the next Layout in an empty field, never switches twice and never opens Start

## 2. Manual Correction in the engine

- [x] 2.1 Write failing engine tests for priorities: revert last Correction (only under Undo conditions; not after an Enter-triggered Correction); correct current Word; correct RecentWord after Space ("yt " → "не "); Layout Switch when nothing to correct; implement; verify they pass
- [x] 2.2 Write failing tests for target choice with three Layouts ("ghbdsn" → "привіт"); implement ranking; verify they pass
- [x] 2.3 Write failing tests that the Hotkey works when Auto Correction is off/paused/excluded and does nothing in a Password Field; implement; verify they pass

## 3. Selection conversion

- [x] 3.1 Write failing tests for reverse KeyMap conversion ("ghbdtn vbh" → "привет мир", digits untouched); implement; verify they pass
- [ ] 3.2 Implement UIA selection reading and type-over replacement; verify manually in Notepad and Chrome; verify that an unreadable selection only switches Layout

## 4. Conflicts

- [x] 4.1 Write failing tests for reading the Windows Toggle registry values into a shortcut set (fake registry reader); implement; verify they pass
- [x] 4.2 Implement the `RegisterHotKey` probe for custom combos; verify manually that Win+Space is reported as used by Windows and that a combo registered in another tool is reported as taken
