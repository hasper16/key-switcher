# Desktop checks: add-auto-correction

The gate, the Trigger Keys, the user's Layout Switch rule, the Pause and the Word counters are covered by unit tests in `Rekey.Core.Tests`; the Trigger Keys the hook holds and the app identity of a frame host window by unit tests in `Rekey.Win32.Tests`. What needs a real Windows desktop is checked by a tool, plus a short checklist by hand for the app itself. Tick a task in `tasks.md` once all of its checks pass on the commit under review.

## Automated: `tools/Rekey.DesktopChecks auto-correction`

Needs the English (US) and Russian Layouts installed. Close every running Rekey, input probe and Calculator, then run from the repository root (Debug build, the default):

```bash
dotnet run --project tools/Rekey.DesktopChecks -- auto-correction
```

Do not touch the mouse or keyboard for about 4 minutes. The tool runs Rekey's worker in its own process with no Word Evaluator of its own, so Words are evaluated as in the app, and changes the Auto Correction settings in memory while it runs; the settings file is not touched. It types the spec's example Words ("ghbdtn", and "hello" in the Russian Layout) and one invented Word into its own test window and into Notepad on an empty file in the temp folder, and reads the text back through UI Automation. It starts Calculator and only looks at it. It prints one ✅ or ❌ line per check, a summary, and exit code 1 when any check fails. Output carries booleans, counts, kinds and codes only, never text. At the end it restores Caps Lock and the mouse position, closes Calculator, empties and closes the Notepad file as the `text-replacement` checks do, and closes its windows. It never reads or writes the clipboard: it compares the clipboard's sequence number before and after.

| Task | What the tool checks |
|---|---|
| 2.1 | "ghbdtn" + Space reads "привет " and the Layout is Russian; Tab leaves the Word as typed by default and is not held; Enter and the numeric keypad Enter correct it; with Tab enabled the Word is corrected and the Tab moves focus on after it; with Enter disabled the Word is left as typed; turned off, the Word is left as typed and Space is not held |
| 2.3 | During a Pause of 8 seconds the Word is left as typed and Space is not held; after it, with nothing told to the worker, the next Word is corrected |
| 3.1 | With the test window's program as an Excluded App, the Word there is left as typed and Space is never held (no added latency), while Notepad is still corrected |
| 3.2 | With Notepad excluded by the identity the worker reported for it, the Word in Notepad is left as typed and the test window is corrected; the line says whether that identity is a packaged one. Calculator, whose window belongs to the frame host process, is identified by its own application user model id and path; excluding it turns the gate to `ExcludedApp` there and the test window is still corrected |
| 3.3 | A Layout request sent to the focused window the way the language bar sends it is reported as the user's; a Word begun in Russian and finished after that switch is left as typed; with "Don't correct after I switch Layout" off the same Word is evaluated; a Word typed right after such a switch is left alone and the next one corrected; after Rekey's own switch to Russian, "hello" typed there is corrected back, so Rekey's switch is not taken for the user's |
| 4.1 | Self-pause (the hook is dropped twice, as in the change-4 checks): the Word is left as typed and Space is not held; after Resume the next Word is corrected. Typing speed (design D8): 20 invented Words at 120, 60, 30 and 10 ms between keys and with each Word sent in one burst; each line gives Words ended, evaluated, dropped for an unconfirmed key, and the unconfirmed keys by cause. These lines fail only when Words go missing altogether: the counts are the measurement |

Last run: 27 of 27 at a8cdcf0 (owner's desktop: Firefox as default browser, Windows Terminal as default console, English (US) and Russian Layouts; product code unchanged since 7e86ff8), with the `text-replacement` run 19 of 19 and the change-4 run 36 of 36 twice in a row on the same commit; no test tab left, Layout list unchanged, nothing left running. Notepad was identified by a packaged identity and Calculator by its own application user model id and path.

Typing speed (design D8), 20 invented Words each: at 120, 60, 30 and 10 ms between keys all 20 were evaluated, none dropped and no key unconfirmed. With each Word sent in one burst all 20 were dropped: 100 keys unconfirmed because a later key was already waiting, none during the check and none for a Password Field. So the confirmation rule costs no Words at typing speeds down to 10 ms between keys, and costs every Word whose keys arrive faster than the worker reads them, as injected or pasted-by-typing text does.

Earlier runs that day failed lines of the change-4 set on this branch and on `main`'s code alike: the browser password box kept Word keys while a test page from an earlier run was still open, and "typed keys build the Word" and the per-window Layout lines failed in some runs. The checks now refuse a leftover test page, prove that their clicks reach the page's own boxes and that a click brought its window to the front, and print what the worker saw of focus. The password lines then passed with a Password Field focused in 10 of 10 rounds. The cause of the "typed keys build the Word" failures was not found; they did not recur.

## By hand

The app has no settings window before `add-tray-and-settings-ui`, so the options are changed in `%LOCALAPPDATA%\Rekey\settings.json` while Rekey is not running (Rekey writes the file on exit). Use a Debug build of `src/Rekey.App` and English (US) as the starting Layout. Whenever a step switches to English (US) before a new Word, wait 3 seconds before typing it: Rekey leaves alone the Word typed right after a Layout Switch by the user. The second check tests exactly that, so there the switch comes in the middle of a Word and the typing goes on at once.

- [x] **Default Corrections (4.2).** Start Rekey with no settings file. In Notepad type "ghbdtn" and Space; switch back to English (US) and wait 3 seconds, then on a new line type "ghbdtn" and Enter; switch back to English (US) again and wait 3 seconds, then type "ghbdtn" and Tab.
  Expected: the first two read "привет" with the Space or the line break after it and the Layout becomes Russian each time; after Tab the text still reads "ghbdtn". The settings file then holds `"triggerKeys": "space, enter"`.
- [x] **The Windows Layout Switch shortcut (4.2).** In Notepad, in Russian, type "ghb", press the Windows shortcut that switches to English (Win+Space or Alt+Shift), type "dtn" and Space. Then type "ghbdtn" and Space again.
  Expected: the first Word stays as typed ("приdtn "); the second is corrected to "привет ".
- [x] **Excluded App (4.2).** Quit Rekey. In the settings file set `"excludedApps": ["Microsoft.WindowsTerminal_8wekyb3d8bbwe!App"]`, start Rekey, and type "ghbdtn" and Space at a prompt in Windows Terminal and then in Notepad, in each window switching to English (US) first if needed and waiting 3 seconds.
  Expected: Windows Terminal keeps "ghbdtn "; Notepad shows "привет ".
- [x] **Pause across a restart (4.2).** Quit Rekey. In the settings file set `"pausedUntil"` to a UTC time three minutes ahead, such as `"2026-11-13T10:15:00+00:00"`, and empty `"excludedApps"`. Start Rekey, switch Notepad to English (US) and wait 3 seconds, type "ghbdtn" and Space in Notepad, quit Rekey after about a minute, start it again and type the Word again; then wait until the time has passed and type it once more.
  Expected: the first two times the text stays "ghbdtn " (the Pause survived the restart); after the time has passed it becomes "привет " without anything else being done.
- [x] **Words lost to unconfirmed keys in real typing (4.2, design D8).** Use the Debug build for a normal working session, then quit Rekey from the tray and open `%LOCALAPPDATA%\Rekey\word-counts.txt`.
  Expected: one line of three counters, `words-ended`, `evaluated` and `unconfirmed`; report them. The share of `unconfirmed` in `words-ended` is how many Words got no evaluation because a key could not be confirmed outside a Password Field.

By hand, last run: all five items passed at 1aad82d on 2026-11-14 on the owner's desktop (product code unchanged since 7e86ff8). The counters file read `words-ended=1 evaluated=1 unconfirmed=0`: a very short session, not a working day, so it shows only that the file is written. The measurement of record for design D8 is the typing-speed run above.
