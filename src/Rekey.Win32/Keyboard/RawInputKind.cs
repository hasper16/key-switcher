namespace Rekey.Win32.Keyboard {
  /// <summary>
  /// Specifies what a low-level hook saw.
  /// </summary>
  internal enum RawInputKind : byte {
    /// <summary>A key went down or auto-repeated.</summary>
    KeyDown,

    /// <summary>A key went up.</summary>
    KeyUp,

    /// <summary>A mouse button went down.</summary>
    MouseDown,

    /// <summary>
    /// The hooks were installed again; key-ups may have been lost while they were gone.
    /// </summary>
    Reattached,
  }
}
