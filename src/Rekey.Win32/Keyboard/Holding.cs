namespace Rekey.Win32.Keyboard {
  /// <summary>
  /// Specifies whether the hook held a key event back from the application.
  /// </summary>
  internal enum Holding : byte {
    /// <summary>The key event went on to the application.</summary>
    None,

    /// <summary>
    /// The hook held the key event back; the worker re-injects it after handling it.
    /// </summary>
    Held,

    /// <summary>
    /// The hook held a Ctrl+Z back as a possible Undo; the worker re-injects it unless the Undo
    /// is applied.
    /// </summary>
    UndoRequest,
  }
}
