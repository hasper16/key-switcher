using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using H.NotifyIcon;
using Rekey.App.Tray;
using Rekey.App.ViewModels;
using Rekey.Core;
using Rekey.Core.Words;
using Rekey.Win32;
using Rekey.Win32.Focus;

namespace Rekey.App {
  [SuppressMessage(
      "Design",
      "CA1001:Types that own disposable fields should be disposable",
      Justification = "WPF ends the Application through OnExit, which disposes the fields.")]
  internal sealed partial class App {
    private const string _restartedAfterFreeze = "--restarted-after-freeze";
#if DEBUG
    private const string _checkFreeze = "--check-freeze";
    private const string _checkSelfPause = "--check-self-pause";
    private const string _checkAutoResume = "--check-auto-resume";
    private const string _checkQuit = "--check-quit";
#endif

    // The tray says how many minutes of a Pause remain and notices its end.
    private static readonly TimeSpan _trayRefreshPeriod = TimeSpan.FromSeconds(20);

    // How long a restarted Rekey waits for the frozen one to exit before it starts anyway.
    private static readonly TimeSpan _oldProcessExitWait = TimeSpan.FromSeconds(10);

    private static readonly string _dataFolder = Path.Join(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Rekey");

    private static readonly WordListsStore _wordsStore = new(_dataFolder);

    private readonly SettingsStore _settingsStore = new(_dataFolder);
    private readonly RestartStateStore _restartStore = new(_dataFolder);
    private readonly WordListsSaver _wordsSaver =
        new(_wordsStore, TimeProvider.System, ReportWordsNotSaved);

    private SingleInstanceGuard? _guard;
    private SettingsHub? _hub;
    private TrayViewModel? _tray;
    private TrayIconView? _trayView;
    private ForegroundApps? _foregroundApps;
    private DispatcherTimer? _trayTimer;
#if DEBUG
    private bool _checkingTray;
    private bool _autoResume;
    private bool _autoQuit;
#endif
    private InputWorker? _worker;
    private FreezeGuard? _freezeGuard;

    // The worker's Word counts as of the latest Trigger Key; only the worker thread writes it.
    private volatile StrongBox<WordCounts>? _wordCounts;

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

      _hub = new SettingsHub(
          _settingsStore.Load().ForNewSession(), _settingsStore.Save, TimeProvider.System);
      _tray = new TrayViewModel(_hub, TimeProvider.System, () => _worker?.ResumeSelfPause());
      var trayIcon = (TaskbarIcon)FindResource("TrayIcon");
      _trayView = new TrayIconView(
          trayIcon, _tray, new TrayCommands(OpenSettings, OpenAbout, () => Shutdown()));
#if DEBUG
      AddDebugMenuItems(_trayView);
#endif
      _trayView.Refresh();

      // Efficiency Mode throttles the process; the keyboard hook must stay fast.
      trayIcon.ForceCreate(enablesEfficiencyMode: false);
      StartInput(_hub.Current);
      _hub.Changed += OnSettingsChanged;
      StartTrayFollowers();
#if DEBUG
      if (e.Args.Contains(_checkFreeze)) {
        FreezeTwiceForCheck();
      }

      if (e.Args.Contains(_checkSelfPause)) {
        _checkingTray = true;
        _autoResume = e.Args.Contains(_checkAutoResume);
        _autoQuit = e.Args.Contains(_checkQuit);
        DropHookTwiceForCheck();
      }
#endif
    }

    protected override void OnSessionEnding(SessionEndingCancelEventArgs e) {
      base.OnSessionEnding(e);

      // Windows may end the process soon after sign-out starts, so write settings now.
      FlushSettings();
      _wordsSaver.Flush();
    }

    protected override void OnExit(ExitEventArgs e) {
      try {
        FlushSettings();
        TraceWordCounts();
      } finally {
        _freezeGuard?.Dispose();
        _worker?.Dispose();

        // After the worker, so its last change is saved too.
        _wordsSaver.Flush();
        _wordsSaver.Dispose();
        _trayTimer?.Stop();
        _foregroundApps?.Dispose();
        _trayView?.Dispose();
        _hub?.Dispose();
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

    // The type only: the message names a path inside the user profile.
    private static void ReportWordsNotSaved(Exception error) {
      Trace.WriteLine($"Rekey could not save its word lists: {error.GetType().Name}");
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
      WordListsDocument? words = _wordsStore.Load();
      _worker = new InputWorker(settings, RecordIncident, TimeProvider.System) {
        InitialWords = words ?? WordListsDocument.Empty,
      };

      // A file Rekey could not read stays as it is: this session learns but does not save.
      if (words is null) {
        Trace.WriteLine("Rekey could not read its word lists and will not save them.");
      } else {
        _worker.WordListsChanged += _wordsSaver.Changed;
      }

      _worker.SelfPauseChanged += until => Dispatcher.BeginInvoke(() => ShowSelfPause(until));
      _worker.Observed += RememberWordCounts;
      _worker.Observed += FollowLayout;
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

    // Runs on the worker thread after each input event.
    private void RememberWordCounts(InputEvent inputEvent) {
      if (inputEvent is KeyDown { Kind: KeyKind.Space or KeyKind.Enter or KeyKind.Tab }
          && _worker is { } worker) {
        _wordCounts = new StrongBox<WordCounts>(worker.WordCounts);
      }
    }

    // How many Words got no evaluation because a key could not be confirmed outside a Password
    // Field (design D8 of add-auto-correction): counters only, never text.
    private void TraceWordCounts() {
      if (_wordCounts?.Value is not { } counts) {
        return;
      }

      string dropped = counts.Unconfirmed.ToString(CultureInfo.InvariantCulture);
      string line = string.Create(
          CultureInfo.InvariantCulture,
          $"words-ended={counts.Ended} evaluated={counts.Evaluated} unconfirmed={dropped}");
      Trace.WriteLine($"Rekey {line}");
#if DEBUG
      File.WriteAllText(Path.Join(_dataFolder, "word-counts.txt"), line);
#endif
    }

    // Runs on the freeze guard's thread while the worker is stuck; the UI thread may still run.
    private void RestartAfterFreeze() {
      // Without the saved time the next process could not keep the once-per-10-minutes limit,
      // so Rekey keeps running instead; the freeze Incident is already recorded.
      if (!_restartStore.TrySaveLastRestart(DateTimeOffset.UtcNow)) {
        Trace.WriteLine("Rekey could not save the restart time and will not restart.");
        return;
      }

      try {
        Process.Start(
            Environment.ProcessPath!,
            [_restartedAfterFreeze, Environment.ProcessId.ToString(CultureInfo.InvariantCulture)]);
      } catch (Win32Exception) {
        Trace.WriteLine("Rekey could not start its replacement and keeps running.");
        return;
      }

      Dispatcher.Invoke(() => _trayView?.Dispose(), TimeSpan.FromSeconds(1));
      FlushSettings();
      _wordsSaver.Flush();
      Environment.Exit(0);
    }

#if DEBUG
    // For tools/Rekey.DesktopChecks: two 7 s freezes 15 s apart, which the freeze guard answers
    // with a restart while the checks keep typing.
    private void FreezeTwiceForCheck() {
      _ = Task.Run(async () => {
        await Task.Delay(TimeSpan.FromSeconds(3));
        _worker?.FreezeForCheck(TimeSpan.FromSeconds(7));
        await Task.Delay(TimeSpan.FromSeconds(15));
        _worker?.FreezeForCheck(TimeSpan.FromSeconds(7));
      });
    }

    // For the manual tray check: the next key after 3 s and after 10 s stalls the hook past
    // Windows' limit, so Windows drops it twice and the app pauses itself while the user types.
    // The first key typed after start stalls the hook past Windows' limit, and the first key
    // typed 8 s later does again: two drops, so the app pauses itself while the user keeps
    // typing.
    private void DropHookTwiceForCheck() {
      _worker?.StallHookForCheck(TimeSpan.FromSeconds(3));
      _ = Task.Run(async () => {
        await Task.Delay(TimeSpan.FromSeconds(8));
        _worker?.StallHookForCheck(TimeSpan.FromSeconds(3));
      });
      WriteTrayState();
    }

    // For tools/Rekey.DesktopChecks: what the tray shows, written after every change.
    private void WriteTrayState() {
      if (!_checkingTray || _tray is null || _trayView is null) {
        return;
      }

      bool paused = _tray.State == TrayState.SelfPaused;
      bool resume = _trayView.FindItem(Strings.MenuResume) is not null;
      Directory.CreateDirectory(_dataFolder);
      File.WriteAllText(
          Path.Join(_dataFolder, "check-tray.txt"),
          $"tooltip-self-paused={paused} resume-item={resume}");
    }

    private void AddDebugMenuItems(TrayIconView trayView) {
      var freeze = new MenuItem { Header = "Debug: freeze input for 7 s" };
      freeze.Click += (_, _) => _worker?.FreezeForCheck(TimeSpan.FromSeconds(7));
      trayView.ExtraItems.Add(freeze);
    }

#endif
    private void StartTrayFollowers() {
      _foregroundApps = new ForegroundApps();
      _foregroundApps.Changed += app => {
        _tray!.CurrentApp = new CurrentApp(app, AppNames.Of(app));
        RefreshTray();
      };
      _foregroundApps.Start();
      _trayTimer = new DispatcherTimer(
          _trayRefreshPeriod, DispatcherPriority.Background, (_, _) => RefreshTray(), Dispatcher);
      _trayTimer.Start();
    }

    private void RefreshTray() {
      _trayView?.Refresh();
#if DEBUG
      WriteTrayState();
#endif
    }

    // Runs on the worker thread after each input event; only Layout changes reach the UI.
    private void FollowLayout(InputEvent inputEvent) {
      if (inputEvent is LayoutChanged changed) {
        string code = changed.Layout.LanguageCode;
        Dispatcher.BeginInvoke(() => {
          _tray!.LanguageCode = code;
          RefreshTray();
        });
      }
    }

    private void OnSettingsChanged(Settings settings) {
      _worker?.UseSettings(settings);
      RefreshTray();
    }

    private void OpenSettings() {
      // The settings window arrives with task group 3 of add-tray-and-settings-ui.
    }

    private void OpenAbout() {
      // The About page arrives with task group 3 of add-tray-and-settings-ui.
    }

    private void ShowSelfPause(DateTimeOffset? until) {
      if (_tray is null) {
        return;
      }

      _tray.SelfPausedUntil = until;
      RefreshTray();
#if DEBUG
      if (until is null && _autoQuit) {
        // For the desktop checks: quit the way the tray's Quit does, 1 s after resuming.
        _ = Task.Delay(TimeSpan.FromSeconds(1)).ContinueWith(
            _ => Dispatcher.BeginInvoke(() => Shutdown()), TaskScheduler.Default);
      }

      if (until is not null && _autoResume) {
        // For the desktop checks: choose Resume as a person would, 2 s later.
        _ = Task.Delay(TimeSpan.FromSeconds(2)).ContinueWith(
            _ => Dispatcher.BeginInvoke(
                () => _trayView?.FindItem(Strings.MenuResume)
                    ?.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent))),
            TaskScheduler.Default);
      }
#endif
    }

    private void FlushSettings() {
      _hub?.Flush();
    }
  }
}
