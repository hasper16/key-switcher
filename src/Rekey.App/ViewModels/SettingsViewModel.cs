using CommunityToolkit.Mvvm.ComponentModel;
using Rekey.Core;
using Rekey.Core.Detection;

namespace Rekey.App.ViewModels {
  // The settings window's view of the settings: each property reads the hub's snapshot and a
  // change replaces it, so the window holds no copy that could go stale.
  internal sealed class SettingsViewModel : ObservableObject {
    private readonly SettingsHub _hub;

    public SettingsViewModel(SettingsHub hub) {
      _hub = hub;
      _hub.Changed += _ => OnPropertyChanged(string.Empty);
    }

    public bool AutoCorrectionEnabled {
      get => _hub.Current.AutoCorrectionEnabled;
      set => _hub.Update(settings => settings with { AutoCorrectionEnabled = value });
    }

    public Sensitivity Sensitivity {
      get => _hub.Current.Sensitivity;
      set => _hub.Update(settings => settings with { Sensitivity = value });
    }

    public bool TriggerOnSpace {
      get => Triggers(TriggerKeys.Space);
      set => SetTrigger(TriggerKeys.Space, value);
    }

    public bool TriggerOnEnter {
      get => Triggers(TriggerKeys.Enter);
      set => SetTrigger(TriggerKeys.Enter, value);
    }

    public bool TriggerOnTab {
      get => Triggers(TriggerKeys.Tab);
      set => SetTrigger(TriggerKeys.Tab, value);
    }

    public bool SkipAfterManualLayoutSwitch {
      get => _hub.Current.SkipAfterManualLayoutSwitch;
      set => _hub.Update(settings => settings with { SkipAfterManualLayoutSwitch = value });
    }

    public bool VerifiedReplacement {
      get => _hub.Current.VerifiedReplacement;
      set => _hub.Update(settings => settings with { VerifiedReplacement = value });
    }

    public bool ShowCursorIndicator {
      get => _hub.Current.ShowCursorIndicator;
      set => _hub.Update(settings => settings with { ShowCursorIndicator = value });
    }

    public bool PlaySwitchSound {
      get => _hub.Current.PlaySwitchSound;
      set => _hub.Update(settings => settings with { PlaySwitchSound = value });
    }

    public SwitchSound SwitchSound {
      get => _hub.Current.SwitchSound;
      set => _hub.Update(settings => settings with { SwitchSound = value });
    }

    public bool LaunchAtSignIn {
      get => _hub.Current.LaunchAtSignIn;
      set => _hub.Update(settings => settings with { LaunchAtSignIn = value });
    }

    public bool KeyboardWatchdogEnabled {
      get => _hub.Current.KeyboardWatchdogEnabled;
      set => _hub.Update(settings => settings with { KeyboardWatchdogEnabled = value });
    }

    public bool RestartAfterFreezeEnabled {
      get => _hub.Current.RestartAfterFreezeEnabled;
      set => _hub.Update(settings => settings with { RestartAfterFreezeEnabled = value });
    }

    private bool Triggers(TriggerKeys key) {
      return (_hub.Current.TriggerKeys & key) != 0;
    }

    private void SetTrigger(TriggerKeys key, bool on) {
      _hub.Update(settings => settings with {
        TriggerKeys = on ? settings.TriggerKeys | key : settings.TriggerKeys & ~key,
      });
    }
  }
}
