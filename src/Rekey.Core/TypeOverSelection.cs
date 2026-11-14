using System.Text;

namespace Rekey.Core {
  /// <summary>
  /// Asks the host to type a text over the selection of the focused control.
  /// </summary>
  /// <remarks>
  /// The host reports the outcome with <see cref="ReplacementFinished" />.
  /// </remarks>
  /// <param name="Selected">The selected text the host read.</param>
  /// <param name="Replacement">The text to type over it.</param>
  public sealed record TypeOverSelection(string Selected, string Replacement) : EngineAction {
    /// <inheritdoc />
    protected override bool PrintMembers(StringBuilder builder) {
      // Typed text stays out of logs.
      builder.Append("Selected.Length = ").Append(Selected.Length)
          .Append(", Replacement.Length = ").Append(Replacement.Length);
      return true;
    }
  }
}
