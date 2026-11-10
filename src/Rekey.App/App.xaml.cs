using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using H.NotifyIcon;
using Rekey.Core;
using Rekey.Win32;

namespace Rekey.App {
  [SuppressMessage(
      "Design",
      "CA1001:Types that own disposable fields should be disposable",
      Justification = "WPF ends the Application through OnExit, which disposes the fields.")]
  internal sealed partial class App {
    private const string _restartedAfterFreeze = "--restarted-after-freeze";
    private const string _toolTip = "Rekey";
    private const string _selfPausedToolTip =
        "Rekey paused itself: it lost the keyboard twice. It retries in 10 minutes.";

    // How long a restarted Rekey waits for the frozen one to exit before it starts anyway.
    private static readonly TimeSpan _oldProcessExitWait = TimeSpan.FromSeconds(10);

    private static readonly string _dataFolder = Path.Join(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Rekey");

    private readonly SettingsStore _settingsStore = new(_dataFolder);
    private readonly RestartStateStore _restartStore = new(_dataFolder);

    private SingleInstanceGuard? _guard;
    private Settings? _settings;
    private TaskbarIcon? _trayIcon;
    private MenuItem? _resumeItem;
    private InputWorker? _worker;
    private FreezeGuard? _freezeGuard;

    protected override void OnStartup(StartupEventArgs e) {
      base.OnStartup(e);
      WaitForFrozenInstance(e.Args);

      // The guard comes first: a second instance must leave without touching settings or the
      // tray.
      _guard = SingleInstanceGuard.TryAcquire(SingleInstanceGuard.NameForCurrentUser());
      if (_guard is null) {
        Shutdown();
        return;
      }

      _settings = _settingsStore.Load();

      _trayIcon = (TaskbarIcon)FindResource("TrayIcon");
      AddMenuItems(_trayIcon);

      // Efficiency Mode throttles the process; the keyboard hook must stay fast.
      _trayIcon.ForceCreate(enablesEfficiencyMode: false);
      StartInput(_settings);
    }

    protected override void OnSessionEnding(SessionEndingCancelEventArgs e) {
      base.OnSessionEnding(e);

      // Windows may end the process soon after sign-out starts, so write settings now.
      FlushSettings();
    }

    protected override void OnExit(ExitEventArgs e) {
      try {
        FlushSettings();
      } finally {
        _freezeGuard?.Dispose();
        _worker?.Dispose();
        _trayIcon?.Dispose();
        _guard?.Dispose();
        base.OnExit(e);
      }
    }

    // Incidents carry enum values only, so the trace never holds typed text.
    private static void RecordIncident(Incident incident) {
      string details = string.Join(',', incident.Details);
      Trace.WriteLine(string.Create(
          CultureInfo.InvariantCulture,
          $"Rekey Incident {incident.Time:O} {incident.Type} {details}"));
    }

    // A Rekey restarted after a freeze waits for the old process to leave, so the old one
    // releases the single-instance claim and its hooks first.
    private static void WaitForFrozenInstance(string[] args) {
      int flag = Array.IndexOf(args, _restartedAfterFreeze);
      if (flag < 0 || flag + 1 >= args.Length
          || !int.TryParse(args[flag + 1], CultureInfo.InvariantCulture, out int processId)) {
        return;
      }

      try {
        using var old = Process.GetProcessById(processId);
        old.WaitForExit(_oldProcessExitWait);
      } catch (ArgumentException) {
        // The old process already exited.
      }
    }

    private void StartInput(Settings settings) {
      _worker = new InputWorker(settings, RecordIncident, TimeProvider.System);
      _worker.SelfPauseChanged += until => Dispatcher.BeginInvoke(() => ShowSelfPause(until));
      try {
        _worker.Start();
      } catch (Win32Exception) {
        // Without hooks Rekey cannot see typing; it stays in the tray so the user can quit.
        Trace.WriteLine("Rekey could not install its keyboard hooks.");
        return;
      }

      _freezeGuard = new FreezeGuard(
          _worker,
          settings,
          _restartStore.LoadLastRestart(),
          RecordIncident,
          RestartAfterFreeze,
          TimeProvider.System);
      _freezeGuard.Start();
    }

    // Runs on the freeze guard's thread while the worker is stuck; the UI thread may still run.
    private void RestartAfterFreeze() {
      _restartStore.SaveLastRestart(DateTimeOffset.UtcNow);
      Process.Start(
          Environment.ProcessPath!,
          [_restartedAfterFreeze, Environment.ProcessId.ToString(CultureInfo.InvariantCulture)]);
      Dispatcher.Invoke(() => _trayIcon?.Dispose(), TimeSpan.FromSeconds(1));
      FlushSettings();
      Environment.Exit(0);
    }

    private void AddMenuItems(TaskbarIcon trayIcon) {
      _resumeItem = new MenuItem { Header = "Resume", Visibility = Visibility.Collapsed };
      _resumeItem.Click += (_, _) => _worker?.ResumeSelfPause();
      trayIcon.ContextMenu.Items.Insert(0, _resumeItem);
#if DEBUG
      var freeze = new MenuItem { Header = "Debug: freeze input for 7 s" };
      freeze.Click += (_, _) => _worker?.FreezeForCheck(TimeSpan.FromSeconds(7));
      trayIcon.ContextMenu.Items.Insert(1, freeze);
#endif
    }

    private void ShowSelfPause(DateTimeOffset? until) {
      if (_trayIcon is null || _resumeItem is null) {
        return;
      }

      _trayIcon.ToolTipText = until is null ? _toolTip : _selfPausedToolTip;
      _resumeItem.Visibility = until is null ? Visibility.Collapsed : Visibility.Visible;
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
