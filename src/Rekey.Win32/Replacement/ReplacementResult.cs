namespace Rekey.Win32.Replacement {
  /// <summary>
  /// Specifies what became of a replacement.
  /// </summary>
  public enum ReplacementResult {
    /// <summary>The text was checked, selected and typed over.</summary>
    Verified,

    /// <summary>The text was erased with Backspace and typed again, unchecked.</summary>
    Retyped,

    /// <summary>The text before the caret differed, so nothing was changed.</summary>
    Mismatch,

    /// <summary>
    /// The key that asked for it waited too long, so the hook may have let later keys through;
    /// nothing was changed.
    /// </summary>
    TooLate,

    /// <summary>
    /// A mouse click came after the key that asked for it, so the caret is where the user put
    /// it; nothing was changed.
    /// </summary>
    Interrupted,

    /// <summary>
    /// Focus moved to another control while the text was checked; nothing was typed, and a
    /// selection made in the control that was read is collapsed there.
    /// </summary>
    FocusMoved,

    /// <summary>
    /// Windows refused some or all of the injected input, as it does for a window of higher
    /// integrity; the text may be partly changed, so nothing is switched or made undoable.
    /// </summary>
    Blocked,

    /// <summary>
    /// The focused app did not finish selecting the text in time; nothing was changed, and any
    /// selection that lands late is collapsed back to the caret.
    /// </summary>
    TimedOut,
  }
}
