using Rekey.Core;

namespace Rekey.Win32.Hotkeys {
  /// <summary>
  /// Finds what a chosen Hotkey would take over (spec manual-correction: "Conflict warning").
  /// </summary>
  public static class HotkeyConflicts {
    /// <summary>
    /// Returns what the Hotkey would take over.
    /// </summary>
    /// <remarks>
    /// A double tap is no chord, so it never matches a shortcut (design D5).
    /// </remarks>
    /// <param name="hotkey">The chosen Hotkey.</param>
    /// <param name="windowsShortcuts">The Windows Layout Switch shortcuts.</param>
    /// <param name="isTaken">
    /// Tells whether another application has registered a combination.
    /// </param>
    /// <returns>One of the enumeration values that specifies the conflict.</returns>
    public static HotkeyConflict Check(
        Hotkey hotkey, IReadOnlySet<KeyChord> windowsShortcuts, Func<KeyChord, bool> isTaken) {
      if (hotkey.TappedModifier != KeyModifiers.None) {
        return HotkeyConflict.None;
      }

      var chord = new KeyChord(hotkey.Modifiers, hotkey.VirtualKey);
      if (windowsShortcuts.Contains(chord)) {
        return HotkeyConflict.WindowsLayoutSwitch;
      }

      return isTaken(chord) ? HotkeyConflict.TakenByAnotherApp : HotkeyConflict.None;
    }
  }
}
