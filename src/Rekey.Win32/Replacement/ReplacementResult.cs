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
  }
}
