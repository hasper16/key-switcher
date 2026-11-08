# Manual checks: add-language-scorer

The Decision logic is pure and covered by automated tests and the accuracy harness. One part touches Windows and is checked by hand on a desktop:

1. **Key Map capture matches the fixtures.** On a Windows machine with any Layouts installed, run:

   ```bash
   dotnet run --project tools/Rekey.KeyMapCapture -c Release -- tests/fixtures/keymaps 00000409 00000419 00000422 00020422
   git status --short tests/fixtures/keymaps
   ```

   Expected: the four files are rewritten unchanged (`git status` shows nothing), and the user's Layout list (Settings → Time & language → Typing → Language bar, or `HKCU\Keyboard Layout\Preload`) is the same before and after: a Layout the session lacked is loaded only for the read.

No other change in this change needs a desktop check: nothing is wired into the running app yet (change `add-auto-correction` does that).
