using Rekey.Win32.Keyboard;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace Rekey.Win32.Replacement {
  /// <summary>
  /// Builds the keyboard input Rekey injects with <c>SendInput</c>, every input tagged so the
  /// hook ignores it (design D2).
  /// </summary>
  internal static class InputBuilder {
    /// <summary>
    /// Returns the inputs of <see cref="Replacement" /> in parts, each but the last ending
    /// right after a space, so the parts can be sent with a pause between them.
    /// </summary>
    /// <remarks>
    /// Notepad garbles or drops what follows a space typed in the same burst (design D4 of
    /// add-manual-correction).
    /// </remarks>
    /// <param name="erase">The number of Backspaces to send first.</param>
    /// <param name="text">The text to type.</param>
    /// <param name="heldModifiers">The modifier keys down now, as for Replacement.</param>
    /// <returns>The parts, in order.</returns>
    public static IReadOnlyList<INPUT[]> ReplacementParts(
        int erase, string text, IReadOnlyList<VIRTUAL_KEY> heldModifiers) {
      INPUT[] inputs = Replacement(erase, text, heldModifiers);
      int start = 0;
      var parts = new List<INPUT[]>();
      for (int i = 0; i < text.Length - 1; i++) {
        if (text[i] == ' ') {
          int end = heldModifiers.Count + (2 * (erase + i + 1));
          parts.Add(inputs[start..end]);
          start = end;
        }
      }

      parts.Add(inputs[start..]);
      return parts;
    }

    /// <summary>
    /// Returns the inputs that erase text before the caret and type a replacement.
    /// </summary>
    /// <param name="erase">The number of characters to erase with Backspace.</param>
    /// <param name="text">The text to type.</param>
    /// <param name="heldModifiers">
    /// The modifier keys down now; they are released first, so Backspace does not
    /// become Ctrl+Backspace, which deletes a whole word, and pressed again after the text, as
    /// the user still holds them.
    /// </param>
    /// <returns>The inputs, in order.</returns>
    public static INPUT[] Replacement(
        int erase, string text, IReadOnlyList<VIRTUAL_KEY> heldModifiers) {
      var inputs = new List<INPUT>(heldModifiers.Count + (2 * (erase + text.Length)));
      inputs.AddRange(heldModifiers.Select(key => Key(key, 0, KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP)));
      for (int i = 0; i < erase; i++) {
        inputs.Add(Key(VIRTUAL_KEY.VK_BACK, 0, 0));
        inputs.Add(Key(VIRTUAL_KEY.VK_BACK, 0, KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP));
      }

      foreach (char unit in text) {
        inputs.Add(Key(0, unit, KEYBD_EVENT_FLAGS.KEYEVENTF_UNICODE));
        inputs.Add(Key(
            0, unit, KEYBD_EVENT_FLAGS.KEYEVENTF_UNICODE | KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP));
      }

      inputs.AddRange(Repress(heldModifiers));
      return [.. inputs];
    }

    /// <summary>
    /// Returns the inputs that collapse a selection to its end, where the caret was before it.
    /// </summary>
    /// <param name="heldModifiers">
    /// The modifier keys down now; they are released around Right, which would otherwise extend
    /// the selection (Shift) or jump a word (Ctrl).
    /// </param>
    /// <returns>The inputs, in order.</returns>
    public static INPUT[] CollapseSelection(IReadOnlyList<VIRTUAL_KEY> heldModifiers) {
      // Right is an extended key; without the flag it would be the keypad's 6.
      return [
          .. heldModifiers.Select(key => Key(key, 0, KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP)),
          Key(VIRTUAL_KEY.VK_RIGHT, 0, KEYBD_EVENT_FLAGS.KEYEVENTF_EXTENDEDKEY),
          Key(
              VIRTUAL_KEY.VK_RIGHT,
              0,
              KEYBD_EVENT_FLAGS.KEYEVENTF_EXTENDEDKEY | KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP),
          .. Repress(heldModifiers),
      ];
    }

    /// <summary>
    /// Returns a press and release of an unassigned key, which keeps the shell from opening
    /// Start when Win is released after a swallowed key.
    /// </summary>
    /// <returns>The inputs to send.</returns>
    public static INPUT[] MaskWin() {
      const VIRTUAL_KEY unassigned = (VIRTUAL_KEY)0xE8;
      return [Key(unassigned, 0, 0), Key(unassigned, 0, KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP)];
    }

    /// <summary>
    /// Returns the inputs that press the held modifiers again after a replacement stopped
    /// part-way, with the masking key after Win.
    /// </summary>
    /// <param name="heldModifiers">The modifier keys the replacement released.</param>
    /// <returns>The inputs, in order.</returns>
    public static INPUT[] Restore(IReadOnlyList<VIRTUAL_KEY> heldModifiers) {
      return [.. Repress(heldModifiers)];
    }

    /// <summary>
    /// Returns the input that sends a held key event on to the application.
    /// </summary>
    /// <remarks>
    /// A key with a scan code goes by scan code, so Windows maps it through the Layout active
    /// when it arrives, which may be the one a Correction just switched to.
    /// </remarks>
    /// <param name="held">The key event the hook held back.</param>
    /// <returns>The input.</returns>
    public static INPUT Reinject(in RawInput held) {
      KEYBD_EVENT_FLAGS flags = held.Kind == RawInputKind.KeyUp
          ? KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP
          : 0;
      if (held.ScanCode == 0) {
        return Key((VIRTUAL_KEY)held.VirtualKey, 0, flags);
      }

      flags |= KEYBD_EVENT_FLAGS.KEYEVENTF_SCANCODE;
      if (held.Extended) {
        flags |= KEYBD_EVENT_FLAGS.KEYEVENTF_EXTENDEDKEY;
      }

      return Key(0, held.ScanCode, flags);
    }

    // Design D0 of add-manual-correction: the user's own Win release follows the re-press, so a
    // key between them keeps the shell from opening Start.
    private static IEnumerable<INPUT> Repress(IReadOnlyList<VIRTUAL_KEY> heldModifiers) {
      IEnumerable<INPUT> again = heldModifiers.Select(key => Key(key, 0, 0));
      return heldModifiers.Any(key => key is VIRTUAL_KEY.VK_LWIN or VIRTUAL_KEY.VK_RWIN)
          ? again.Concat(MaskWin())
          : again;
    }

    private static INPUT Key(VIRTUAL_KEY key, ushort scanCode, KEYBD_EVENT_FLAGS flags) {
      return new INPUT {
        type = INPUT_TYPE.INPUT_KEYBOARD,
        Anonymous = new INPUT._Anonymous_e__Union {
          ki = new KEYBDINPUT {
            wVk = key,
            wScan = scanCode,
            dwFlags = flags,
            dwExtraInfo = InjectedInput.Tag,
          },
        },
      };
    }
  }
}
