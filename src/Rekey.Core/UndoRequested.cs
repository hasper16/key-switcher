namespace Rekey.Core {
  /// <summary>
  /// Reports a Ctrl+Z that the hook held back because it came right after a Correction.
  /// </summary>
  /// <remarks>
  /// The host passes the Ctrl+Z on to the application unless the engine performs the Undo.
  /// </remarks>
  /// <param name="Timestamp">The time of the press, measured from system start.</param>
  public sealed record UndoRequested(TimeSpan Timestamp) : InputEvent(Timestamp);
}
