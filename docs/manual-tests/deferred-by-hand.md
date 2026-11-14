# Deferred checks by hand

By the owner's decision of 2026-11-14, the checks that need a person at the keyboard are done once, in a final acceptance pass after all roadmap changes, together with any fixes they lead to. Each change adds its section here instead of waiting for them; its own `docs/manual-tests/<change>.md` keeps the automated checks and points here. Each section names the commit its steps were written against; run them on the final build, and note where behaviour changed since.

## add-manual-correction

Written against f4ec2f6 (PR #17). The automated `manual-correction` run passed on that commit (run 7); these steps cover what injected keys cannot.

The app has no settings window before `add-tray-and-settings-ui`, so the Hotkey is changed in `%LOCALAPPDATA%\Rekey\settings.json` while Rekey is not running (Rekey writes the file on exit). Use a Debug build of `src/Rekey.App`. These checks use the physical keyboard, which the tool cannot. Each step stands alone: it names the Hotkey it needs (quit Rekey, set `"hotkey"`, start Rekey again when it differs from the current one) and starts with English (US) active in the window it types in.

- [ ] **Double tap of Shift (1.2, 2.1).** Start Rekey with `"autoCorrectionEnabled": false` and `"hotkey": "doubleShift"`. In Notepad type "ghbdtn" and tap Shift twice quickly. Switch back to English (US), press Enter for a new line, then type "Ghbdtn" holding Shift for the capital and tap Shift once.
  Expected: the first Word becomes "привет" and the Layout Russian; the second is left as typed (the Shift held for "G" does not count as a tap).
- [ ] **Win+Space with the physical keys (1.4).** Quit Rekey, set `"hotkey": "winSpace"`, start Rekey. In Notepad, in English (US), type "ghbdtn" and press Win+Space; then on a new empty line press Win+Space once more; then press Win alone.
  Expected: "ghbdtn" becomes "привет" and the Layout is Russian, with no Windows layout pop-up switching a second time and no Start menu; on the empty line the Layout becomes the one after Russian in Windows' order (English (US) when only those two are installed) and Start stays closed; Win alone opens Start as usual.
- [ ] **Selection a control does not expose (3.2).** Quit Rekey, set `"hotkey": "doubleShift"`, start Rekey. Open the Run dialog (Win+R), switch to English (US), type "ghbdtn", select it with Shift+Home and tap Shift twice.
  Expected: if the field exposes its selection, it becomes "привет"; if it does not, the text stays "ghbdtn" and only the Layout switches. Report which happened.
- [ ] **Password Field (2.3).** With `"hotkey": "doubleShift"`, on any sign-in page, switch to English (US), type "ghbdtn" in the password box and tap Shift twice.
  Expected: nothing happens: the box keeps its characters and the Layout stays English (US).

### Open item to evaluate

- [ ] **Keys typed during a multi-part replacement.** Since 1ded4ab, a replacement with spaces is typed in parts with a 50 ms pause after each space (design D4 of add-manual-correction), because Notepad garbles text after a space in one burst of injected input. The hook holds typed keys behind a Correction for at most 150 ms (design D1 of add-text-replacement), so keys typed after that can land between the parts, inside the replacement: in a run of two or three Deferred Words corrected while typing fast, or while typing during a long selection conversion right after the Hotkey. Kept as is for now (owner decision 2026-11-14). At the final pass, try both cases and decide whether to keep it or to let the worker extend the hook's hold while a multi-part replacement types, which changes the 150 ms rule and would be its own change.

## add-word-learning

Written against the head of the `add-word-learning` PR. The rules are covered by unit tests; these steps check the app wiring. Use a Debug build of `src/Rekey.App` with English (US) and Russian installed and default settings. The app has no word list editors before `add-tray-and-settings-ui`; inspect `%LOCALAPPDATA%\Rekey\words.json` with Rekey closed.

- [ ] **Undo protects, and survives a restart (3.1, 4.1).** Quit Rekey, delete `words.json` if present, and start Rekey. In Notepad, in English (US), type "ntcn" and Space: it becomes "тест ". Press Ctrl+Z within 5 s: it is "ntcn " again. Wait 3 s, then type "ntcn" and Space once more. Quit Rekey from the tray, start it, switch to English (US), type "ntcn" and Space.
  Expected: the second and third "ntcn" stay as typed; `words.json` lists "ntcn" under `protected` with `"source": "auto"`.
- [ ] **Manual Correction learns (3.2).** In Notepad, in English (US), type "rhby;" and Space: it stays. Press the Hotkey: it becomes "кринж ". Switch to English (US) and type "rhby;" and Space.
  Expected: the second "rhby;" becomes "кринж" on its own; `words.json` lists "кринж" under `learned` with `"language": "ru"` once Rekey has quit.
- [ ] **Unreadable file is kept aside (1.2).** Quit Rekey, replace the content of `words.json` with `{ broken`, start Rekey.
  Expected: Rekey starts as usual; `words.json.bak` holds `{ broken`.
