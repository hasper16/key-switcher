namespace Rekey.Win32.Replacement {
  /// <summary>
  /// Specifies when to collapse a selection Rekey asked the app to make.
  /// </summary>
  internal enum Deselection {
    /// <summary>
    /// Always: the selection may still be on its way, as after a timeout.
    /// </summary>
    Always,

    /// <summary>
    /// Only if the control's selection is still exactly the Word, so a caret the user placed
    /// with a click stays where it is.
    /// </summary>
    IfStillSelected,
  }
}
