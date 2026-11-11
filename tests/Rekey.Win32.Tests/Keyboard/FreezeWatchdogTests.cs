using Rekey.Core;
using Rekey.Win32.Keyboard;
using Xunit;

namespace Rekey.Win32.Tests.Keyboard {
  // Spec keyboard-capture: "Restart after a repeated freeze"; design D7.
  public sealed class FreezeWatchdogTests {
    private readonly ManualTime _time = new();
    private readonly FakePulse _pulse = new();

    [Fact]
    public void StallOfFiveSecondsIsNotYetAFreeze() {
      FreezeWatchdog watchdog = NewWatchdog();

      Assert.Equal(FreezeOutcome.None, Stall(watchdog, TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public void StallOverFiveSecondsWithQueuedInputIsRecorded() {
      FreezeWatchdog watchdog = NewWatchdog();

      FreezeOutcome outcome = Stall(watchdog, TimeSpan.FromSeconds(5.5));

      Assert.False(outcome.Restart);
      Assert.Equal(IncidentType.InputFrozen, outcome.Incident?.Type);
    }

    [Fact]
    public void StallWithoutQueuedInputIsNotAFreeze() {
      FreezeWatchdog watchdog = NewWatchdog();
      watchdog.Check();
      _time.Advance(TimeSpan.FromSeconds(30));

      Assert.Equal(FreezeOutcome.None, watchdog.Check());
    }

    [Fact]
    public void LongFreezeIsRecordedOnce() {
      FreezeWatchdog watchdog = NewWatchdog();
      Stall(watchdog, TimeSpan.FromSeconds(6));

      _time.Advance(TimeSpan.FromSeconds(10));

      Assert.Equal(FreezeOutcome.None, watchdog.Check());
    }

    [Fact]
    public void SecondFreezeWithinAnHourRestarts() {
      FreezeWatchdog watchdog = NewWatchdog();
      Freeze(watchdog);
      _time.Advance(TimeSpan.FromMinutes(20));

      FreezeOutcome second = Freeze(watchdog);

      Assert.True(second.Restart);
      Assert.Equal(IncidentType.FreezeRestart, second.Incident?.Type);
    }

    [Fact]
    public void FreezesTwoHoursApartDoNotRestart() {
      FreezeWatchdog watchdog = NewWatchdog();
      Freeze(watchdog);
      _time.Advance(TimeSpan.FromHours(2));

      Assert.False(Freeze(watchdog).Restart);
    }

    [Fact]
    public void NoRestartWithinTenMinutesOfTheLastOne() {
      FreezeWatchdog watchdog = NewWatchdog(lastRestart: _time.GetUtcNow());
      _time.Advance(TimeSpan.FromMinutes(4));

      Freeze(watchdog);
      FreezeOutcome second = Freeze(watchdog);

      Assert.False(second.Restart);
      Assert.Equal(IncidentType.InputFrozen, second.Incident?.Type);
    }

    [Fact]
    public void RestartTenMinutesAfterTheLastOneIsAllowed() {
      FreezeWatchdog watchdog = NewWatchdog(lastRestart: _time.GetUtcNow());
      _time.Advance(TimeSpan.FromMinutes(10));

      Freeze(watchdog);

      Assert.True(Freeze(watchdog).Restart);
    }

    [Fact]
    public void TurnedOffRestartOnlyRecords() {
      FreezeWatchdog watchdog = NewWatchdog();
      watchdog.RestartEnabled = false;
      Freeze(watchdog);

      FreezeOutcome second = Freeze(watchdog);

      Assert.False(second.Restart);
      Assert.Equal(IncidentType.InputFrozen, second.Incident?.Type);
    }

    private FreezeWatchdog NewWatchdog(DateTimeOffset? lastRestart = null) {
      return new FreezeWatchdog(_pulse, _time, lastRestart);
    }

    // The worker stops with input waiting; the watchdog checks before and after.
    private FreezeOutcome Stall(FreezeWatchdog watchdog, TimeSpan length) {
      _pulse.Beat();
      _pulse.QueuedInput = 3;
      watchdog.Check();
      _time.Advance(length);
      return watchdog.Check();
    }

    // A freeze long enough to count, after which the worker recovers.
    private FreezeOutcome Freeze(FreezeWatchdog watchdog) {
      FreezeOutcome outcome = Stall(watchdog, TimeSpan.FromSeconds(6));
      _pulse.QueuedInput = 0;
      _pulse.Beat();
      watchdog.Check();
      return outcome;
    }

    private sealed class FakePulse : IWorkerPulse {
      public long Heartbeat { get; private set; }

      public int QueuedInput { get; set; }

      public void Beat() {
        Heartbeat++;
      }
    }
  }
}
