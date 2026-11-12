namespace Rekey.Win32.Replacement {
  /// <summary>
  /// Specifies why the text before the caret did not match the Word.
  /// </summary>
  public enum MismatchKind {
    /// <summary>The text matched, or was not read.</summary>
    None,

    /// <summary>
    /// The Word sits just before the caret with other characters attached after it, as an
    /// autocomplete leaves it; cancelled, the app keeps being checked.
    /// </summary>
    EditedWord,

    /// <summary>
    /// The wider read failed, or came back full without a space, so it may hide a long
    /// completion; cancelled, the app keeps being checked.
    /// </summary>
    Inconclusive,

    /// <summary>
    /// The text has nothing to do with the Word at the caret; cancelled, and a strike for the
    /// app.
    /// </summary>
    Unrelated,
  }
}
