using Rekey.Core.Languages;

namespace Rekey.ProfileBuilder {
  /// <summary>
  /// Holds the hand-written facts about a Language that word lists cannot give.
  /// </summary>
  /// <param name="Code">The Language.</param>
  /// <param name="DisplayName">The English name of the Language.</param>
  /// <param name="Alphabet">The letters of the Language.</param>
  /// <param name="LayoutIds">The Windows Layout ids (KLIDs) that type the Language.</param>
  /// <param name="FrequencyList">The file name of the word-frequency list.</param>
  internal sealed record LanguageDefinition(
      Language Code,
      string DisplayName,
      string Alphabet,
      IReadOnlyList<string> LayoutIds,
      string FrequencyList);
}
