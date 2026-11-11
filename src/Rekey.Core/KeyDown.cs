using System.Text;
using Rekey.Core.Detection;

namespace Rekey.Core {
  /// <summary>
  /// Reports a key the user pressed.
  /// </summary>
  /// <param name="Timestamp">The time of the press, measured from system start.</param>
  /// <param name="Keystroke">The key position and the Shift, Caps Lock and AltGr state.</param>
  /// <param name="Kind">
  /// One of the enumeration values that specifies what the key means for the current Word.
  /// </param>
  /// <param name="Modifiers">
  /// A bitwise combination of the enumeration values that specifies the modifier keys held down.
  /// AltGr is reported in <paramref name="Keystroke" />, not as Ctrl and Alt.
  /// </param>
  /// <param name="FocusConfirmed">
  /// <see langword="true" /> if the host confirmed that the key went to a control that is not a
  /// Password Field; otherwise, <see langword="false" />. The default is
  /// <see langword="true" />.
  /// </param>
  /// <param name="Held">
  /// <see langword="true" /> if the hook held the key back, so it has not reached the application
  /// yet; otherwise, <see langword="false" />. The default is <see langword="false" />.
  /// </param>
  public sealed record KeyDown(
      TimeSpan Timestamp,
      Keystroke Keystroke,
      KeyKind Kind,
      KeyModifiers Modifiers,
      bool FocusConfirmed = true,
      bool Held = false)
      : InputEvent(Timestamp) {
    /// <summary>
    /// Appends the members to the text form without the Keystroke, so typed text cannot reach
    /// logs.
    /// </summary>
    /// <param name="builder">The builder that receives the text form.</param>
    /// <returns><see langword="true" /> because members were appended.</returns>
    protected override bool PrintMembers(StringBuilder builder) {
      base.PrintMembers(builder);
      builder.Append(", Kind = ").Append(Kind).Append(", Modifiers = ").Append(Modifiers)
          .Append(", FocusConfirmed = ").Append(FocusConfirmed).Append(", Held = ").Append(Held);
      return true;
    }
  }
}
