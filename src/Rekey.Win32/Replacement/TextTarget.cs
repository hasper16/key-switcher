namespace Rekey.Win32.Replacement {
  /// <summary>
  /// Specifies how a window takes a replacement.
  /// </summary>
  internal enum TextTarget {
    /// <summary>
    /// An ordinary control: its text may be checked and selected, then typed over.
    /// </summary>
    Editable,

    /// <summary>
    /// A console proven to take Backspaces and typed Unicode characters: its text may be
    /// checked, but its UI Automation selection is the terminal's own, so it is retyped.
    /// </summary>
    Console,

    /// <summary>
    /// A window where typed text is not proven to arrive: nothing is erased or typed.
    /// </summary>
    Unsupported,
  }
}
