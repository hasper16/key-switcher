using Rekey.Core;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace Rekey.Win32.Hotkeys {
  /// <summary>
  /// Finds out whether another application has registered a key combination as its hotkey
  /// (design D5 of add-manual-correction).
  /// </summary>
  public static class HotkeyProbe {
    // Any id works: the registration lives only for the probe, on the calling thread.
    private const int _probeId = 0x5245;

    /// <summary>
    /// Returns whether the combination is registered as a hotkey by another application.
    /// </summary>
    /// <remarks>
    /// Registers the combination for the calling thread and unregisters it at once; Windows
    /// refuses a combination that is already registered. Shortcuts that applications catch
    /// with a keyboard hook are not seen.
    /// </remarks>
    /// <param name="chord">The combination.</param>
    /// <returns>
    /// <see langword="true" /> when Windows refuses to register it; otherwise,
    /// <see langword="false" />.
    /// </returns>
    public static bool IsTaken(KeyChord chord) {
      HOT_KEY_MODIFIERS modifiers = HOT_KEY_MODIFIERS.MOD_NOREPEAT;
      if ((chord.Modifiers & KeyModifiers.Shift) != 0) {
        modifiers |= HOT_KEY_MODIFIERS.MOD_SHIFT;
      }

      if ((chord.Modifiers & KeyModifiers.Control) != 0) {
        modifiers |= HOT_KEY_MODIFIERS.MOD_CONTROL;
      }

      if ((chord.Modifiers & KeyModifiers.Alt) != 0) {
        modifiers |= HOT_KEY_MODIFIERS.MOD_ALT;
      }

      if ((chord.Modifiers & KeyModifiers.Windows) != 0) {
        modifiers |= HOT_KEY_MODIFIERS.MOD_WIN;
      }

      if (!PInvoke.RegisterHotKey(HWND.Null, _probeId, modifiers, chord.VirtualKey)) {
        return true;
      }

      PInvoke.UnregisterHotKey(HWND.Null, _probeId);
      return false;
    }
  }
}
