using System.Text;

namespace Rekey.Core.Words {
  /// <summary>
  /// A Protected Word: a word Rekey never auto-corrects.
  /// </summary>
  /// <param name="Text">The word, normalised, without punctuation at its edges.</param>
  /// <param name="Source">Who added the word.</param>
  /// <param name="LastUsed">When the word was added or last kept a Word as typed.</param>
  public sealed record ProtectedWord(string Text, WordSource Source, DateTimeOffset LastUsed) {
    /// <summary>
    /// Appends the members without the word, so it cannot reach logs.
    /// </summary>
    /// <param name="builder">The builder that receives the text form.</param>
    /// <returns><see langword="true" /> because members were appended.</returns>
    private bool PrintMembers(StringBuilder builder) {
      builder.Append("Text.Length = ").Append(Text.Length)
          .Append(", Source = ").Append(Source);
      return true;
    }
  }
}
