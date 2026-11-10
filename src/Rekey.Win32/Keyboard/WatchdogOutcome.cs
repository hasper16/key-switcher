using Rekey.Core;

namespace Rekey.Win32.Keyboard {
  /// <summary>
  /// Holds what a watchdog check asks the worker to do.
  /// </summary>
  /// <param name="Reinstall">
  /// <see langword="true" /> to remove and install the hooks again; otherwise,
  /// <see langword="false" />.
  /// </param>
  /// <param name="Incidents">The Incidents to record.</param>
  internal sealed record WatchdogOutcome(bool Reinstall, IReadOnlyList<Incident> Incidents) {
    /// <summary>Gets the outcome that asks for nothing.</summary>
    public static WatchdogOutcome None { get; } = new(false, []);

    /// <inheritdoc />
    public bool Equals(WatchdogOutcome? other) {
      return other is not null
          && Reinstall == other.Reinstall
          && Incidents.SequenceEqual(other.Incidents);
    }

    /// <inheritdoc />
    public override int GetHashCode() {
      return HashCode.Combine(Reinstall, Incidents.Count);
    }
  }
}
