using System.Diagnostics;
using System.Drawing;
using System.IO;
using Rekey.Core;
using Rekey.Core.Languages;
using Rekey.Win32;
using Windows.Win32;

namespace Rekey.DesktopChecks {
  // Runs every desktop check against the real hooks, windows and settings of this session.
  internal sealed partial class Checks : IDisposable {
    private static readonly TimeSpan _settle = TimeSpan.FromMilliseconds(700);
    private static readonly TimeSpan _eventWait = TimeSpan.FromSeconds(3);

    private readonly Report _report = new();
    private readonly EventLog _log = new();
    private readonly Restorer _restorer;
    private readonly InputWorker _worker;
    private readonly DateTime _startedAt = DateTime.Now;
    private HostWindow? _first;
    private HostWindow? _second;
    private Point? _browserBox;
    private Point? _console;

    public Checks() {
      _restorer = new Restorer(_report);
      _worker = new InputWorker(new Settings(), _log.AddIncident, TimeProvider.System);
      _worker.Observed += inputEvent => _log.Add(inputEvent, _worker.WordLength);
      _worker.SelfPauseChanged += _log.AddSelfPause;
    }

    public int Run() {
      Console.WriteLine("Rekey desktop checks. Do not touch the mouse or keyboard for about");
      Console.WriteLine("5 minutes. Every changed setting is restored at the end.");
      Console.WriteLine("Starting in 5 s.");
      Thread.Sleep(TimeSpan.FromSeconds(5));
      Point cursor = SystemState.Cursor();
      _restorer.Defer("mouse position", () => PInvoke.SetCursorPos(cursor.X, cursor.Y));
      bool capsLock = SystemState.CapsLockOn();
      _restorer.Defer("Caps Lock", () => {
        if (SystemState.CapsLockOn() != capsLock) {
          Input.ToggleCapsLock();
        }
      });

      EventLog.Position start = _log.Mark();
      _worker.Start();
      Thread.Sleep(TimeSpan.FromSeconds(2));
      _first = HostWindow.Start("Rekey check A", new Point(80, 80));
      _second = HostWindow.Start("Rekey check B", new Point(560, 80));
      Thread.Sleep(_settle);

      Step("2.1 hooks", () => CheckHooks(start));
      Step("3.2 Win32 masked edit", CheckMaskedEdit);
      Step("3.3 browser page", CheckBrowser);
      Step("4.4 verified switch", CheckSwitches);
      Step("4.3 current Layout", CheckLayoutModes);
      Step("4.1 Layout list", CheckLayoutList);
      Step("5.3 hook watchdog", CheckHookWatchdog);
      Step("5.5 freeze watchdog", CheckFreezeWatchdog);
      Step("5.3 the app's tray", CheckAppTray);
      Step("5.5 restart after a freeze", CheckRestart);

      Dispose();
      _report.PrintSummary();
      return _report.Failed ? 1 : 0;
    }

    public void Dispose() {
      _restorer.Dispose();
      _first?.Dispose();
      _second?.Dispose();
      _first = null;
      _second = null;
      _worker.Dispose();
    }

    private static string RepositoryRoot() {
      DirectoryInfo? folder = new(AppContext.BaseDirectory);
      while (folder is not null && !File.Exists(Path.Join(folder.FullName, "Rekey.slnx"))) {
        folder = folder.Parent;
      }

      return folder?.FullName
          ?? throw new InvalidOperationException("Run the checks from the repository.");
    }

    private static void Focus(Point at) {
      Input.Click(at);
      Thread.Sleep(_settle);
    }

    private static void Kill(Process process) {
      if (!process.HasExited) {
        process.Kill();
      }
    }

    private HostWindow First() {
      return _first ?? throw new InvalidOperationException("No test window.");
    }

    private HostWindow Second() {
      return _second ?? throw new InvalidOperationException("No test window.");
    }

    // Runs one check; an exception fails it with its type only.
    private void Step(string name, Action check) {
      try {
        check();
      } catch (Exception e) when (e is not OutOfMemoryException) {
        _report.Add(name, false, $"stopped by {e.GetType().Name}");
      }
    }

    private void Expect(string item, bool passed, string detail) {
      _report.Add(item, passed, detail);
    }

    private List<KeyDown> WaitForKeys(EventLog.Position mark, int count) {
      EventLog.WaitFor(() => _log.KeyDownsSince(mark).Count >= count, _eventWait);
      Thread.Sleep(TimeSpan.FromMilliseconds(200));
      return _log.KeyDownsSince(mark);
    }

#if DEBUG
    private bool WaitForIncident(EventLog.Position mark, IncidentType type, TimeSpan timeout) {
      return EventLog.WaitFor(() => _log.IncidentsSince(mark).Contains(type), timeout);
    }
#endif

    // The Layout of the latest LayoutChanged the worker saw.
    private LayoutId? CurrentLayout() {
      return _log.EventsSince(default)
          .Select(seen => seen.Event)
          .OfType<LayoutChanged>()
          .LastOrDefault()?.Layout;
    }

    private IReadOnlyList<LayoutId> InstalledLayouts() {
      return _log.EventsSince(default)
          .Select(seen => seen.Event)
          .OfType<LayoutsInstalledChanged>()
          .LastOrDefault()?.Layouts ?? [];
    }
  }
}
