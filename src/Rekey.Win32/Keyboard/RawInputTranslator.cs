using System.Collections.Frozen;
using Rekey.Core;
using Rekey.Core.Detection;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace Rekey.Win32.Keyboard {
  /// <summary>
  /// Turns raw hook input into the engine's input events, tracking the modifier keys and Caps
  /// Lock from the key-downs and key-ups it sees.
  /// </summary>
  /// <remarks>
  /// Not thread-safe: the worker thread owns it.
  /// </remarks>
  internal sealed class RawInputTranslator {
    private static readonly FrozenSet<VIRTUAL_KEY> _navigationKeys = new[] {
      VIRTUAL_KEY.VK_LEFT, VIRTUAL_KEY.VK_UP, VIRTUAL_KEY.VK_RIGHT, VIRTUAL_KEY.VK_DOWN,
      VIRTUAL_KEY.VK_HOME, VIRTUAL_KEY.VK_END, VIRTUAL_KEY.VK_PRIOR, VIRTUAL_KEY.VK_NEXT,
      VIRTUAL_KEY.VK_DELETE, VIRTUAL_KEY.VK_ESCAPE,
    }.ToFrozenSet();

    // Keys on the numeric keypad that type a character with Num Lock on. Key Maps list only the
    // main block, so these build a Word that no Layout renders, and nothing gets corrected.
    private static readonly FrozenSet<VIRTUAL_KEY> _keypadCharacters = new[] {
      VIRTUAL_KEY.VK_NUMPAD0, VIRTUAL_KEY.VK_NUMPAD1, VIRTUAL_KEY.VK_NUMPAD2,
      VIRTUAL_KEY.VK_NUMPAD3, VIRTUAL_KEY.VK_NUMPAD4, VIRTUAL_KEY.VK_NUMPAD5,
      VIRTUAL_KEY.VK_NUMPAD6, VIRTUAL_KEY.VK_NUMPAD7, VIRTUAL_KEY.VK_NUMPAD8,
      VIRTUAL_KEY.VK_NUMPAD9, VIRTUAL_KEY.VK_MULTIPLY, VIRTUAL_KEY.VK_ADD,
      VIRTUAL_KEY.VK_SUBTRACT, VIRTUAL_KEY.VK_DECIMAL, VIRTUAL_KEY.VK_DIVIDE,
    }.ToFrozenSet();

    private FrozenSet<ushort> _characterScanCodes = FrozenSet<ushort>.Empty;
    private ModifierState _held;
    private bool _capsLockOn;
    private bool _capsLockDown;

    public RawInputTranslator(bool capsLockOn) {
      _capsLockOn = capsLockOn;
    }

    [Flags]
    private enum ModifierState {
      None = 0,
      ShiftLeft = 1,
      ShiftRight = 2,
      ControlLeft = 4,
      ControlRight = 8,
      AltLeft = 16,
      AltRight = 32,
      WindowsLeft = 64,
      WindowsRight = 128,
    }

    /// <summary>
    /// Sets the Key Maps of the active Layouts; a key that types text in any of them is a
    /// character key.
    /// </summary>
    public void UseKeyMaps(IEnumerable<KeyMap> keyMaps) {
      _characterScanCodes = keyMaps
          .SelectMany(map => map.Entries)
          .Where(entry => entry.Normal.Length > 0 || entry.Shifted.Length > 0)
          .Select(entry => entry.ScanCode)
          .ToFrozenSet();
    }

    /// <summary>
    /// Returns the input event for one raw input, or <see langword="null" /> for a key-up.
    /// </summary>
    public InputEvent? Translate(in RawInput input) {
      var timestamp = TimeSpan.FromMilliseconds(input.Tick);
      var key = (VIRTUAL_KEY)input.VirtualKey;
      switch (input.Kind) {
        case RawInputKind.MouseDown:
          return new MouseClick(timestamp);
        case RawInputKind.KeyUp:
          Release(key);
          return null;
        default:
          Press(key);
          return KeyDownOf(input, key, timestamp);
      }
    }

    private static ModifierState ModifierOf(VIRTUAL_KEY key) {
      return key switch {
        VIRTUAL_KEY.VK_LSHIFT => ModifierState.ShiftLeft,
        VIRTUAL_KEY.VK_RSHIFT => ModifierState.ShiftRight,
        VIRTUAL_KEY.VK_LCONTROL => ModifierState.ControlLeft,
        VIRTUAL_KEY.VK_RCONTROL => ModifierState.ControlRight,
        VIRTUAL_KEY.VK_LMENU => ModifierState.AltLeft,
        VIRTUAL_KEY.VK_RMENU => ModifierState.AltRight,
        VIRTUAL_KEY.VK_LWIN => ModifierState.WindowsLeft,
        VIRTUAL_KEY.VK_RWIN => ModifierState.WindowsRight,
        _ => ModifierState.None,
      };
    }

    private void Press(VIRTUAL_KEY key) {
      _held |= ModifierOf(key);
      if (key == VIRTUAL_KEY.VK_CAPITAL && !_capsLockDown) {
        _capsLockDown = true;
        _capsLockOn = !_capsLockOn;
      }
    }

    private void Release(VIRTUAL_KEY key) {
      _held &= ~ModifierOf(key);
      if (key == VIRTUAL_KEY.VK_CAPITAL) {
        _capsLockDown = false;
      }
    }

    private KeyDown KeyDownOf(in RawInput input, VIRTUAL_KEY key, TimeSpan timestamp) {
      // Layouts with AltGr make Windows send a left Ctrl along with right Alt.
      bool altGr = Holds(ModifierState.AltRight) && Holds(ModifierState.ControlLeft);
      KeyModifiers modifiers = KeyModifiers.None;
      if (Holds(ModifierState.ShiftLeft | ModifierState.ShiftRight)) {
        modifiers |= KeyModifiers.Shift;
      }

      if (!altGr && Holds(ModifierState.ControlLeft | ModifierState.ControlRight)) {
        modifiers |= KeyModifiers.Control;
      }

      if (!altGr && Holds(ModifierState.AltLeft | ModifierState.AltRight)) {
        modifiers |= KeyModifiers.Alt;
      }

      if (Holds(ModifierState.WindowsLeft | ModifierState.WindowsRight)) {
        modifiers |= KeyModifiers.Windows;
      }

      var keystroke = new Keystroke(
          input.ScanCode, (modifiers & KeyModifiers.Shift) != 0, _capsLockOn, altGr);
      return new KeyDown(timestamp, keystroke, KindOf(input, key), modifiers);
    }

    private KeyKind KindOf(in RawInput input, VIRTUAL_KEY key) {
      return key switch {
        VIRTUAL_KEY.VK_BACK => KeyKind.Backspace,
        VIRTUAL_KEY.VK_TAB => KeyKind.Tab,
        VIRTUAL_KEY.VK_RETURN => KeyKind.Enter,
        VIRTUAL_KEY.VK_SPACE => KeyKind.Space,
        _ when _navigationKeys.Contains(key) => KeyKind.Navigation,
        _ when ModifierOf(key) != ModifierState.None => KeyKind.Modifier,
        _ when _keypadCharacters.Contains(key) => KeyKind.Character,
        _ when !input.Extended && _characterScanCodes.Contains(input.ScanCode) =>
            KeyKind.Character,
        _ => KeyKind.Other,
      };
    }

    private bool Holds(ModifierState modifiers) {
      return (_held & modifiers) != 0;
    }
  }
}
