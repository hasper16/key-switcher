namespace Rekey.Accuracy {
  /// <summary>
  /// Holds the counts of one stream of Words typed in one Layout.
  /// </summary>
  /// <param name="Name">The stream, such as <c>ru typed in en</c>.</param>
  /// <param name="IsPositive">
  /// <see langword="true" /> if the Words were typed in the wrong Layout; otherwise,
  /// <see langword="false" />.
  /// </param>
  /// <param name="Words">The Words counted.</param>
  /// <param name="Hits">
  /// The Words corrected: to the intended text in a wrong-layout stream, at all in a correctly
  /// typed one.
  /// </param>
  internal sealed record StreamResult(string Name, bool IsPositive, int Words, int Hits);
}
