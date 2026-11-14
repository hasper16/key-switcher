namespace Rekey.Core {
  /// <summary>
  /// Reports that the user's settings changed.
  /// </summary>
  /// <param name="Timestamp">The time of the change, measured from system start.</param>
  /// <param name="Settings">The settings now in effect.</param>
  public sealed record SettingsChanged(TimeSpan Timestamp, Settings Settings)
      : InputEvent(Timestamp);
}
