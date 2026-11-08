using System.Globalization;
using Rekey.Core.Languages;

namespace Rekey.ProfileBuilder {
  /// <summary>
  /// Builds a Language Profile from a word-frequency list and a curated short-word list.
  /// </summary>
  internal static class ProfileGenerator {
    /// <summary>
    /// Returns the Language Profile built from the given lists.
    /// </summary>
    /// <param name="definition">The hand-written facts about the Language.</param>
    /// <param name="frequencyLines">The lines of a <c>word count</c> frequency list.</param>
    /// <param name="shortWordLines">
    /// The lines of the curated one- and two-letter word list; blank lines and lines starting with
    /// <c>#</c> are skipped.
    /// </param>
    /// <param name="options">The generator settings.</param>
    /// <returns>The profile, ready to write.</returns>
    /// <exception cref="InvalidDataException">
    /// A short word is longer than two letters or has a character outside the alphabet.
    /// </exception>
    public static LanguageProfileDocument Build(
        LanguageDefinition definition,
        IEnumerable<string> frequencyLines,
        IEnumerable<string> shortWordLines,
        BuildOptions options) {
      var letters = WordNormaliser.Normalise(definition.Alphabet).ToHashSet();
      KeyValuePair<string, long>[] words = TopByCount(
          CountWords(frequencyLines, letters).Where(pair => pair.Value >= options.MinCount),
          options.TopWords);
      string[] shortWords = ParseShortWords(shortWordLines, letters);
      return new LanguageProfileDocument(
          LanguageProfileDocument.CurrentFormat,
          definition.Code,
          definition.DisplayName,
          definition.LayoutIds,
          definition.Alphabet,
          [.. shortWords.Where(word => word.Length == 1)],
          [.. shortWords.Where(word => word.Length == 2)],
          [.. words.Select(pair => pair.Key)],
          NGramFrequencies(words, 2, letters, options.TopNGrams),
          NGramFrequencies(words, 3, letters, options.TopNGrams));
    }

    private static Dictionary<string, long> CountWords(
        IEnumerable<string> lines, HashSet<char> letters) {
      var counts = new Dictionary<string, long>(StringComparer.Ordinal);
      foreach (string[] parts in lines.Select(line => line.Split(' '))) {
        long n = 0;
        if (parts.Length == 2
            && long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out n)) {
          string word = WordNormaliser.Normalise(parts[0]);
          if (IsWord(word, letters)) {
            // Lists count "ё" and "е" spellings apart; normalised, they are one word.
            counts[word] = counts.GetValueOrDefault(word) + n;
          }
        }
      }

      return counts;
    }

    // Letters only, with an apostrophe or hyphen allowed between letters.
    private static bool IsWord(string word, HashSet<char> letters) {
      if (word.Length == 0 || !letters.Contains(word[0]) || !letters.Contains(word[^1])) {
        return false;
      }

      return word.All(c => letters.Contains(c) || c is '\'' or '-');
    }

    private static string[] ParseShortWords(IEnumerable<string> lines, HashSet<char> letters) {
      string[] words = [.. lines
          .Select(line => line.Trim())
          .Where(line => line.Length > 0 && !line.StartsWith('#'))
          .Select(WordNormaliser.Normalise)
          .Distinct(StringComparer.Ordinal)
          .Order(StringComparer.Ordinal)];
      foreach (string word in words) {
        if (word.Length > 2 || !word.All(letters.Contains)) {
          throw new InvalidDataException(
              $"Short word '{word}' is longer than two letters or not in the alphabet.");
        }
      }

      return words;
    }

    private static SortedDictionary<string, double> NGramFrequencies(
        IEnumerable<KeyValuePair<string, long>> words, int n, HashSet<char> letters, int top) {
      var counts = new Dictionary<string, long>(StringComparer.Ordinal);
      foreach ((string word, long count) in words) {
        for (int i = 0; i + n <= word.Length; i++) {
          string gram = word.Substring(i, n);
          if (gram.All(letters.Contains)) {
            counts[gram] = counts.GetValueOrDefault(gram) + count;
          }
        }
      }

      double total = counts.Values.Sum();
      return new SortedDictionary<string, double>(
          TopByCount(counts, top).ToDictionary(pair => pair.Key, pair => Round(pair.Value / total)),
          StringComparer.Ordinal);
    }

    // Count descending, then ordinal, so ties never depend on input order.
    private static KeyValuePair<string, long>[] TopByCount(
        IEnumerable<KeyValuePair<string, long>> counts, int top) {
      return [.. counts
          .OrderByDescending(pair => pair.Value)
          .ThenBy(pair => pair.Key, StringComparer.Ordinal)
          .Take(top)];
    }

    // Six significant digits: enough for scoring, short and stable in the file.
    private static double Round(double value) {
      return double.Parse(
          value.ToString("G6", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
    }
  }
}
