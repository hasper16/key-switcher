namespace Rekey.Core {
  /// <summary>
  /// Reports that the user performed the Hotkey, asking for a Manual Correction.
  /// </summary>
  /// <remarks>
  /// The host holds back the keys typed after the Hotkey until the engine's actions are done.
  /// </remarks>
  /// <param name="Timestamp">The time of the Hotkey, measured from system start.</param>
  /// <param name="Selection">
  /// The text selected in the focused control, or <see langword="null" /> when nothing is
  /// selected or the control does not expose its selection. The default is
  /// <see langword="null" />.
  /// </param>
  public sealed record ManualCorrectionRequested(TimeSpan Timestamp, string? Selection = null)
      : InputEvent(Timestamp) {
    /// <inheritdoc />
    protected override bool PrintMembers(System.Text.StringBuilder builder) {
      // Typed text stays out of logs.
      base.PrintMembers(builder);
      builder.Append(", Selection.Length = ").Append(Selection?.Length ?? 0);
      return true;
    }
  }
}
