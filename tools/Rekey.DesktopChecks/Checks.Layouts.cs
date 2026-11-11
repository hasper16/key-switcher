using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using Rekey.Core;
using Rekey.Core.Languages;
using Windows.Win32.Foundation;

namespace Rekey.DesktopChecks {
  // Checks 4.1, 4.3 and 4.4: the Layout list, the current Layout and verified switches.
  internal sealed partial class Checks {
    private const string _consoleTitle = "Rekey check console";

    // Languages the checks may add for a moment, with the KLID of their default Layout.
    private static readonly (string Tag, uint Klid)[] _extraLanguages = [
        ("de-DE", 0x00000407), ("fr-FR", 0x0000040C), ("it-IT", 0x00000410)];

    private static readonly TimeSpan _layoutListWait = TimeSpan.FromSeconds(15);

    private void CheckSwitches() {
      if (TwoLayouts() is not var (first, second)) {
        Expect("4.4 verified switch", false, "fewer than two Layouts installed");
        return;
      }

      ExpectSwitch("4.4 in a test window", First().Ordinary, first, second);
      if (_browserBox is { } browser) {
        ExpectSwitch("4.4 in a browser text box", browser, first, second);
      }

      if (StartConsole() is { } console) {
        ExpectSwitch("4.4 in a console window", console, first, second);
      } else {
        Expect("4.4 in a console window", false, "the console window did not appear");
      }
    }

    private void CheckLayoutModes() {
      if (TwoLayouts() is not var (first, second)) {
        Expect("4.3 current Layout", false, "fewer than two Layouts installed");
        return;
      }

      bool perWindowBefore = SystemState.PerWindowInput();
      _restorer.Defer(
          "per-window input setting", () => SystemState.SetPerWindowInput(perWindowBefore));

      SystemState.SetPerWindowInput(true);
      Thread.Sleep(_settle);
      SwitchIn(First().Ordinary, first);
      SwitchIn(Second().Ordinary, second);
      bool firstKept = FocusShows(First().Ordinary, first);
      bool secondKept = FocusShows(Second().Ordinary, second);
      Expect(
          "4.3 per-window mode: each window keeps its Layout",
          firstKept && secondKept,
          $"window A {firstKept}, window B {secondKept}");
      if (_console is { } console) {
        SwitchIn(console, first);
        bool consoleKept = FocusShows(Second().Ordinary, second) && FocusShows(console, first);
        Expect("4.3 per-window mode: a console keeps its Layout", consoleKept, $"{consoleKept}");
      }

      SystemState.SetPerWindowInput(false);
      Thread.Sleep(_settle);
      SwitchIn(First().Ordinary, second);
      bool shared = FocusShows(Second().Ordinary, second);
      Expect("4.3 global mode: the Layout follows to the next window", shared, $"{shared}");
      SwitchIn(First().Ordinary, first);
    }

    private void CheckLayoutList() {
      if (File.Exists(SystemState.LanguageSnapshot)) {
        Expect(
            "4.1 Layout list",
            false,
            "a language list saved by an earlier run is waiting; run restore-languages first");
        return;
      }

      // Read before any change; an unreadable list stops the check here.
      LanguageList original = SystemState.ReadLanguages();
      (string tag, uint klid) = _extraLanguages.FirstOrDefault(extra => !original.Has(extra.Tag));
      if (tag is null) {
        Expect("4.1 Layout added", false, "every spare language is already installed");
        return;
      }

      File.WriteAllText(SystemState.LanguageSnapshot, original.ToText());
      _restorer.Defer("language list", () => {
        SystemState.RestoreLanguages(original);
        File.Delete(SystemState.LanguageSnapshot);
      });
      var added = new LayoutId(klid);
      EventLog.Position mark = _log.Mark();
      var clock = Stopwatch.StartNew();
      SystemState.AddLanguage(tag);
      bool seen = EventLog.WaitFor(() => ListedSince(mark, added) == true, _layoutListWait);
      Expect(
          "4.1 an added Layout is seen within 5 s",
          seen && clock.Elapsed <= TimeSpan.FromSeconds(5),
          string.Create(
              CultureInfo.InvariantCulture,
              $"seen {seen} after {clock.Elapsed.TotalSeconds:F1} s"));

      EventLog.Position removed = _log.Mark();
      SystemState.RestoreLanguages(original);
      bool gone = EventLog.WaitFor(() => ListedSince(removed, added) == false, _layoutListWait);
      Expect("4.1 a removed Layout disappears", gone, $"gone {gone}");
      Expect("4.1 the language list is restored", true, "read back the same, settings included");
    }

    // Whether the latest Layout list since the mark includes the Layout; null when none came.
    private bool? ListedSince(EventLog.Position mark, LayoutId layout) {
      LayoutsInstalledChanged? latest = _log.EventsSince(mark)
          .Select(seen => seen.Event)
          .OfType<LayoutsInstalledChanged>()
          .LastOrDefault();
      return latest?.Layouts.Contains(layout);
    }

    private (LayoutId First, LayoutId Second)? TwoLayouts() {
      LayoutId[] text = [.. InstalledLayouts().Where(layout => layout.Value >> 28 != 0xE)];
      return text.Length >= 2 ? (text[0], text[1]) : null;
    }

    private void ExpectSwitch(string item, Point at, LayoutId first, LayoutId second) {
      Focus(at);
      LayoutId target = CurrentLayout() == first ? second : first;
      EventLog.Position mark = _log.Mark();
      bool landed = SwitchIn(at, target);
      bool clean = !_log.IncidentsSince(mark).Contains(IncidentType.SwitchNotLanded);
      Expect(item, landed && clean, $"landed {landed}, no failed-switch Incident {clean}");
    }

    // Switches the window at the point to a Layout; returns whether the worker saw it land.
    // Waits for the worker's own report on this request, so focus never moves while the switch
    // is pending, even when the window already had the Layout.
    private bool SwitchIn(Point at, LayoutId target) {
      Focus(at);
      EventLog.Position mark = _log.Mark();
      _worker.SwitchLayout(target);
      bool reported = EventLog.WaitFor(
          () => _log.EventsSince(mark).Exists(
              seen => seen.Event is LayoutChanged changed && changed.Layout == target)
              || _log.IncidentsSince(mark).Contains(IncidentType.SwitchNotLanded),
          TimeSpan.FromSeconds(2));
      return reported && CurrentLayout() == target;
    }

    private bool FocusShows(Point at, LayoutId expected) {
      Focus(at);
      return EventLog.WaitFor(() => CurrentLayout() == expected, TimeSpan.FromSeconds(2));
    }

    private Point? StartConsole() {
      if (_console is not null) {
        return _console;
      }

      string shell = Path.Join(Environment.SystemDirectory, "cmd.exe");
      var start = new ProcessStartInfo(shell) { UseShellExecute = true };
      start.ArgumentList.Add("/k");
      start.ArgumentList.Add($"title {_consoleTitle}");
      Process console = Process.Start(start)
          ?? throw new InvalidOperationException("The console did not start.");
      _restorer.Defer("test console", () => {
        Kill(console);
        console.Dispose();
      });
      HWND? window = Desktop.FindWindow(_consoleTitle, TimeSpan.FromSeconds(10));
      _console = window is { } found ? Desktop.Center(found) : null;
      return _console;
    }
  }
}
