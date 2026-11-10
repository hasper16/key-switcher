using Rekey.Core;

namespace Rekey.Win32.Keyboard {
  /// <summary>
  /// Holds what a freeze check asks the host to do.
  /// </summary>
  /// <param name="Restart">
  /// <see langword="true" /> to restart Rekey; otherwise, <see langword="false" />.
  /// </param>
  /// <param name="Incident">The Incident to record, if any.</param>
  internal sealed record FreezeOutcome(bool Restart, Incident? Incident) {
    /// <summary>Gets the outcome that asks for nothing.</summary>
    public static FreezeOutcome None { get; } = new(false, null);
  }
}
