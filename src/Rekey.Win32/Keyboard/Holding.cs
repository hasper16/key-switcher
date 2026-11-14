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

    /// <summary>
    /// The key performed a combination Hotkey; the worker handles the Manual Correction and never
    /// re-injects the key.
    /// </summary>
    Hotkey,

    /// <summary>
    /// The key completed a double-tap Hotkey; the worker re-injects it after the Manual
    /// Correction.
    /// </summary>
    HotkeyTap,

    /// <summary>
    /// A repeat or release of a combination Hotkey's key, dropped for good; it holds nothing
    /// back.
    /// </summary>
    Swallowed,
  }
}
