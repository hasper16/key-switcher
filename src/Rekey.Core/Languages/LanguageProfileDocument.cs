namespace Rekey.Core.Languages {
  /// <summary>
  /// Holds a Language Profile as its file stores it: format 1, the JSON contract shared by the
  /// profile generator and the loader.
  /// </summary>
  /// <param name="Format">The version of the file format.</param>
  /// <param name="Code">The Language the profile describes.</param>
  /// <param name="DisplayName">The English name of the Language.</param>
  /// <param name="LayoutIds">The Windows Layout ids (KLIDs) that type the Language.</param>
  /// <param name="Alphabet">The letters of the Language.</param>
  /// <param name="Words1">The valid one-letter words.</param>
  /// <param name="Words2">The valid two-letter words.</param>
  /// <param name="CommonWords">The common words, most frequent first.</param>
  /// <param name="Bigrams">The relative frequencies of letter pairs.</param>
  /// <param name="Trigrams">The relative frequencies of letter triples.</param>
  internal sealed record LanguageProfileDocument(
      int Format,
      Language Code,
      string DisplayName,
      IReadOnlyList<string> LayoutIds,
      string Alphabet,
      IReadOnlyList<string> Words1,
      IReadOnlyList<string> Words2,
      IReadOnlyList<string> CommonWords,
      IReadOnlyDictionary<string, double> Bigrams,
      IReadOnlyDictionary<string, double> Trigrams) {
    /// <summary>
    /// The only file format this version reads and writes.
    /// </summary>
    public const int CurrentFormat = 1;
  }
}
