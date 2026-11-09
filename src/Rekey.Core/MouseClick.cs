namespace Rekey.Core {
  /// <summary>
  /// Reports that the user pressed a mouse button.
  /// </summary>
  /// <param name="Timestamp">The time of the press, measured from system start.</param>
  public sealed record MouseClick(TimeSpan Timestamp) : InputEvent(Timestamp);
}
