using Rekey.App.ViewModels;
using Rekey.Core;
using Rekey.Core.Tests;
using Xunit;

namespace Rekey.App.Tests.ViewModels {
  public sealed class TrayViewModelTests : IDisposable {
    private static readonly CurrentApp _telegram =
        new(new AppId(@"C:\Apps\Telegram\Telegram.exe"), "Telegram");

    private readonly ManualClock _clock = new();
    private readonly SettingsHub _hub;
    private readonly TrayViewModel _tray;
    private int _selfPauseResumes;

    public TrayViewModelTests() {
      _hub = new SettingsHub(new Settings(), _ => { }, _clock);
      _tray = new TrayViewModel(_hub, _clock, () => _selfPauseResumes++);
    }

    public void Dispose() {
      _hub.Dispose();
    }

    [Fact]
    public void ActiveByDefault() {
      Assert.Equal(TrayState.Active, _tray.State);
      Assert.Equal("Rekey is on", _tray.ToolTip);
    }

    [Fact]
    public void PausedSaysTheMinutesLeft() {
      _tray.Pause(PausePreset.ThirtyMinutes);
      _clock.Advance(TimeSpan.FromMinutes(10.5));

      Assert.Equal(TrayState.Paused, _tray.State);
      Assert.Equal("Rekey is paused: 20 minutes left", _tray.ToolTip);
      Assert.True(_tray.IsPaused);
    }

    [Fact]
    public void LastPausedMinuteIsSingular() {
      _tray.Pause(PausePreset.FifteenMinutes);
      _clock.Advance(TimeSpan.FromMinutes(14.5));

      Assert.Equal("Rekey is paused: 1 minute left", _tray.ToolTip);
    }

    [Fact]
    public void PauseUntilRestartSaysSo() {
      _tray.Pause(PausePreset.UntilRestart);

      Assert.Equal(TrayState.Paused, _tray.State);
      Assert.Equal("Rekey is paused until it restarts", _tray.ToolTip);
    }

    [Fact]
    public void PauseEndsOnItsOwn() {
      _tray.Pause(PausePreset.FifteenMinutes);
      _clock.Advance(TimeSpan.FromMinutes(15));

      Assert.Equal(TrayState.Active, _tray.State);
      Assert.False(_tray.IsPaused);
    }

    [Fact]
    public void ResumeEndsThePause() {
      _tray.Pause(PausePreset.OneHour);

      _tray.Resume();

      Assert.Null(_hub.Current.PausedUntil);
      Assert.Equal(TrayState.Active, _tray.State);
    }

    [Fact]
    public void OffWhenAutoCorrectionIsTurnedOff() {
      _tray.ToggleAutoCorrection();

      Assert.False(_hub.Current.AutoCorrectionEnabled);
      Assert.Equal(TrayState.Off, _tray.State);
      Assert.Equal("Auto Correction is off", _tray.ToolTip);
    }

    [Fact]
    public void SelfPauseWinsOverEverythingElse() {
      _tray.ToggleAutoCorrection();
      _tray.SelfPausedUntil = _clock.GetUtcNow().AddMinutes(10);

      Assert.Equal(TrayState.SelfPaused, _tray.State);
      Assert.Equal(
          "Rekey paused itself: it lost the keyboard twice. It retries in 10 minutes.",
          _tray.ToolTip);
      Assert.True(_tray.IsPaused);
    }

    [Fact]
    public void ResumeDuringSelfPauseAsksTheWorker() {
      _tray.SelfPausedUntil = _clock.GetUtcNow().AddMinutes(10);

      _tray.Resume();

      Assert.Equal(1, _selfPauseResumes);
    }

    [Fact]
    public void NoExcludeItemWithoutACurrentApp() {
      Assert.Null(_tray.ExcludeItemText);
    }

    [Fact]
    public void ExcludeItemTurnsTheCurrentAppOff() {
      _tray.CurrentApp = _telegram;
      Assert.Equal("Turn off in Telegram", _tray.ExcludeItemText);

      _tray.ToggleCurrentApp();

      Assert.True(_hub.Current.ExcludedApps.Contains(_telegram.Id));
      Assert.Equal("Turn on in Telegram", _tray.ExcludeItemText);
    }

    [Fact]
    public void ExcludeItemTurnsAnExcludedAppBackOn() {
      _tray.CurrentApp = _telegram;
      _tray.ToggleCurrentApp();

      _tray.ToggleCurrentApp();

      Assert.Empty(_hub.Current.ExcludedApps.Entries);
      Assert.Equal("Turn off in Telegram", _tray.ExcludeItemText);
    }

    [Fact]
    public void LanguageCodeIsShown() {
      _tray.LanguageCode = "RU";

      Assert.Equal("RU", _tray.LanguageCode);
    }
  }
}
