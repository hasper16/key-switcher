namespace Rekey.Core {
  /// <summary>
  /// Describes the state the engine needs besides the input itself.
  /// </summary>
  /// <param name="IsPasswordField">
  /// <see langword="true" /> if the focused control is a Password Field; otherwise,
  /// <see langword="false" />.
  /// </param>
  public sealed record Context(bool IsPasswordField);
}
