# Deferred checks by hand

By the owner's decision of 2026-11-14, the checks that need a person at the keyboard are done once, in a final acceptance pass after all roadmap changes, together with any fixes they lead to. Each change adds its section here instead of waiting for them; its own `docs/manual-tests/<change>.md` keeps the automated checks and points here. Each section names the commit its steps were written against; run them on the final build, and note where behaviour changed since.

## add-manual-correction

Written against f4ec2f6 (PR #17). The automated `manual-correction` run passed on that commit (run 7); these steps cover what injected keys cannot.

The app has no settings window before `add-tray-and-settings-ui`, so the Hotkey is changed in `%LOCALAPPDATA%\Rekey\settings.json` while Rekey is not running (Rekey writes the file on exit). Use a Debug build of `src/Rekey.App` and English (US) as the starting Layout. These checks use the physical keyboard, which the tool cannot.

- [ ] **Double tap of Shift (1.2, 2.1).** Start Rekey with `"autoCorrectionEnabled": false` and `"hotkey": "doubleShift"`. In Notepad type "ghbdtn" and tap Shift twice quickly. Switch back to English (US), press Enter for a new line, then type "Ghbdtn" holding Shift for the capital and tap Shift once.
  Expected: the first Word becomes "привет" and the Layout Russian; the second is left as typed (the Shift held for "G" does not count as a tap).
- [ ] **Win+Space with the physical keys (1.4).** Quit Rekey, set `"hotkey": "winSpace"`, start Rekey. In Notepad, in English (US), type "ghbdtn" and press Win+Space; then on a new empty line press Win+Space once more; then press Win alone.
  Expected: "ghbdtn" becomes "привет" and the Layout is Russian, with no Windows layout pop-up switching a second time and no Start menu; on the empty line the Layout becomes the one after Russian in Windows' order (English (US) when only those two are installed) and Start stays closed; Win alone opens Start as usual.
- [ ] **Selection a control does not expose (3.2).** Quit Rekey, set `"hotkey": "doubleShift"`, start Rekey. Open the Run dialog (Win+R), type "ghbdtn", select it with Shift+Home and tap Shift twice.
  Expected: if the field exposes its selection, it becomes "привет"; if it does not, the text stays "ghbdtn" and only the Layout switches. Report which happened.
- [ ] **Password Field (2.3).** On any sign-in page, switch to English (US), type "ghbdtn" in the password box and tap Shift twice.
  Expected: nothing happens: the box keeps its characters and the Layout stays English (US).

### Open item to evaluate

- [ ] **Keys typed during a multi-part replacement.** Since 1ded4ab, a replacement with spaces is typed in parts with a 50 ms pause after each space (design D4 of add-manual-correction), because Notepad garbles text after a space in one burst of injected input. The hook holds typed keys behind a Correction for at most 150 ms (design D1 of add-text-replacement), so keys typed after that can land between the parts, inside the replacement: in a run of two or three Deferred Words corrected while typing fast, or while typing during a long selection conversion right after the Hotkey. Kept as is for now (owner decision 2026-11-14). At the final pass, try both cases and decide whether to keep it or to let the worker extend the hook's hold while a multi-part replacement types, which changes the 150 ms rule and would be its own change.
