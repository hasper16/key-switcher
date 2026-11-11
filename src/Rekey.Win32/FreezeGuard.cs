using Rekey.Core;
using Rekey.Win32.Keyboard;

namespace Rekey.Win32 {
  /// <summary>
  /// Watches the worker thread from a thread of its own and asks the host to restart Rekey after
  /// a repeated freeze.
  /// </summary>
  public sealed class FreezeGuard : IDisposable {
    private static readonly TimeSpan _checkPeriod = TimeSpan.FromMilliseconds(500);

    private readonly FreezeWatchdog _watchdog;
    private readonly Action<Incident> _recordIncident;
    private readonly Action _restart;
    private readonly Thread _thread;
    private readonly ManualResetEventSlim _stop = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="FreezeGuard" /> class.
    /// </summary>
    /// <param name="worker">The worker to watch.</param>
    /// <param name="settings">
    /// The settings that say whether a repeated freeze restarts Rekey.
    /// </param>
    /// <param name="lastRestart">
    /// The time of the last restart after a freeze, or <see langword="null" /> when none.
    /// </param>
    /// <param name="recordIncident">
    /// The sink that records Incidents; it is called on the guard's thread and must write before
    /// it returns, because a restart follows.
    /// </param>
    /// <param name="restart">
    /// The host's restart; called on the guard's thread. It may return without restarting.
    /// </param>
    /// <param name="time">The clock; <see cref="TimeProvider.System" /> outside tests.</param>
    public FreezeGuard(
        InputWorker worker,
        Settings settings,
        DateTimeOffset? lastRestart,
        Action<Incident> recordIncident,
        Action restart,
        TimeProvider time) {
      _watchdog = new FreezeWatchdog(worker, time, lastRestart) {
        RestartEnabled = settings.RestartAfterFreezeEnabled,
      };
      _recordIncident = recordIncident;
      _restart = restart;
      _thread = new Thread(Run) { Name = "Rekey freeze guard", IsBackground = true };
    }

    /// <summary>Starts watching.</summary>
    public void Start() {
      _thread.Start();
    }

    /// <inheritdoc />
    public void Dispose() {
      // Bounded, so quitting Rekey always works; the guard is a background thread.
      _stop.Set();
      if (_thread.IsAlive && !_thread.Join(TimeSpan.FromSeconds(2))) {
        return;
      }

      _stop.Dispose();
    }

    private void Run() {
      while (!_stop.Wait(_checkPeriod)) {
        FreezeOutcome outcome = _watchdog.Check();
        if (outcome.Incident is { } incident) {
          _recordIncident(incident);
        }

        // The host may decline to restart, so the guard keeps watching afterwards.
        if (outcome.Restart) {
          _restart();
        }
      }
    }
  }
}
