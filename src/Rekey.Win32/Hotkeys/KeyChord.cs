using Rekey.Core;

namespace Rekey.Win32.Hotkeys {
  /// <summary>
  /// Represents a keyboard shortcut: modifiers held with one key, or modifiers alone.
  /// </summary>
  /// <param name="Modifiers">
  /// A bitwise combination of the enumeration values that specifies the modifiers.
  /// </param>
  /// <param name="VirtualKey">
  /// The Windows virtual-key code of the key, or 0 for a shortcut of modifiers alone.
  /// </param>
  public readonly record struct KeyChord(KeyModifiers Modifiers, ushort VirtualKey);
}
