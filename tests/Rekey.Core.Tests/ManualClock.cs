namespace Rekey.Core.Tests {
  // A wall clock the test moves by hand; its timers fire while it moves.
  internal sealed class ManualClock : TimeProvider {
    private readonly List<ManualTimer> _timers = [];

    public DateTimeOffset Now { get; private set; } = new(2026, 11, 12, 12, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() {
      return Now;
    }

    public override ITimer CreateTimer(
        TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) {
      var timer = new ManualTimer(this, () => callback(state));
      timer.Change(dueTime, period);
      _timers.Add(timer);
      return timer;
    }

    public void Advance(TimeSpan by) {
      DateTimeOffset end = Now + by;
      while (_timers.Where(timer => timer.Due <= end).MinBy(timer => timer.Due) is { } next) {
        Now = next.Due!.Value;
        next.Fire();
      }

      Now = end;
    }

    // Fires once at its due time; periodic timers are not needed yet.
    private sealed class ManualTimer(ManualClock clock, Action fire) : ITimer {
      public DateTimeOffset? Due { get; private set; }

      public bool Change(TimeSpan dueTime, TimeSpan period) {
        Due = dueTime == Timeout.InfiniteTimeSpan ? null : clock.Now + dueTime;
        return true;
      }

      public void Fire() {
        Due = null;
        fire();
      }

      public void Dispose() {
        Due = null;
      }

      public ValueTask DisposeAsync() {
        Dispose();
        return ValueTask.CompletedTask;
      }
    }
  }
}
