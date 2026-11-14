namespace Rekey.Core.Words {
  /// <summary>
  /// A snapshot of the Protected Words and Learned Words, as <c>words.json</c> stores them.
  /// </summary>
  /// <param name="Protected">The Protected Words.</param>
  /// <param name="Learned">The Learned Words.</param>
  public sealed record WordListsDocument(
      IReadOnlyList<ProtectedWord> Protected, IReadOnlyList<LearnedWord> Learned) {
    /// <summary>
    /// Gets a snapshot with both lists empty.
    /// </summary>
    public static WordListsDocument Empty { get; } = new([], []);
  }
}
