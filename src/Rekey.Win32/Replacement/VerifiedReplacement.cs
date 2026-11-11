namespace Rekey.Win32.Replacement {
  /// <summary>
  /// Decides how to replace the text before the caret, checking it first where the control
  /// exposes it (spec text-replacement "Verified replacement", design D3).
  /// </summary>
  internal static class VerifiedReplacement {
    /// <summary>
    /// Checks the text before the caret and, when it matches, selects it.
    /// </summary>
    /// <param name="reader">
    /// The focused control's text, or <see langword="null" /> to skip the check.
    /// </param>
    /// <param name="expected">The text Rekey expects just before the caret.</param>
    /// <returns>How to replace the text.</returns>
    public static ReplacementMethod Prepare(ITextBeforeCaret? reader, string expected) {
      string? before = reader?.Read(expected.Length);
      if (reader is null || before is null) {
        return ReplacementMethod.Retype;
      }

      if (!string.Equals(before, expected, StringComparison.Ordinal)) {
        return ReplacementMethod.Cancel;
      }

      if (reader.SelectRead()) {
        return ReplacementMethod.SelectAndType;
      }

      // A selection that timed out may still land, and Backspaces would then erase too much.
      return reader.TimedOut ? ReplacementMethod.Cancel : ReplacementMethod.Retype;
    }
  }
}
