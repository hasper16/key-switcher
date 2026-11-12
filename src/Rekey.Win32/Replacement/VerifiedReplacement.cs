using System.Buffers;

namespace Rekey.Win32.Replacement {
  /// <summary>
  /// Decides how to replace the text before the caret, checking it first where the control
  /// exposes it (spec text-replacement "Verified replacement", design D3).
  /// </summary>
  internal static class VerifiedReplacement {
    // How far beyond the Word a mismatch looks, enough for a typical autocomplete suffix.
    private const int _contextLength = 32;

    private static readonly SearchValues<char> _spaces = SearchValues.Create(" \t\r\n\u00A0");

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
      return Prepare(reader, expected, stillValid, canSelect, out _);
    }

    /// <summary>
    /// Checks the text before the caret and, when it matches and the request is still valid,
    /// selects it; says why when it did not match.
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
    /// <param name="mismatch">Why the text did not match, for diagnostics.</param>
    /// <returns>How to replace the text.</returns>
    public static ReplacementMethod Prepare(
        ITextBeforeCaret? reader,
        string expected,
        Func<bool> stillValid,
        bool canSelect,
        out MismatchKind mismatch) {
      mismatch = MismatchKind.None;
      string? before = reader?.Read(expected.Length);
      if (reader is null || before is null) {
        return ReplacementMethod.Retype;
      }

      if (!string.Equals(before, expected, StringComparison.Ordinal)) {
        return Classify(reader, before, expected, out mismatch);
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

    /// <summary>
    /// Tells an edited Word from unrelated text, given a wider stretch read before the caret.
    /// </summary>
    /// <param name="context">
    /// The text read, or <see langword="null" /> when the read failed.
    /// </param>
    /// <param name="expected">The text Rekey expected just before the caret.</param>
    /// <param name="limit">The number of characters asked for.</param>
    /// <returns>Why the text did not match.</returns>
    public static MismatchKind ClassifyContext(string? context, string expected, int limit) {
      if (context is null) {
        return MismatchKind.Inconclusive;
      }

      // An autocomplete attaches characters to the Word, with no space before the caret; an
      // earlier copy of the same Word elsewhere on the line says nothing.
      int at = context.LastIndexOf(expected, StringComparison.Ordinal);
      if (at >= 0 && IsAttachedSuffix(context.AsSpan(at + expected.Length))) {
        return MismatchKind.EditedWord;
      }

      // A full read without a space may be the tail of a completion longer than the read.
      bool full = context.Length >= limit;
      return full && !context.AsSpan().ContainsAny(_spaces)
          ? MismatchKind.Inconclusive
          : MismatchKind.Unrelated;
    }

    // A read ending with the Word but longer comes from a provider that widened the range; the
    // Word inside a wider stretch was changed around, as by an autocomplete; anything else
    // says nothing about the Word.
    private static ReplacementMethod Classify(
        ITextBeforeCaret reader, string before, string expected, out MismatchKind mismatch) {
      mismatch = MismatchKind.None;
      if (before.Length > expected.Length && before.EndsWith(expected, StringComparison.Ordinal)) {
        return ReplacementMethod.Retype;
      }

      int limit = expected.Length + _contextLength;
      mismatch = ClassifyContext(reader.ReadContext(limit), expected, limit);
      return mismatch == MismatchKind.Unrelated
          ? ReplacementMethod.Unrelated
          : ReplacementMethod.Cancel;
    }

    private static bool IsAttachedSuffix(ReadOnlySpan<char> suffix) {
      return !suffix.IsEmpty && !suffix.ContainsAny(_spaces);
    }
  }
}
