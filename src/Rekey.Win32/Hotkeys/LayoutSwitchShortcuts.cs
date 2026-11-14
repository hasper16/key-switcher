using Microsoft.Win32;
using Rekey.Core;

namespace Rekey.Win32.Hotkeys {
  /// <summary>
  /// Reads the shortcuts Windows switches Layout with (design D5 of add-manual-correction).
  /// </summary>
  public static class LayoutSwitchShortcuts {
    private const string _toggleKey = @"Keyboard Layout\Toggle";
    private const ushort _graveAccent = 0xC0;

    /// <summary>
    /// Returns the Layout Switch shortcuts of the current Windows user.
    /// </summary>
    /// <returns>The shortcuts; Win+Space is always one.</returns>
    public static IReadOnlySet<KeyChord> Read() {
      using RegistryKey? toggle = Registry.CurrentUser.OpenSubKey(_toggleKey);
      return Parse(name => toggle?.GetValue(name) as string);
    }

    /// <summary>
    /// Returns the Layout Switch shortcuts that the values of the Toggle registry key name.
    /// </summary>
    /// <param name="toggleValue">
    /// Returns the Toggle value of a name, or <see langword="null" /> when it is missing.
    /// </param>
    /// <returns>The shortcuts; Win+Space is always one.</returns>
    public static IReadOnlySet<KeyChord> Parse(Func<string, string?> toggleValue) {
      string? language = toggleValue("Language Hotkey") ?? toggleValue("Hotkey");
      return new[] { language, toggleValue("Layout Hotkey") }
          .Select(ChordOf)
          .OfType<KeyChord>()

          // Windows keeps Win+Space even when the other shortcuts are turned off.
          .Append(new KeyChord(KeyModifiers.Windows, Hotkey.SpaceKey))
          .ToHashSet();
    }

    private static KeyChord? ChordOf(string? value) {
      return value?.Trim() switch {
        "1" => new KeyChord(KeyModifiers.Alt | KeyModifiers.Shift, 0),
        "2" => new KeyChord(KeyModifiers.Control | KeyModifiers.Shift, 0),
        "4" => new KeyChord(KeyModifiers.None, _graveAccent),
        _ => null,
      };
    }
  }
}
