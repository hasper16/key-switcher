using System.Text;

namespace Rekey.Core {
  /// <summary>
  /// Reports a key the user pressed.
  /// </summary>
  /// <param name="Timestamp">The time of the press, measured from system start.</param>
  /// <param name="Character">
  /// The character the key produces in the current Layout, or <see langword="null" /> when it
  /// produces none.
  /// </param>
  /// <param name="Modifiers">
  /// A bitwise combination of the enumeration values that specifies the modifier keys held down.
  /// </param>
  public sealed record KeyDown(TimeSpan Timestamp, char? Character, KeyModifiers Modifiers)
      : InputEvent(Timestamp) {
    /// <summary>
    /// Appends the members to the text form without the typed character, so typed text cannot
    /// reach logs.
    /// </summary>
    /// <param name="builder">The builder that receives the text form.</param>
    /// <returns><see langword="true" /> because members were appended.</returns>
    protected override bool PrintMembers(StringBuilder builder) {
      base.PrintMembers(builder);
      builder.Append(", Modifiers = ").Append(Modifiers);
      return true;
    }
  }
}
