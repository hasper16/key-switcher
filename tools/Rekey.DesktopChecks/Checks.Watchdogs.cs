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

#if DEBUG
    // Windows removes a low-level hook whose callback runs longer than this.
    private static int HookTimeoutMilliseconds() {
      object? value = Registry.GetValue(
          @"HKEY_CURRENT_USER\Control Panel\Desktop", "LowLevelHooksTimeout", null);
      return value switch {
        int number => number,
        string text when int.TryParse(text, CultureInfo.InvariantCulture, out int number) => number,
        _ => 1000,
      };
    }
#endif

#if DEBUG
    private void CheckHookWatchdog() {
      TimeSpan stall = TimeSpan.FromMilliseconds(HookTimeoutMilliseconds() + 1500);
      Focus(First().Ordinary);

      // First drop: Shift goes up while the hook is gone.
      EventLog.Position first = _log.Mark();
      Input.Key(VIRTUAL_KEY.VK_LSHIFT, 0x2A, up: false);
      _worker.StallHookForCheck(stall);
      Input.Type("q");
      Input.Key(VIRTUAL_KEY.VK_LSHIFT, 0x2A, up: true);
      Input.Type("wer");
      bool reattached = WaitForIncident(first, IncidentType.HookReinstalled, _watchdogWait);
      Expect("5.3 a dropped hook is reinstalled", reattached, $"HookReinstalled {reattached}");
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
      Input.Type("wer");
      bool paused = WaitForIncident(second, IncidentType.SelfPaused, _watchdogWait)
          && _log.SelfPausesSince(second).Exists(until => until is not null);
      Expect("5.3 a second drop pauses Rekey itself", paused, $"SelfPaused {paused}");
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
#else
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

      string app = Path.Join(
          RepositoryRoot(), "src", _appName, "bin", "Debug", "net10.0-windows", $"{_appName}.exe");
      _restorer.Defer("Rekey settings file", SystemState.KeepFile("settings.json"));
      _restorer.Defer("Rekey restart state", SystemState.KeepFile("restart-state.txt"));
      _restorer.Defer("test Rekey processes", StopTestApps);
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
