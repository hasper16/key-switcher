using Rekey.Core;

namespace Rekey.Win32.Keyboard {
  /// <summary>
  /// Counts hook callbacks that took longer than 1 ms and reports them as one Incident per
  /// 10 minutes at most.
  /// </summary>
  /// <remarks>
  /// The hook thread is the only writer of the count; the worker thread only reads it.
  /// </remarks>
  internal sealed class SlowCallbackMeter {
    // Design decisions (D1): the slow threshold and the reporting period.
    private static readonly TimeSpan _slow = TimeSpan.FromMilliseconds(1);
    private static readonly TimeSpan _period = TimeSpan.FromMinutes(10);

    private readonly TimeProvider _time;
    private readonly long _slowTicks;
    private long _slowCount;
    private long _reportedCount;
    private DateTimeOffset? _lastReport;

    public SlowCallbackMeter(TimeProvider time) {
      _time = time;
      _slowTicks = (long)(_slow.TotalSeconds * time.TimestampFrequency);
    }

    /// <summary>Records one callback; called only by the hook thread.</summary>
    public void Record(long started, long ended) {
      if (ended - started > _slowTicks) {
        Volatile.Write(ref _slowCount, _slowCount + 1);
      }
    }

    /// <summary>
    /// Returns an Incident when slow callbacks happened since the last report and the last
    /// report is at least 10 minutes old; called only by the worker thread.
    /// </summary>
    public Incident? TakeIncident() {
      long count = Volatile.Read(ref _slowCount);
      DateTimeOffset now = _time.GetUtcNow();
      if (count == _reportedCount || now - _lastReport < _period) {
        return null;
      }

      _reportedCount = count;
      _lastReport = now;
      return new Incident(now, IncidentType.SlowHookCallback, []);
    }
  }
}
