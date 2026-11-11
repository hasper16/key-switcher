namespace Rekey.Core {
  /// <summary>
  /// Reports that some input never reached Rekey, as when its queue was full, so the current
  /// Word may be missing Keystrokes.
  /// </summary>
  /// <param name="Timestamp">The time the loss was noticed, measured from system start.</param>
  public sealed record InputLost(TimeSpan Timestamp) : InputEvent(Timestamp);
}
