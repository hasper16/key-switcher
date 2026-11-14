using System.Text;
using Rekey.Core.Languages;

namespace Rekey.Core.Words {
  /// <summary>
  /// A Learned Word: a word Rekey accepts as valid in a Language although its Language Profile
  /// lacks it.
  /// </summary>
  /// <param name="Text">The word, normalised, without punctuation at its edges.</param>
  /// <param name="Language">The Language the word belongs to.</param>
  /// <param name="Source">Who added the word.</param>
  /// <param name="LastUsed">When the word was added or last made a Correction.</param>
  public sealed record LearnedWord(
      string Text, Language Language, WordSource Source, DateTimeOffset LastUsed) {
    /// <summary>
    /// Appends the members without the word, so it cannot reach logs.
    /// </summary>
    /// <param name="builder">The builder that receives the text form.</param>
    /// <returns><see langword="true" /> because members were appended.</returns>
    private bool PrintMembers(StringBuilder builder) {
      builder.Append("Text.Length = ").Append(Text.Length)
          .Append(", Language = ").Append(Language)
          .Append(", Source = ").Append(Source);
      return true;
    }
  }
}
