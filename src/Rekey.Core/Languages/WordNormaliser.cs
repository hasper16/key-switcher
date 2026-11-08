namespace Rekey.Core.Languages {
  /// <summary>
  /// Brings a word to the form in which Language Profiles store and look up words.
  /// </summary>
  /// <remarks>
  /// The profile generator and the runtime lookup both use this class, so generated data and
  /// lookups cannot drift apart.
  /// </remarks>
  public static class WordNormaliser {
    /// <summary>
    /// Returns the normalised form of a word: lower case, Russian "ё" as "е", and every apostrophe
    /// variant as <c>'</c>.
    /// </summary>
    /// <param name="word">The word to normalise.</param>
    /// <returns>The normalised word.</returns>
    public static string Normalise(string word) {
      return word.ToLowerInvariant()
          .Replace('ё', 'е')
          .Replace('’', '\'')
          .Replace('ʼ', '\'');
    }
  }
}
