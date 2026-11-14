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
    private KeyModifiers _held;

    // The combination's key from its press until its release, so its repeats and release never
    // reach the application either.
    private ushort _swallowing = _none;

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
      KeyModifiers modifier = ModifierOf((VIRTUAL_KEY)virtualKey);
      if (modifier != KeyModifiers.None) {
        _held = down ? _held | modifier : _held & ~modifier;
      }

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

      if (down && canFire && virtualKey == _hotkey.VirtualKey && _held == _hotkey.Modifiers
          && AreModifiersDown()) {
        _swallowing = virtualKey;
        return HotkeyVerdict.Fire;
      }

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
