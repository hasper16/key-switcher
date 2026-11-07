namespace Rekey.Core {
  /// <summary>
  /// Describes one input the engine reacts to.
  /// </summary>
  /// <param name="Timestamp">The time of the input, measured from system start.</param>
  public abstract record InputEvent(TimeSpan Timestamp);
}
