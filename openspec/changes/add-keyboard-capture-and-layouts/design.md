# Design

## Context

ADR 0002: the engine consumes events and returns actions; this change builds the Win32 adapters that produce those events. The original re-enables its keyboard tap when macOS disables it, runs an independent watchdog thread, clears typing state after a gap of more than 2 s, and adopts the real layout when its own switch does not land.

## Goals / Non-Goals

**Goals:**
- Hook callback that never risks Windows' `LowLevelHooksTimeout`.
- Detection of a silently removed hook.
- Accurate current Layout under both Windows Layout modes (global and per-window).

**Non-Goals:**
- Holding Trigger Keys or injecting text (change `add-text-replacement`).
- Deciding when to correct (change `add-auto-correction`).

## Decisions

### D1. Hook thread
- Dedicated `Thread` (above-normal priority) runs `SetWindowsHookEx(WH_KEYBOARD_LL)` and `SetWindowsHookEx(WH_MOUSE_LL)` and a `GetMessage` loop.
- Keyboard callback: ignore events with `LLKHF_INJECTED` *and* our `dwExtraInfo` tag (`0x52454B59`, "REKY"); copy `(vkCode, scanCode, flags, time)` into a bounded single-producer/single-consumer ring buffer (a preallocated struct array with volatile head and tail; `System.Threading.Channels` is not used because its bounded writer takes a lock and its unbounded one allocates) and signal the worker through an auto-reset event; return `CallNextHookEx`. No allocation, no locks, no logging on this path. Key-ups are copied too, so the worker can track Shift, Ctrl, Alt, Win and AltGr itself. When the buffer is full the event is dropped and counted.
- Mouse callback: only `WM_*BUTTONDOWN` → enqueue `MouseClick`.
- The callback measures its own duration; over 1 ms increments a "slow callback" counter reported as an Incident in aggregate.

### D2. Translation off the hook thread
The worker thread turns raw events into `InputEvent`: decides letter vs boundary using the current Layout's KeyMap (a key is "letter-producing" if it produces a letter in **any** active Layout), tracks Shift/Caps Lock, and timestamps for the 2-second gap rule. The `WordTracker` (current Word) lives in the engine (Core), so the rules in the spec are unit-tested in Core.

### D3. Foreground and focus
- `SetWinEventHook(EVENT_SYSTEM_FOREGROUND)` and `EVENT_OBJECT_FOCUS` (out-of-context) on the worker's own message-loop thread → `FocusChanged(appId, isPassword, isElevated)`. When no foreground window can be read, `FocusLost` instead. The worker dispatches waiting messages before each queued key, so a focus change whose WinEvent has already arrived is applied before the keys typed after it; a WinEvent still in flight is covered by the per-key confirmation below, within its accepted limit.
- `isPassword`: UI Automation `IsPassword` property of the focused element, or the `ES_PASSWORD` style for classic Edit controls; when UI Automation cannot answer (unavailable, failed, timed out after 200 ms) the control counts as a Password Field.
- Per-key confirmation (owner decision): a focus move inside one window (Tab, Shift+Tab, a click, page script) keeps the window handle and reaches Rekey only through an asynchronous WinEvent. So before the engine keeps a character key, the worker asks UI Automation whether the focused control is a Password Field, and the key counts as confirmed only when the answer is "no" and the ring holds nothing written after that key (a later key or click may already have moved focus). Before that check, the worker also compares the focused UI Automation element with the one at the last focus report; a different element means focus moved without a WinEvent yet (including a move made by the page's script), so focus is reported at once with the key's time and the key is dropped. An unconfirmed key is dropped and the rest of its Word is not kept. Cost: a few UI Automation calls per keystroke on the worker, usually a few milliseconds and at most the 200 ms timeout each on a hung app; the hook never waits, so typing is never delayed. Side effect: when the worker falls behind fast typing, that Word gets no Correction.
- Focus events carry the WinEvent's own time, and the engine ignores keys typed before the latest focus change, because the worker may read queued keys after focus has already moved on.
- `isElevated`: `OpenProcessToken` + `GetTokenInformation(TokenElevation)` on the foreground process; compared with Rekey's own elevation.
- `appId`: full image path via `QueryFullProcessImageName`.

### D4. Hook watchdog
- Every 250 ms the watchdog compares `GetLastInputInfo().dwTime` (system-wide last input) with the timestamp of the last event our hook received.
- If system input is newer by > 1,500 ms, no keyboard/mouse event reached us, and the foreground window is not elevated → assume the hook was removed: unhook, reinstall on the hook thread, record Incident `hook-reinstalled`. The elevation check matters because UIPI hides input aimed at an elevated window from a non-elevated process's LL hook, while `GetLastInputInfo` still advances; without it every administrator console would look like a dropped hook.
- Two reinstalls within 10 minutes → emit `SelfPause(until = now + 10 min)` to the engine and tray; after the period, resume automatically. Manual Resume clears the state.
- Mouse movement also updates `GetLastInputInfo`, so the comparison uses our mouse hook too (moves are counted but not enqueued).

### D5. Layout list, current Layout, KeyMaps
- List: `GetKeyboardLayoutList`; KLID via `ActivateKeyboardLayout` + `GetKeyboardLayoutName` is avoided (side effects) — instead read `HKL` low word (language) and high word (device / layout id) and map to KLID through `HKCU\Keyboard Layout\Preload` + `Substitutes` and `HKLM\SYSTEM\CurrentControlSet\Control\Keyboard Layouts`.
- Change detection: `WM_INPUTLANGCHANGE` is not delivered to us for other windows, so poll the list every 2 s (cheap) and diff.
- Current Layout: `GetKeyboardLayout(GetWindowThreadProcessId(GetForegroundWindow()))`, re-read on foreground/focus events and after every Keystroke batch (cheap). Console windows (conhost) report the host's thread; use `GetGUIThreadInfo` focus window when available.
- KeyMap: for each HKL and each printable scan code, `ToUnicodeEx` with a fresh key-state array (no Shift / Shift), flag `0x4` (do not change keyboard state) to avoid corrupting dead-key state in the active window.

### D6. Layout Switch
- `PostMessage(hwndFocus, WM_INPUTLANGCHANGEREQUEST, 0, hkl)` to the focused window (from `GetGUIThreadInfo`), falling back to the foreground window.
- Verify by polling `GetKeyboardLayout` of that thread every 25 ms up to 500 ms; on timeout adopt the real Layout and record Incident `switch-not-landed`.

### D7. Freeze watchdog and self-restart
- A watchdog thread (separate from the hook watchdog in D4) reads a heartbeat counter that only the worker writes after each processed batch (one writer). If input is queued and the heartbeat has not moved for > 5 s, that is a freeze.
- Second freeze within 1 h → record Incident `freeze-restart` (flushed synchronously), start a new Rekey process with `--restarted-after-freeze <pid>` and exit. The new process waits for the old PID to exit before taking the single-instance mutex. The time of the last restart is kept in a small state file next to the settings, so the 10-minute limit survives the restart.
- `add-telemetry` reuses this watchdog's freeze signal for freeze reports instead of running its own.
- Settings `KeyboardWatchdogEnabled` and `RestartAfterFreezeEnabled` (both on, as in the original). With the keyboard watchdog off, D4 does not run. With restart off, freezes are still detected and recorded (telemetry needs them) but never restart the process.

## Risks / Trade-offs

- [`GetLastInputInfo` also counts input from other sessions? No — it is per session; but injected input from other tools also updates it] → only treat as a drop when *our* hook saw nothing for > 1.5 s while the system saw input; other injectors still pass through our hook, so they do not trigger false drops.
- [UI Automation focus queries are slow in some apps (100+ ms)] → done on the worker thread, never on the hook thread, with a 200 ms timeout; the per-key confirmation repeats the query for every character key, so a slow app only delays the worker, and a timeout counts as a Password Field; while focus is unknown (before the first report, or when no foreground window can be read) the engine keeps no Keystrokes (safe default), so a Password Field leaks into a Word only in the accepted focus round-trip case listed below.
- [A page script moves focus into a Password Field and back to the same control within milliseconds of a keystroke, before Windows delivers either WinEvent] → accepted by the owner and documented: the worker sees only the focus after the key, and a focus query in the hook callback is ruled out, so that key may reach the Word until the next focus notice clears it.
- [Per-window Layout mode] → handled by always reading the foreground thread's Layout.
- [`ToUnicodeEx` flag 0x4 needs Windows 10 1607+] → Windows 10/11 only is the supported baseline.
