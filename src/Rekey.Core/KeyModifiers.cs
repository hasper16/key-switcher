namespace Rekey.Core {
  /// <summary>
  /// Specifies the modifier keys held down during a key press.
  /// </summary>
  [Flags]
  public enum KeyModifiers {
    /// <summary>No modifier key is held down.</summary>
    None = 0,

    /// <summary>A Shift key is held down.</summary>
    Shift = 1,

    /// <summary>A Ctrl key is held down.</summary>
    Control = 2,

    /// <summary>An Alt key is held down.</summary>
    Alt = 4,

    /// <summary>A Windows logo key is held down.</summary>
    Windows = 8,
  }
}
