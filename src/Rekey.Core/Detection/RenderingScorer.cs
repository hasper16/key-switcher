using System.Collections.Frozen;
using Rekey.Core.Languages;

namespace Rekey.Core.Detection {
  /// <summary>
  /// Scores how plausible a Rendering is as text in a Language.
  /// </summary>
  public static class RenderingScorer {
    // A letter group missing from the profile counts as this share, so one unseen group lowers
    // the score without making it minus infinity.
    private const double _unseenShare = 1e-6;

    // Map the mean log share of letter groups to (0, 1). Initial values; the accuracy harness
    // tunes them (design D2).
    private const double _nGramSlope = 1.5;
    private const double _nGramMidpoint = -10.5;

    // A Learned Word scores as a mid-frequency common word (design D2 of add-word-learning).
    private const int _learnedRank = 5_000;

    /// <summary>
    /// Returns the Score of a Rendering in a Language.
    /// </summary>
    /// <remarks>
    /// A text of one or two letters is a word only when the profile lists it as a short word;
    /// a longer text, when it is a common word.
    /// </remarks>
    /// <param name="profile">The Language Profile to score against.</param>
    /// <param name="letters">The Rendering without edge punctuation, in any case.</param>
    /// <returns>The Score.</returns>
    public static RenderingScore Score(LanguageProfile profile, string letters) {
      return Score(profile, letters, FrozenSet<string>.Empty);
    }

    /// <summary>
    /// Returns the Score of a Rendering in a Language, counting the user's Learned Words of that
    /// Language as common words.
    /// </summary>
    /// <param name="profile">The Language Profile to score against.</param>
    /// <param name="letters">The Rendering without edge punctuation, in any case.</param>
    /// <param name="learned">The Learned Words of the Language, normalised.</param>
    /// <returns>The Score.</returns>
    public static RenderingScore Score(
        LanguageProfile profile, string letters, IReadOnlySet<string> learned) {
      string word = WordNormaliser.Normalise(letters);
      bool isWord = word.Length <= 2
          ? profile.Words1.Contains(word) || profile.Words2.Contains(word)
          : profile.CommonWordRanks.ContainsKey(word) || IsSplitContraction(profile, word);
      if (isWord) {
        return new RenderingScore(true, 1 + RankBonus(profile, word));
      }

      if (learned.Contains(word)) {
        return new RenderingScore(true, 1 + RankBonus(profile.CommonWordRanks.Count, _learnedRank));
      }

      double nGram = NGramScore(profile, word);
      return new RenderingScore(false, Sigmoid(_nGramSlope * (nGram - _nGramMidpoint)));
    }

    /// <summary>
    /// Returns the mean natural logarithm of the shares of a text's letter pairs and triples.
    /// </summary>
    /// <param name="profile">The Language Profile that holds the shares.</param>
    /// <param name="letters">The text, in any case.</param>
    /// <returns>
    /// The mean log share; a group the profile lacks counts as a share of one in a million.
    /// </returns>
    public static double NGramScore(LanguageProfile profile, string letters) {
      string word = WordNormaliser.Normalise(letters);
      double sum = 0;
      int count = 0;
      for (int i = 0; i + 2 <= word.Length; i++) {
        sum += LogShare(profile.Bigrams, word.Substring(i, 2));
        count++;
      }

      for (int i = 0; i + 3 <= word.Length; i++) {
        sum += LogShare(profile.Trigrams, word.Substring(i, 3));
        count++;
      }

      return count == 0 ? Math.Log(_unseenShare) : sum / count;
    }

    // Some word lists split contractions at the apostrophe ("don't" became "don" and "t").
    private static bool IsSplitContraction(LanguageProfile profile, string word) {
      string[] parts = word.Split('\'');
      return parts.Length > 1
          && parts.All(part => part.Length > 0 && profile.CommonWordRanks.ContainsKey(part));
    }

    private static double LogShare(FrozenDictionary<string, float> shares, string group) {
      return Math.Log(shares.TryGetValue(group, out float share) ? share : _unseenShare);
    }

    private static double RankBonus(LanguageProfile profile, string word) {
      return profile.CommonWordRanks.TryGetValue(word, out int rank)
          ? RankBonus(profile.CommonWordRanks.Count, rank)
          : 0;
    }

    private static double RankBonus(int count, int rank) {
      return count > 1 ? Math.Max(0, 1 - (Math.Log(rank + 1) / Math.Log(count))) : 0;
    }

    private static double Sigmoid(double x) {
      return 1 / (1 + Math.Exp(-x));
    }
  }
}
