using Rekey.Core;

namespace Rekey.App.ViewModels {
  // What the tray icon and its menu show, and what their items do; the view reads it each time
  // the menu opens and when the state changes.
  internal sealed class TrayViewModel {
    private readonly SettingsHub _hub;
    private readonly TimeProvider _clock;
    private readonly Action _resumeSelfPause;

    public TrayViewModel(SettingsHub hub, TimeProvider clock, Action resumeSelfPause) {
      _hub = hub;
      _clock = clock;
      _resumeSelfPause = resumeSelfPause;
    }

    // Set by the view from the worker's report; null when Rekey has not paused itself.
    public DateTimeOffset? SelfPausedUntil { get; set; }

    public CurrentApp? CurrentApp { get; set; }

    public string? LanguageCode { get; set; }

    public TrayState State {
      get {
        Settings settings = _hub.Current;
        if (SelfPausedUntil is not null) {
          return TrayState.SelfPaused;
        }

        if (!settings.AutoCorrectionEnabled) {
          return TrayState.Off;
        }

        return _clock.GetUtcNow() < settings.PausedUntil ? TrayState.Paused : TrayState.Active;
      }
    }

    public bool IsPaused => State is TrayState.Paused or TrayState.SelfPaused;

    public bool AutoCorrectionEnabled => _hub.Current.AutoCorrectionEnabled;

    public string ToolTip => State switch {
      TrayState.SelfPaused => Strings.TraySelfPaused,
      TrayState.Off => Strings.TrayOff,
      TrayState.Paused => PausedToolTip(_hub.Current.PausedUntil!.Value),
      _ => Strings.TrayOn,
    };

    public string? ExcludeItemText {
      get {
        if (CurrentApp is not { } app) {
          return null;
        }

        string format = _hub.Current.ExcludedApps.Contains(app.Id)
            ? Strings.TurnOnIn
            : Strings.TurnOffIn;
        return UiText.Format(format, app.Name);
      }
    }

    public void ToggleAutoCorrection() {
      _hub.Update(settings => settings with {
        AutoCorrectionEnabled = !settings.AutoCorrectionEnabled,
      });
    }

    public void Pause(PausePreset preset) {
      DateTimeOffset until = preset switch {
        PausePreset.FifteenMinutes => _clock.GetUtcNow().AddMinutes(15),
        PausePreset.ThirtyMinutes => _clock.GetUtcNow().AddMinutes(30),
        PausePreset.OneHour => _clock.GetUtcNow().AddHours(1),
        PausePreset.ThreeHours => _clock.GetUtcNow().AddHours(3),
        _ => Settings.UntilRestart,
      };
      _hub.Update(settings => settings with { PausedUntil = until });
    }

    // Ends whichever pause is on: the user's or the one Rekey took itself.
    public void Resume() {
      if (SelfPausedUntil is not null) {
        _resumeSelfPause();
      }

      _hub.Update(settings => settings with { PausedUntil = null });
    }

    public void ToggleCurrentApp() {
      if (CurrentApp is not { } app) {
        return;
      }

      _hub.Update(settings => {
        IEnumerable<string> entries = settings.ExcludedApps.Contains(app.Id)
            ? settings.ExcludedApps.Entries.Where(entry => !Matches(entry, app.Id))
            : [.. settings.ExcludedApps.Entries, app.Id.Key];
        return settings with { ExcludedApps = new ExcludedAppList(entries) };
      });
    }

    private static bool Matches(string entry, AppId app) {
      return new ExcludedAppList([entry]).Contains(app);
    }

    private string PausedToolTip(DateTimeOffset until) {
      if (until == Settings.UntilRestart) {
        return Strings.TrayPausedUntilRestart;
      }

      int minutes = (int)Math.Ceiling((until - _clock.GetUtcNow()).TotalMinutes);
      return minutes <= 1
          ? Strings.TrayPausedOneMinute
          : UiText.Format(Strings.TrayPausedMinutes, minutes);
    }
  }
}
