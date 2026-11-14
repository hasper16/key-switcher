namespace Rekey.Win32.Keyboard {
  /// <summary>
  /// Specifies what a key means for the Hotkey.
  /// </summary>
  internal enum HotkeyVerdict : byte {
    /// <summary>The key has nothing to do with the Hotkey.</summary>
    None,

    /// <summary>The key performs a combination Hotkey; the application never gets it.</summary>
    Fire,

    /// <summary>
    /// The key completes a double tap; the application gets it after the Manual Correction.
    /// </summary>
    Tap,

    /// <summary>
    /// A repeat or release of a combination's key; the application never gets it.
    /// </summary>
    Swallow,
  }
}
