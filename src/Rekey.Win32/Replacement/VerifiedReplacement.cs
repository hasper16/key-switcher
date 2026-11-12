namespace Rekey.Win32.Replacement {
  /// <summary>
  /// Decides how to replace the text before the caret, checking it first where the control
  /// exposes it (spec text-replacement "Verified replacement", design D3).
  /// </summary>
  internal static class VerifiedReplacement {
    // How far beyond the Word a mismatch looks, enough for a typical autocomplete suffix.
    private const int _contextLength = 32;

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
        return Classify(reader, before, expected);
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

    // A read ending with the Word but longer comes from a provider that widened the range; the
    // Word inside a wider stretch was changed around, as by an autocomplete; anything else
    // says nothing about the Word.
    private static ReplacementMethod Classify(
        ITextBeforeCaret reader, string before, string expected) {
      if (before.Length > expected.Length && before.EndsWith(expected, StringComparison.Ordinal)) {
        return ReplacementMethod.Retype;
      }

      // A wider read that fails or hits the requested limit cannot rule out a longer
      // autocomplete, so it cancels without blaming the app.
      int limit = expected.Length + _contextLength;
      string? context = reader.ReadContext(limit);
      bool keepChecking = context is null
          || context.Length >= limit
          || context.Contains(expected, StringComparison.Ordinal);
      return keepChecking ? ReplacementMethod.Cancel : ReplacementMethod.Unrelated;
    }
  }
}
