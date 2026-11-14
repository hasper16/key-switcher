using Rekey.Core;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace Rekey.Win32.Keyboard {
  /// <summary>
  /// Recognises the Hotkey in the keys the hook sees (design D1 of add-manual-correction).
  /// </summary>
  /// <remarks>
  /// The worker builds one per Hotkey setting; afterwards only the hook thread uses it, and it
  /// never allocates there.
  /// </remarks>
  internal sealed class HotkeyDetector {
    private const ushort _none = 0;

    private readonly Hotkey _hotkey;
    private readonly DoubleTap? _doubleTap;
    private readonly Func<VIRTUAL_KEY, bool> _isDown;
    private Side _held;

    // Whether the combination's key went on to the application when it was pressed, so its
    // repeats and release go on too.
    private bool _keyPassedDown;

    // The combination's key from its press until its release, so its repeats and release never
    // reach the application either.
    private ushort _swallowing = _none;

    // Each side of a modifier, so releasing one side keeps the modifier held by the other.
    [Flags]
    private enum Side : byte {
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
    /// Initializes a new instance of the <see cref="HotkeyDetector" /> class.
    /// </summary>
    /// <param name="hotkey">The Hotkey to recognise.</param>
    /// <param name="isDown">
    /// Tells whether a key is physically down now, so a modifier whose release the hook missed
    /// cannot make an ordinary key perform the Hotkey.
    /// </param>
    public HotkeyDetector(Hotkey hotkey, Func<VIRTUAL_KEY, bool> isDown) {
      _hotkey = hotkey;
      _isDown = isDown;
      _doubleTap = hotkey.TappedModifier != KeyModifiers.None
          ? new DoubleTap(hotkey.TappedModifier)
          : null;
    }

    /// <summary>
    /// Observes one key going down or up and returns what it means for the Hotkey.
    /// </summary>
    /// <param name="virtualKey">The virtual-key code of the key.</param>
    /// <param name="down">
    /// <see langword="true" /> for a press or repeat; <see langword="false" /> for a release.
    /// </param>
    /// <param name="tick">The time of the event in milliseconds.</param>
    /// <param name="canFire">
    /// <see langword="false" /> when the key cannot be queued for the worker, so it must go on
    /// to the application.
    /// </param>
    /// <returns>One of the enumeration values that specifies the meaning.</returns>
    public HotkeyVerdict Observe(ushort virtualKey, bool down, long tick, bool canFire = true) {
      var key = (VIRTUAL_KEY)virtualKey;
      KeyModifiers modifier = ModifierOf(key);
      Side side = SideOf(key);
      _held = down ? _held | side : _held & ~side;

      if (_doubleTap is not null) {
        return _doubleTap.Observe(modifier, down, tick) && canFire
            ? HotkeyVerdict.Tap
            : HotkeyVerdict.None;
      }

      if (virtualKey == _swallowing) {
        if (!down) {
          _swallowing = _none;
        }

        return HotkeyVerdict.Swallow;
      }

      if (virtualKey != _hotkey.VirtualKey) {
        return HotkeyVerdict.None;
      }

      if (down && !_keyPassedDown && canFire && Held() == _hotkey.Modifiers
          && AreModifiersDown()) {
        _swallowing = virtualKey;
        return HotkeyVerdict.Fire;
      }

      _keyPassedDown = down;
      return HotkeyVerdict.None;
    }

    private static KeyModifiers ModifierOf(VIRTUAL_KEY key) {
      return key switch {
        VIRTUAL_KEY.VK_LSHIFT or VIRTUAL_KEY.VK_RSHIFT => KeyModifiers.Shift,
        VIRTUAL_KEY.VK_LCONTROL or VIRTUAL_KEY.VK_RCONTROL => KeyModifiers.Control,
        VIRTUAL_KEY.VK_LMENU or VIRTUAL_KEY.VK_RMENU => KeyModifiers.Alt,
        VIRTUAL_KEY.VK_LWIN or VIRTUAL_KEY.VK_RWIN => KeyModifiers.Windows,
        _ => KeyModifiers.None,
      };
    }

    private static Side SideOf(VIRTUAL_KEY key) {
      return key switch {
        VIRTUAL_KEY.VK_LSHIFT => Side.ShiftLeft,
        VIRTUAL_KEY.VK_RSHIFT => Side.ShiftRight,
        VIRTUAL_KEY.VK_LCONTROL => Side.ControlLeft,
        VIRTUAL_KEY.VK_RCONTROL => Side.ControlRight,
        VIRTUAL_KEY.VK_LMENU => Side.AltLeft,
        VIRTUAL_KEY.VK_RMENU => Side.AltRight,
        VIRTUAL_KEY.VK_LWIN => Side.WindowsLeft,
        VIRTUAL_KEY.VK_RWIN => Side.WindowsRight,
        _ => Side.None,
      };
    }

    private KeyModifiers Held() {
      KeyModifiers held = KeyModifiers.None;
      if ((_held & (Side.ShiftLeft | Side.ShiftRight)) != 0) {
        held |= KeyModifiers.Shift;
      }

      if ((_held & (Side.ControlLeft | Side.ControlRight)) != 0) {
        held |= KeyModifiers.Control;
      }

      if ((_held & (Side.AltLeft | Side.AltRight)) != 0) {
        held |= KeyModifiers.Alt;
      }

      if ((_held & (Side.WindowsLeft | Side.WindowsRight)) != 0) {
        held |= KeyModifiers.Windows;
      }

      return held;
    }

    private bool AreModifiersDown() {
      return Holds(KeyModifiers.Shift, VIRTUAL_KEY.VK_LSHIFT, VIRTUAL_KEY.VK_RSHIFT)
          && Holds(KeyModifiers.Control, VIRTUAL_KEY.VK_LCONTROL, VIRTUAL_KEY.VK_RCONTROL)
          && Holds(KeyModifiers.Alt, VIRTUAL_KEY.VK_LMENU, VIRTUAL_KEY.VK_RMENU)
          && Holds(KeyModifiers.Windows, VIRTUAL_KEY.VK_LWIN, VIRTUAL_KEY.VK_RWIN);
    }

    private bool Holds(KeyModifiers modifier, VIRTUAL_KEY left, VIRTUAL_KEY right) {
      return (_hotkey.Modifiers & modifier) == 0 || _isDown(left) || _isDown(right);
    }
  }
}
