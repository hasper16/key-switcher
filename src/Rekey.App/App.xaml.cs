using System.IO;
using System.Windows;
using H.NotifyIcon;
using Rekey.Core;
using Rekey.Win32;

namespace Rekey.App {
  internal sealed partial class App {
    private readonly SettingsStore _settingsStore = new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Rekey"));

    private SingleInstanceGuard? _guard;
    private Settings? _settings;
    private TaskbarIcon? _trayIcon;

    protected override void OnStartup(StartupEventArgs e) {
      base.OnStartup(e);

      // The guard comes first: a second instance must leave without touching settings or the
      // tray.
      _guard = SingleInstanceGuard.TryAcquire(SingleInstanceGuard.NameForCurrentUser());
      if (_guard is null) {
        Shutdown();
        return;
      }

      _settings = _settingsStore.Load();

      _trayIcon = (TaskbarIcon)FindResource("TrayIcon");

      // Efficiency Mode throttles the process; the keyboard hook added later must stay fast.
      _trayIcon.ForceCreate(enablesEfficiencyMode: false);
    }

    protected override void OnSessionEnding(SessionEndingCancelEventArgs e) {
      base.OnSessionEnding(e);

      // Windows may end the process soon after sign-out starts, so write settings now.
      FlushSettings();
    }

    protected override void OnExit(ExitEventArgs e) {
      FlushSettings();
      _trayIcon?.Dispose();
      _guard?.Dispose();
      base.OnExit(e);
    }

    private void FlushSettings() {
      if (_settings is not null) {
        _settingsStore.Save(_settings);
      }
    }

    private void OnQuitClick(object sender, RoutedEventArgs e) {
      Shutdown();
    }
  }
}
