namespace Rekey.Core {
  /// <summary>
  /// Reports how the host executed the latest <see cref="ReplaceText" />.
  /// </summary>
  /// <param name="Timestamp">The time the replacement finished, measured from system start.</param>
  /// <param name="Applied">
  /// <see langword="true" /> if the text was replaced; <see langword="false" /> if the host
  /// cancelled it, because the text before the caret differed or the key that asked for it was
  /// too old.
  /// </param>
  public sealed record ReplacementFinished(TimeSpan Timestamp, bool Applied)
      : InputEvent(Timestamp);
}
