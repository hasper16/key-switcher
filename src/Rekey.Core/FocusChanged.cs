namespace Rekey.Core {
  /// <summary>
  /// Reports that keyboard focus moved to another control or window.
  /// </summary>
  /// <param name="Timestamp">The time of the change, measured from system start.</param>
  /// <param name="AppId">The full image path of the process that owns the focused window.</param>
  /// <param name="IsPassword">
  /// <see langword="true" /> if the focused control is a Password Field; otherwise,
  /// <see langword="false" />.
  /// </param>
  /// <param name="IsElevated">
  /// <see langword="true" /> if the focused window's process has higher privileges than Rekey;
  /// otherwise, <see langword="false" />.
  /// </param>
  public sealed record FocusChanged(
      TimeSpan Timestamp, string AppId, bool IsPassword, bool IsElevated)
      : InputEvent(Timestamp);
}
