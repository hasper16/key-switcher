namespace Rekey.Win32.Replacement {
  /// <summary>
  /// Specifies how a replacement reaches the application.
  /// </summary>
  internal enum ReplacementMethod {
    /// <summary>Erase the text with Backspace, then type the replacement.</summary>
    Retype,

    /// <summary>The text is selected; type the replacement over it.</summary>
    SelectAndType,

    /// <summary>The text before the caret is not what Rekey expects; change nothing.</summary>
    Cancel,

    /// <summary>
    /// Selecting the text ran out of time, but the app may still apply the selection; change
    /// nothing, and collapse any selection back to the caret.
    /// </summary>
    Abandon,
  }
}
