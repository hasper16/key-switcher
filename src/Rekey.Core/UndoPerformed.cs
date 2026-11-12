using System.Text;

namespace Rekey.Core {
  /// <summary>
  /// Reports an Undo to the features that learn from it and count it.
  /// </summary>
  /// <param name="Original">The text as the user typed it, now restored.</param>
  /// <param name="Rendering">The text the Correction had made of it.</param>
  public sealed record UndoPerformed(string Original, string Rendering) : EngineAction {
    /// <summary>
    /// Appends the lengths of the texts to the text form, so typed text cannot reach logs.
    /// </summary>
    /// <param name="builder">The builder that receives the text form.</param>
    /// <returns><see langword="true" /> because members were appended.</returns>
    protected override bool PrintMembers(StringBuilder builder) {
      builder.Append("Original.Length = ").Append(Original.Length)
          .Append(", Rendering.Length = ").Append(Rendering.Length);
      return true;
    }
  }
}
