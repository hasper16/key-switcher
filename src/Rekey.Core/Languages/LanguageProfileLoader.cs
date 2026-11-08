using System.Collections.Frozen;
using System.Text.Json;

namespace Rekey.Core.Languages {
  /// <summary>
  /// Loads a Language Profile file into lookup structures.
  /// </summary>
  public static class LanguageProfileLoader {
    /// <summary>
    /// Reads, validates and normalises a Language Profile file.
    /// </summary>
    /// <param name="stream">The stream that holds the file.</param>
    /// <returns>The loaded profile.</returns>
    /// <exception cref="ProfileLoadException">
    /// The stream does not hold valid format 1 JSON, misses a field, or holds an invalid value.
    /// </exception>
    public static LanguageProfile Load(Stream stream) {
      LanguageProfileDocument document;
      try {
        document = LanguageProfileJson.Read(stream);
      } catch (JsonException e) {
        throw new ProfileLoadException("The profile is not a valid format 1 file.", e);
      }

      return ToProfile(document);
    }

    private static LanguageProfile ToProfile(LanguageProfileDocument document) {
      if (document.Format != LanguageProfileDocument.CurrentFormat) {
        throw new ProfileLoadException($"Format {document.Format} is not supported.");
      }

      if (!Enum.IsDefined(document.Code)) {
        throw new ProfileLoadException("The Language code is not supported.");
      }

      return new LanguageProfile(
          document.Code,
          document.DisplayName,
          ParseLayoutIds(document.LayoutIds),
          document.Alphabet,
          NormaliseAll(document.Words1),
          NormaliseAll(document.Words2),
          RankCommonWords(document.CommonWords),
          NormaliseKeys(document.Bigrams),
          NormaliseKeys(document.Trigrams));
    }

    private static FrozenSet<LayoutId> ParseLayoutIds(IReadOnlyList<string> texts) {
      if (texts.Count == 0) {
        throw new ProfileLoadException("The profile lists no Layout ids.");
      }

      return texts.Select(text => LayoutId.TryParse(text, out LayoutId id)
              ? id
              : throw new ProfileLoadException("A Layout id is not an 8-digit KLID."))
          .ToFrozenSet();
    }

    private static FrozenSet<string> NormaliseAll(IEnumerable<string> words) {
      return words.Select(WordNormaliser.Normalise).ToFrozenSet(StringComparer.Ordinal);
    }

    private static FrozenDictionary<string, int> RankCommonWords(IReadOnlyList<string> words) {
      var ranks = new Dictionary<string, int>(words.Count, StringComparer.Ordinal);
      for (int rank = 0; rank < words.Count; rank++) {
        ranks.TryAdd(WordNormaliser.Normalise(words[rank]), rank);
      }

      return ranks.ToFrozenDictionary(StringComparer.Ordinal);
    }

    private static FrozenDictionary<string, float> NormaliseKeys(
        IReadOnlyDictionary<string, double> frequencies) {
      return frequencies.ToFrozenDictionary(
          pair => WordNormaliser.Normalise(pair.Key),
          pair => (float)pair.Value,
          StringComparer.Ordinal);
    }
  }
}
