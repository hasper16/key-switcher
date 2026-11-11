using System.Diagnostics;
using System.Globalization;
using System.IO;
using Microsoft.Win32;
using Rekey.Core;
using Rekey.Win32;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace Rekey.DesktopChecks {
  // Checks 5.3 and 5.5: a hook Windows removed is reattached, and a repeated freeze restarts.
  internal sealed partial class Checks {
    private const string _appName = "Rekey.App";
#if DEBUG
    private static readonly TimeSpan _freeze = TimeSpan.FromSeconds(7);
    private static readonly TimeSpan _watchdogWait = TimeSpan.FromSeconds(4);
#endif

    private static string AppPath() {
      return Path.Join(
          RepositoryRoot(), "src", _appName, "bin", "Debug", "net10.0-windows", $"{_appName}.exe");
    }

#if DEBUG
    // Windows removes a low-level hook whose callback runs longer than this.
    private static int HookTimeoutMilliseconds() {
      object? value = Registry.GetValue(
          @"HKEY_CURRENT_USER\Control Panel\Desktop", "LowLevelHooksTimeout", null);
      int set = value switch {
        int number => number,
        string text when int.TryParse(text, CultureInfo.InvariantCulture, out int number) => number,
        _ => 1000,
      };

      // Windows caps the effective limit at 1,000 ms whatever the registry says.
      return Math.Clamp(set, 0, 1000);
    }
#endif

#if DEBUG
    // Keeps typing, as a user would after the hook drops, until the condition holds. The
    // watchdog acts only on input newer than the last the hook saw, and the stalled callback
    // stamps its time when it finally returns.
    private static bool TypeUntil(Func<bool> condition, int seconds = 6) {
      var clock = Stopwatch.StartNew();
      while (clock.Elapsed < TimeSpan.FromSeconds(seconds)) {
        Input.Type("w");
        if (EventLog.WaitFor(condition, TimeSpan.FromMilliseconds(300))) {
          return true;
        }
      }

      return condition();
    }

    private static string? ReadState(string path) {
      try {
        return File.Exists(path) ? File.ReadAllText(path).Trim() : null;
      } catch (IOException) {
        return null;
      }
    }

    private void CheckHookWatchdog() {
      TimeSpan stall = TimeSpan.FromMilliseconds(HookTimeoutMilliseconds() + 1500);
      Focus(First().Ordinary);

      // First drop: Shift goes up while the hook is gone.
      EventLog.Position first = _log.Mark();
      Input.Key(VIRTUAL_KEY.VK_LSHIFT, 0x2A, up: false);
      _worker.StallHookForCheck(stall);
      Input.Type("q");
      Input.Key(VIRTUAL_KEY.VK_LSHIFT, 0x2A, up: true);
      bool reattached = TypeUntil(() => _log.IncidentsSince(first)
          .Contains(IncidentType.HookReinstalled));
      Expect("5.3 a dropped hook is reinstalled", reattached, IncidentsText(first));
      Thread.Sleep(_settle);
      EventLog.Position afterFirst = _log.Mark();
      Input.Type("t");
      KeyDown? shifted = WaitForKeys(afterFirst, 1).FirstOrDefault();
      Expect(
          "5.3 Shift released while the hook was gone is resynced",
          shifted?.Modifiers == KeyModifiers.None,
          $"modifiers {shifted?.Modifiers}");

      // Second drop within 10 minutes: Caps Lock toggles while the hook is gone.
      EventLog.Position second = _log.Mark();
      _worker.StallHookForCheck(stall);
      Input.Type("q");
      Input.ToggleCapsLock();
      bool paused = TypeUntil(() => _log.IncidentsSince(second)
          .Contains(IncidentType.SelfPaused))
          && EventLog.WaitFor(
              () => _log.SelfPausesSince(second).Exists(until => until is not null),
              _watchdogWait);
      Expect("5.3 a second drop pauses Rekey itself", paused, IncidentsText(second));
      Thread.Sleep(_settle);
      EventLog.Position afterSecond = _log.Mark();
      Input.Type("y");
      KeyDown? capsKey = WaitForKeys(afterSecond, 1).FirstOrDefault();
      bool capsLock = SystemState.CapsLockOn();
      Expect(
          "5.3 Caps Lock toggled while the hook was gone is resynced",
          capsKey?.Keystroke.CapsLock == capsLock,
          $"Keystroke Caps Lock {capsKey?.Keystroke.CapsLock}, system {capsLock}");
      Input.ToggleCapsLock();

      EventLog.Position resume = _log.Mark();
      _worker.ResumeSelfPause();
      bool resumed = EventLog.WaitFor(
          () => _log.SelfPausesSince(resume).Contains(null), _watchdogWait);
      Expect("5.3 Resume ends the self-pause", resumed, $"resumed {resumed}");
    }

    private string IncidentsText(EventLog.Position mark) {
      List<IncidentType> seen = _log.IncidentsSince(mark);
      return seen.Count == 0 ? "no Incident" : "Incidents " + string.Join(", ", seen);
    }

    private void CheckFreezeWatchdog() {
      using var restartAsked = new ManualResetEventSlim();
      using var guard = new FreezeGuard(
          _worker, new Settings(), null, _log.AddIncident, restartAsked.Set, TimeProvider.System);
      guard.Start();
      Focus(First().Ordinary);

      EventLog.Position first = _log.Mark();
      FreezeWhileTyping();
      bool frozen = WaitForIncident(first, IncidentType.InputFrozen, _watchdogWait);
      Expect("5.5 a freeze is recorded", frozen, $"InputFrozen {frozen}");
      Thread.Sleep(TimeSpan.FromSeconds(2));

      EventLog.Position second = _log.Mark();
      FreezeWhileTyping();
      bool restart = WaitForIncident(second, IncidentType.FreezeRestart, _watchdogWait)
          && restartAsked.Wait(_watchdogWait);
      Expect("5.5 a second freeze within an hour asks to restart", restart, $"{restart}");
    }

    private void CheckAppTray() {
      if (Process.GetProcessesByName(_appName).Length > 0) {
        Expect("5.3 the app's tray shows the self-pause", false, "close the running Rekey first");
        return;
      }

      string state = Path.Join(SystemState.DataFolder, "check-tray.txt");
      _restorer.Defer("Rekey settings file", SystemState.KeepFile("settings.json"));
      _restorer.Defer("Rekey restart state", SystemState.KeepFile("restart-state.txt"));
      _restorer.Defer("Rekey tray check file", SystemState.KeepFile("check-tray.txt"));
      _restorer.Defer("test Rekey processes", StopTestApps);
      File.Delete(Path.Join(SystemState.DataFolder, "settings.json"));
      File.Delete(state);

      var start = new ProcessStartInfo(AppPath());
      start.ArgumentList.Add("--check-self-pause");
      start.ArgumentList.Add("--check-auto-resume");
      using Process app = Process.Start(start)
          ?? throw new InvalidOperationException("The app did not start.");
      EventLog.WaitFor(() => ReadState(state) is not null, TimeSpan.FromSeconds(10));
      Focus(First().Ordinary);

      // The app drops its own hook at the first key and again 8 s later, then pauses itself
      // and, in this mode, chooses Resume 2 s after that.
      const string paused = "tooltip-self-paused=True resume-item=True";
      const string running = "tooltip-self-paused=False resume-item=False";
      bool shown = TypeUntil(() => ReadState(state) == paused, seconds: 30);
      Expect(
          "5.3 the app's tray shows the self-pause with Resume",
          shown,
          $"tray state {ReadState(state) ?? "not written"}");
      bool resumed = EventLog.WaitFor(() => ReadState(state) == running, TimeSpan.FromSeconds(10));
      Expect(
          "5.3 Resume in the app's tray ends the self-pause",
          resumed,
          $"tray state {ReadState(state) ?? "not written"}");
      StopTestApps();
    }

#else
    private void CheckAppTray() {
      Expect("5.3 the app's tray shows the self-pause", false, "run the checks from a Debug build");
    }

    private void CheckHookWatchdog() {
      Expect("5.3 hook watchdog", false, "run the checks from a Debug build");
    }

    private void CheckFreezeWatchdog() {
      Expect("5.5 freeze watchdog", false, "run the checks from a Debug build");
    }
#endif

    private void CheckRestart() {
      if (Process.GetProcessesByName(_appName).Length > 0) {
        Expect("5.5 Rekey restarts itself", false, "close the running Rekey first");
        return;
      }

      string app = AppPath();
      _restorer.Defer("Rekey settings file", SystemState.KeepFile("settings.json"));
      _restorer.Defer("Rekey restart state", SystemState.KeepFile("restart-state.txt"));
      _restorer.Defer("test Rekey processes", StopTestApps);

      // A recent real restart or saved settings could stop the test restart; start clean.
      File.Delete(Path.Join(SystemState.DataFolder, "settings.json"));
      File.Delete(Path.Join(SystemState.DataFolder, "restart-state.txt"));
      string state = Path.Join(SystemState.DataFolder, "restart-state.txt");
      DateTime stateBefore = File.Exists(state) ? File.GetLastWriteTime(state) : DateTime.MinValue;

      using Process first = Process.Start(app, "--check-freeze");
      Focus(First().Ordinary);
      var clock = Stopwatch.StartNew();
      while (clock.Elapsed < TimeSpan.FromSeconds(32)) {
        Input.Type("q");
        Thread.Sleep(TimeSpan.FromMilliseconds(300));
      }

      bool replaced = EventLog.WaitFor(
          () => first.HasExited
              && Process.GetProcessesByName(_appName).Any(process => process.Id != first.Id),
          TimeSpan.FromSeconds(15));
      bool saved = File.Exists(state) && File.GetLastWriteTime(state) > stateBefore;
      Expect(
          "5.5 Rekey restarts itself after a repeated freeze",
          replaced && saved,
          $"new process {replaced}, restart time saved {saved}");
    }

#if DEBUG
    private void FreezeWhileTyping() {
      _worker.FreezeForCheck(_freeze);
      var clock = Stopwatch.StartNew();
      while (clock.Elapsed < _freeze) {
        Input.Type("q");
        Thread.Sleep(TimeSpan.FromMilliseconds(300));
      }
    }
#endif

    private void StopTestApps() {
      foreach (Process process in Process.GetProcessesByName(_appName)) {
        using (process) {
          if (process.StartTime >= _startedAt) {
            Kill(process);
          }
        }
      }
    }
  }
}
