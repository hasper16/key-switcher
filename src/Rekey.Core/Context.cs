namespace Rekey.Core {
  /// <summary>
  /// Describes the focused control the user is typing into.
  /// </summary>
  /// <param name="AppId">The full image path of the process that owns the focused window.</param>
  /// <param name="IsPasswordField">
  /// <see langword="true" /> if the focused control is a Password Field; otherwise,
  /// <see langword="false" />.
  /// </param>
  /// <param name="IsElevated">
  /// <see langword="true" /> if the focused window's process has higher privileges than Rekey;
  /// otherwise, <see langword="false" />.
  /// </param>
  public sealed record Context(string AppId, bool IsPasswordField, bool IsElevated);
}
