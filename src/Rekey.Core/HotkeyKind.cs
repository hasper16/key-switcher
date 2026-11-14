namespace Rekey.Core {
  /// <summary>
  /// Specifies the gesture of a Hotkey.
  /// </summary>
  public enum HotkeyKind {
    /// <summary>Shift pressed and released twice in quick succession.</summary>
    DoubleShift,

    /// <summary>Ctrl pressed and released twice in quick succession.</summary>
    DoubleCtrl,

    /// <summary>Alt pressed and released twice in quick succession.</summary>
    DoubleAlt,

    /// <summary>Ctrl+Space.</summary>
    CtrlSpace,

    /// <summary>Win+Space, which takes over the Windows Layout Switch shortcut.</summary>
    WinSpace,

    /// <summary>A combination of modifiers and one key the user chose.</summary>
    Custom,
  }
}
