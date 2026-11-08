using System.Collections.Frozen;

namespace Rekey.Core.Languages {
  /// <summary>
  /// Holds the recognition data for one Language, ready for lookups.
  /// </summary>
  /// <remarks>
  /// Words and letter groups are stored normalised by <see cref="WordNormaliser" />.
  /// </remarks>
  public sealed record LanguageProfile {
    internal LanguageProfile(
        Language language,
        string displayName,
        FrozenSet<LayoutId> layoutIds,
        string alphabet,
        FrozenSet<string> words1,
        FrozenSet<string> words2,
        FrozenDictionary<string, int> commonWordRanks,
        FrozenDictionary<string, float> bigrams,
        FrozenDictionary<string, float> trigrams) {
      Language = language;
      DisplayName = displayName;
      LayoutIds = layoutIds;
      Alphabet = alphabet;
      Words1 = words1;
      Words2 = words2;
      CommonWordRanks = commonWordRanks;
      Bigrams = bigrams;
      Trigrams = trigrams;
    }

    /// <summary>
    /// Gets the Language the profile describes.
    /// </summary>
    public Language Language { get; }

    /// <summary>
    /// Gets the English name of the Language.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the Layouts listed as typing the Language.
    /// </summary>
    public FrozenSet<LayoutId> LayoutIds { get; }

    /// <summary>
    /// Gets the letters of the Language.
    /// </summary>
    public string Alphabet { get; }

    /// <summary>
    /// Gets the valid one-letter words.
    /// </summary>
    public FrozenSet<string> Words1 { get; }

    /// <summary>
    /// Gets the valid two-letter words.
    /// </summary>
    public FrozenSet<string> Words2 { get; }

    /// <summary>
    /// Gets the frequency rank of each common word; the most frequent word has rank 0.
    /// </summary>
    public FrozenDictionary<string, int> CommonWordRanks { get; }

    /// <summary>
    /// Gets the relative frequency of each frequent letter pair.
    /// </summary>
    public FrozenDictionary<string, float> Bigrams { get; }

    /// <summary>
    /// Gets the relative frequency of each frequent letter triple.
    /// </summary>
    public FrozenDictionary<string, float> Trigrams { get; }

    /// <summary>
    /// Returns a value indicating whether a word is a valid one- or two-letter word.
    /// </summary>
    /// <param name="word">The word in any case.</param>
    /// <returns>
    /// <see langword="true" /> if the word is a valid short word; otherwise,
    /// <see langword="false" />.
    /// </returns>
    public bool ContainsShortWord(string word) {
      string normalised = WordNormaliser.Normalise(word);
      return Words1.Contains(normalised) || Words2.Contains(normalised);
    }

    /// <summary>
    /// Returns a value indicating whether a word is one of the common words.
    /// </summary>
    /// <param name="word">The word in any case.</param>
    /// <returns>
    /// <see langword="true" /> if the word is a common word; otherwise, <see langword="false" />.
    /// </returns>
    public bool ContainsCommonWord(string word) {
      return CommonWordRanks.ContainsKey(WordNormaliser.Normalise(word));
    }
  }
}
