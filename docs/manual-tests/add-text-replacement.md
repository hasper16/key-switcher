# Desktop checks: add-text-replacement

The engine's Corrections, runs and Undo rules, the hook's holding rules, the input it injects and the verification decision are covered by unit tests. What needs a real Windows desktop is checked by a tool, plus two short checks by hand. Tick a task in `tasks.md` once all of its checks pass on the commit under review.

## Automated: `tools/Rekey.DesktopChecks text-replacement`

Needs the English (US) and Russian Layouts installed. Close every running Rekey and input probe, then run from the repository root (Debug build, the default):

```bash
dotnet run --project tools/Rekey.DesktopChecks -- text-replacement
```

Do not touch the mouse or keyboard for about 3 minutes. The tool runs Rekey's worker in its own process with a Word Evaluator that corrects only the spec's example Words ("ghbdtn" → "привет", "yt pyf." → "не знаю") from English (US) to Russian. It opens its own test windows, Notepad on an empty file in the temp folder, the local test page (`tools/Rekey.DesktopChecks/check-page.html`, invented fields) in the default browser and a console; it types and reads the text back through UI Automation. It prints one ✅ or ❌ line per check, a summary, and exit code 1 when any check fails. Output carries booleans, lengths, kinds and codes only, never text. At the end it restores Caps Lock and the mouse position, empties and saves the Notepad file, closes its tab, closes any Notepad process the run started (a Notepad you had open keeps running) and deletes the file, and closes its windows, the test page tab and the console. It never reads or writes the clipboard: it compares the clipboard's sequence number before and after.

| Task | What the tool checks |
|---|---|
| 3.2 | In a test window, "ghbdtn" + Space reads "привет " with the space after the Word, and the Layout is then Russian; Space and three letters sent in one burst during the Correction follow it in order |
| 4.1 | The corrected Word, then the Space, in Notepad and a browser text box; in the default console (Windows Terminal) and a classic conhost window the corrected Word, retyped (`Retyped`) (each console check first proves the buffer is read by finding digits typed before the Word); a Deferred run "yt pyf." + Space reads "не знаю " |
| 4.2 | In the test page's chat box, where Enter sends the message, "ghbdtn" + Enter sends "привет"; the clipboard sequence number is unchanged after every Correction and Undo |
| 5.1 | In Notepad and the browser text box the text was checked and selected through UI Automation (`Verified`), not retyped |
| 5.2 | Text before the caret that differs from the Word (letters the hook ignores, as an autocomplete would add) is left alone, with a `CorrectionCancelled` Incident |
| 6.1 | Ctrl+Z 1 s after a Correction restores "ghbdtn " and English (US), is reported as an Undo and does not reach the window; after 6 s, after another letter, or after an Enter-triggered Correction, Ctrl+Z reaches the window and nothing is undone |

`dotnet run --project tools/Rekey.DesktopChecks -- console-probe` (about two minutes, no Rekey corrections) types into the default console and into a classic conhost window, seven ways each, and reports per way whether the corrected Word shows, after proving the buffer read by digits typed first, plus each window's class: Backspaces then Unicode characters in one call, Unicode alone, one call per key, a 150 ms pause after the Backspaces, the keys typed in the Russian Layout, the keys by scan code alone, and erase, verified switch to Russian, retype the keys. It decides how Rekey replaces text in consoles.

Last run: 18 of 18 at f9b57f7 (owner's desktop: Windows Terminal as default console, classic conhost, English (US) and Russian Layouts), with the change-4 run 35 of 35; nothing left running.

If the browser does not expose the page's boxes to UI Automation, the browser lines fail with that reason; make Edge or Chrome the default browser and run again.

## By hand

These need apps the tool cannot drive reliably. The app gets its own Word Evaluator only in `add-auto-correction`, so run the worker with the checks' evaluator, which corrects only the spec's example Words:

```bash
dotnet run --project tools/Rekey.DesktopChecks -- try-corrections
```

It prints one line per replacement (`Verified`, `Retyped`, `Mismatch` or `TooLate`), Undo and Incident, never text. Press Enter in its window to stop it. Use English (US) as the starting Layout.

- [ ] **Electron editor (4.1).** In VS Code, in a plain-text file, type "ghbdtn" and Space.
  Expected: the line reads "привет " and the Layout is Russian; nothing else in the file changed. Ctrl+Z right away gives back "ghbdtn " and English (US).
- [ ] **Chat app with Enter (4.2).** In Telegram Desktop, in Saved Messages, copy some text first, then type "ghbdtn" and press Enter.
  Expected: the sent message reads "привет"; pasting afterwards gives the text copied before.
