using Rekey.Core;

namespace Rekey.Win32.Keyboard {
  /// <summary>
  /// Notices when Windows silently removed the low-level hooks and pauses Rekey itself after
  /// repeated failures.
  /// </summary>
  /// <remarks>
  /// The worker calls <see cref="Check" /> every 250 ms. Not thread-safe: the worker owns it.
  /// </remarks>
  internal sealed class HookWatchdog {
    // Design decisions (D4): how long the system may see input the hook does not, how close two
    // failures must be to pause, and how long the pause lasts.
    private const long _dropAfterMilliseconds = 1500;
    private static readonly TimeSpan _failureWindow = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan _pause = TimeSpan.FromMinutes(10);

    private readonly IInputActivity _activity;
    private readonly TimeProvider _time;

    // System input up to this tick is already accounted for: it went to an elevated window, came
    // before a reinstall, or arrived while the watchdog was off.
    private long _accountedTick = long.MinValue;
    private DateTimeOffset? _lastFailure;

    public HookWatchdog(IInputActivity activity, TimeProvider time) {
      _activity = activity;
      _time = time;
    }

    /// <summary>Gets or sets a value indicating whether the watchdog checks the hooks.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets the time the self-pause ends, or <see langword="null" /> when Rekey is not paused.
    /// </summary>
    public DateTimeOffset? SelfPausedUntil { get; private set; }

    /// <summary>Checks whether the hooks still receive input.</summary>
    /// <param name="foregroundElevated">
    /// <see langword="true" /> if the foreground window is more privileged than Rekey, so Windows
    /// hides its input from the hooks; otherwise, <see langword="false" />.
    /// </param>
    /// <returns>What the worker must do.</returns>
    public WatchdogOutcome Check(bool foregroundElevated) {
      DateTimeOffset now = _time.GetUtcNow();
      if (SelfPausedUntil is { } until) {
        if (now < until) {
          return WatchdogOutcome.None;
        }

        SelfPausedUntil = null;
        return Reattach([]);
      }

      if (!Enabled || foregroundElevated) {
        _accountedTick = _activity.SystemLastInputTick;
        return WatchdogOutcome.None;
      }

      long seen = Math.Max(_activity.HookLastInputTick, _accountedTick);
      if (_activity.SystemLastInputTick - seen <= _dropAfterMilliseconds) {
        return WatchdogOutcome.None;
      }

      List<Incident> incidents = [new Incident(now, IncidentType.HookReinstalled, [])];
      if (now - _lastFailure < _failureWindow) {
        SelfPausedUntil = now + _pause;
        incidents.Add(new Incident(now, IncidentType.SelfPaused, []));
        _lastFailure = null;
      } else {
        _lastFailure = now;
      }

      return Reattach(incidents);
    }

    /// <summary>Ends a self-pause at the user's request and reattaches at once.</summary>
    /// <returns>What the worker must do.</returns>
    public WatchdogOutcome Resume() {
      SelfPausedUntil = null;
      _lastFailure = null;
      return Reattach([]);
    }

    private WatchdogOutcome Reattach(IReadOnlyList<Incident> incidents) {
      _accountedTick = _activity.SystemLastInputTick;
      return new WatchdogOutcome(true, incidents);
    }
  }
}
