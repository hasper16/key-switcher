namespace Rekey.Core {
  /// <summary>
  /// Reports that Rekey paused itself after repeated keyboard failures, or that the pause ended.
  /// </summary>
  /// <param name="Timestamp">The time of the change, measured from system start.</param>
  /// <param name="Paused">
  /// <see langword="true" /> if Rekey is now paused; <see langword="false" /> if the pause
  /// ended.
  /// </param>
  public sealed record SelfPauseChanged(TimeSpan Timestamp, bool Paused) : InputEvent(Timestamp);
}
