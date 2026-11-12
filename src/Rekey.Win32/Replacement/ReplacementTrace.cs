namespace Rekey.Win32.Replacement {
  /// <summary>
  /// Describes how the text read before the caret compared with the Word, without either text,
  /// for diagnosing apps whose text check fails.
  /// </summary>
  /// <param name="Reader">The control that was read.</param>
  /// <param name="ReadLength">The number of characters read.</param>
  /// <param name="ExpectedLength">The number of characters expected.</param>
  /// <param name="SameStart">How many leading characters agree.</param>
  /// <param name="SameEnd">How many trailing characters agree.</param>
  /// <param name="ExpectedInside">
  /// <see langword="true" /> if the expected text appears anywhere in what was read;
  /// otherwise, <see langword="false" />.
  /// </param>
  public sealed record ReplacementTrace(
      ReaderFacts Reader,
      int ReadLength,
      int ExpectedLength,
      int SameStart,
      int SameEnd,
      bool ExpectedInside) {
    /// <summary>
    /// Gets why the text did not match, and so whether the app got a strike.
    /// </summary>
    public MismatchKind Kind { get; init; }

    /// <summary>
    /// Compares a read with the expected text.
    /// </summary>
    /// <param name="read">The text read before the caret.</param>
    /// <param name="expected">The text Rekey expected.</param>
    /// <param name="reader">The control that was read.</param>
    /// <returns>The comparison, without either text.</returns>
    public static ReplacementTrace Of(string read, string expected, ReaderFacts reader) {
      int limit = Math.Min(read.Length, expected.Length);
      int start = 0;
      while (start < limit && read[start] == expected[start]) {
        start++;
      }

      int end = 0;
      while (end < limit && read[^(end + 1)] == expected[^(end + 1)]) {
        end++;
      }

      return new ReplacementTrace(
          reader,
          read.Length,
          expected.Length,
          start,
          end,
          read.Contains(expected, StringComparison.Ordinal));
    }
  }
}
