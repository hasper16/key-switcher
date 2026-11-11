using System.Text;

namespace Rekey.Core {
  /// <summary>
  /// Asks the host to replace the text just before the caret, verifying it first where the
  /// focused control exposes its text, and then report <see cref="ReplacementFinished" />.
  /// </summary>
  /// <param name="Expected">
  /// The text Rekey expects just before the caret; the host erases exactly this many characters.
  /// </param>
  /// <param name="Replacement">The text to type in its place.</param>
  public sealed record ReplaceText(string Expected, string Replacement) : EngineAction {
    /// <summary>
    /// Appends the lengths of the texts to the text form, so typed text cannot reach logs.
    /// </summary>
    /// <param name="builder">The builder that receives the text form.</param>
    /// <returns><see langword="true" /> because members were appended.</returns>
    protected override bool PrintMembers(StringBuilder builder) {
      builder.Append("Expected.Length = ").Append(Expected.Length)
          .Append(", Replacement.Length = ").Append(Replacement.Length);
      return true;
    }
  }
}
