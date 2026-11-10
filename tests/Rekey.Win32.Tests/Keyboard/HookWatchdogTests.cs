using Rekey.Core;
using Rekey.Win32.Keyboard;
using Xunit;

namespace Rekey.Win32.Tests.Keyboard {
  // Spec keyboard-capture: "Keyboard observation recovers automatically" and "Self-pause after
  // repeated failures"; design D4.
  public sealed class HookWatchdogTests {
    private readonly ManualTime _time = new();
    private readonly FakeActivity _activity = new();
    private readonly HookWatchdog _watchdog;

    public HookWatchdogTests() {
      _watchdog = new HookWatchdog(_activity, _time);
    }

    [Fact]
    public void HookSilentWhileTheUserTypesIsReinstalled() {
      _activity.UserTypesUnseen(TimeSpan.FromMilliseconds(1600));

      WatchdogOutcome outcome = _watchdog.Check(foregroundElevated: false);

      Assert.True(outcome.Reinstall);
      Assert.Equal([IncidentType.HookReinstalled], Types(outcome));
    }

    [Fact]
    public void ShortGapIsNotADrop() {
      _activity.UserTypesUnseen(TimeSpan.FromMilliseconds(1500));

      Assert.Equal(WatchdogOutcome.None, _watchdog.Check(foregroundElevated: false));
    }

    [Fact]
    public void ElevatedForegroundIsNotADrop() {
      _activity.UserTypesUnseen(TimeSpan.FromSeconds(5));

      Assert.Equal(WatchdogOutcome.None, _watchdog.Check(foregroundElevated: true));
    }

    [Fact]
    public void TypingInAnElevatedWindowIsNotADropAfterLeavingIt() {
      _activity.UserTypesUnseen(TimeSpan.FromSeconds(5));
      _watchdog.Check(foregroundElevated: true);

      _activity.UserTypesUnseen(TimeSpan.FromMilliseconds(250));

      Assert.Equal(WatchdogOutcome.None, _watchdog.Check(foregroundElevated: false));
    }

    [Fact]
    public void TurnedOffWatchdogChecksNothing() {
      _watchdog.Enabled = false;
      _activity.UserTypesUnseen(TimeSpan.FromSeconds(5));

      Assert.Equal(WatchdogOutcome.None, _watchdog.Check(foregroundElevated: false));
    }

    [Fact]
    public void ReinstalledHookGetsAFreshStart() {
      _activity.UserTypesUnseen(TimeSpan.FromMilliseconds(1600));
      _watchdog.Check(foregroundElevated: false);

      _activity.UserTypesUnseen(TimeSpan.FromMilliseconds(500));

      Assert.Equal(WatchdogOutcome.None, _watchdog.Check(foregroundElevated: false));
    }

    [Fact]
    public void SecondFailureWithinTenMinutesPausesItself() {
      Fail();
      _time.Advance(TimeSpan.FromMinutes(9));

      WatchdogOutcome second = Fail();

      Assert.True(second.Reinstall);
      Assert.Equal([IncidentType.HookReinstalled, IncidentType.SelfPaused], Types(second));
      Assert.Equal(_time.GetUtcNow() + TimeSpan.FromMinutes(10), _watchdog.SelfPausedUntil);
    }

    [Fact]
    public void FailuresElevenMinutesApartDoNotPause() {
      Fail();
      _time.Advance(TimeSpan.FromMinutes(11));
      Fail();

      Assert.Null(_watchdog.SelfPausedUntil);
    }

    [Fact]
    public void SelfPauseEndsAfterTenMinutesWithAFreshReinstall() {
      Fail();
      Fail();
      _time.Advance(TimeSpan.FromMinutes(10));

      WatchdogOutcome outcome = _watchdog.Check(foregroundElevated: false);

      Assert.True(outcome.Reinstall);
      Assert.Null(_watchdog.SelfPausedUntil);
    }

    [Fact]
    public void SelfPauseHoldsBeforeTenMinutes() {
      Fail();
      Fail();
      _time.Advance(TimeSpan.FromMinutes(9));

      Assert.Equal(WatchdogOutcome.None, _watchdog.Check(foregroundElevated: false));
      Assert.NotNull(_watchdog.SelfPausedUntil);
    }

    [Fact]
    public void ManualResumeReattachesAndClearsThePause() {
      Fail();
      Fail();

      WatchdogOutcome outcome = _watchdog.Resume();

      Assert.True(outcome.Reinstall);
      Assert.Null(_watchdog.SelfPausedUntil);
    }

    [Fact]
    public void FailureAfterManualResumeDoesNotPauseAgainAtOnce() {
      Fail();
      Fail();
      _watchdog.Resume();

      Fail();

      Assert.Null(_watchdog.SelfPausedUntil);
    }

    private static IncidentType[] Types(WatchdogOutcome outcome) {
      return [.. outcome.Incidents.Select(incident => incident.Type)];
    }

    private WatchdogOutcome Fail() {
      _activity.UserTypesUnseen(TimeSpan.FromSeconds(2));
      return _watchdog.Check(foregroundElevated: false);
    }

    // Moves the system's last input forward without the hook seeing it.
    private sealed class FakeActivity : IInputActivity {
      public long SystemLastInputTick { get; private set; } = 5_000;

      public long HookLastInputTick { get; } = 5_000;

      public void UserTypesUnseen(TimeSpan after) {
        SystemLastInputTick += (long)after.TotalMilliseconds;
      }
    }
  }
}
