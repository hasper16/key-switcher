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
      return Prepare(reader, expected, () => true, canSelect: true);
    }

    /// <summary>
    /// Checks the text before the caret and, when it matches and the request is still valid,
    /// selects it.
    /// </summary>
    /// <param name="reader">
    /// The focused control's text, or <see langword="null" /> to skip the check.
    /// </param>
    /// <param name="expected">The text Rekey expects just before the caret.</param>
    /// <param name="stillValid">
    /// Called just before selecting; a click in between would otherwise be undone by the
    /// selection.
    /// </param>
    /// <param name="canSelect">
    /// <see langword="false" /> where typing does not replace a UI Automation selection, as
    /// in a console: matching text is then retyped, never selected.
    /// </param>
    /// <returns>How to replace the text.</returns>
    public static ReplacementMethod Prepare(
        ITextBeforeCaret? reader, string expected, Func<bool> stillValid, bool canSelect) {
      string? before = reader?.Read(expected.Length);
      if (reader is null || before is null) {
        return ReplacementMethod.Retype;
      }

      if (!string.Equals(before, expected, StringComparison.Ordinal)) {
        return ReplacementMethod.Cancel;
      }

      if (!canSelect) {
        return ReplacementMethod.Retype;
      }

      if (!stillValid()) {
        return ReplacementMethod.Stale;
      }

      if (reader.SelectRead()) {
        return ReplacementMethod.SelectAndType;
      }

      // A selection that timed out may still land, and Backspaces would then erase too much.
      return reader.SelectionPending ? ReplacementMethod.Abandon : ReplacementMethod.Retype;
    }
  }
}
