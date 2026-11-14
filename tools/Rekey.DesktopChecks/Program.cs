using System.Globalization;
using System.IO;
using Rekey.Core;
using Rekey.Win32;

namespace Rekey.DesktopChecks {
  /// <summary>
  /// Runs the desktop checks of change add-keyboard-capture-and-layouts on this machine, or with
  /// <c>text-replacement</c> those of change add-text-replacement, or with
  /// <c>auto-correction</c> those of change add-auto-correction.
  /// </summary>
  /// <remarks>
  /// Output carries counts, kinds and codes only, never typed text. With <c>host</c> it instead
  /// shows one of the test windows the checks drive.
  /// </remarks>
  internal static class Program {
    [STAThread]
    private static int Main(string[] args) {
      if (HostWindow.IsHostCommand(args)) {
        return HostWindow.Run(args[1]);
      }

      if (args is ["restore-languages"]) {
        return RestoreLanguages();
      }

      if (args is ["text-replacement"]) {
        using var replacement = new Checks(new Settings(), new SpecWords().Evaluate);
        return replacement.RunTextReplacement();
      }

      if (args is ["auto-correction"]) {
        // No evaluator: the worker evaluates Words itself, as the app does.
        using var autoCorrection = new Checks(new Settings());
        return autoCorrection.RunAutoCorrection();
      }

      if (args is ["manual-correction"]) {
        // No evaluator: the worker evaluates Words itself, as the app does.
        using var manualCorrection = new Checks(new Settings());
        return manualCorrection.RunManualCorrection();
      }

      if (args is ["console-probe"]) {
        using var probe = new Checks();
        return probe.RunConsoleProbe();
      }

      if (args is ["try-corrections"]) {
        return TryCorrections();
      }

      using var checks = new Checks();
      return checks.Run();
    }

    // Corrects the spec's example Words in any app until Enter is pressed here, for the checks
    // by hand; prints kinds only, never text.
    private static int TryCorrections() {
      using var worker = new InputWorker(
          new Settings(),
          incident => Console.WriteLine($"Incident {incident.Type}"),
          TimeProvider.System,
          new SpecWords().Evaluate);
      worker.Replaced += result => Console.WriteLine($"Replacement {result}");
      worker.Mismatched += trace => Console.WriteLine($"Mismatch {trace}");
      var tracer = new WordEndTracer();
      worker.Observed += inputEvent => {
        if (inputEvent is FocusChanged or FocusLost) {
          Console.WriteLine($"Focus report: {worker.LastFocusMove}");
        }

        if (tracer.Add(inputEvent, worker.WordLength) is { } line) {
          string slowest = worker.SlowestKeyCheckMilliseconds.ToString(
              "F0", CultureInfo.InvariantCulture);
          Console.WriteLine($"{line}; keys rejected so far {worker.KeyRejects}, slowest "
              + $"Password Field check {slowest} ms");
        }
      };
      worker.Undone += _ => Console.WriteLine("Undo");
      worker.Start();
      Console.WriteLine("Rekey now corrects the spec's example Words typed in English (US).");
      Console.WriteLine("Press Enter in this window to stop.");
      Console.ReadLine();
      return 0;
    }

    // Puts back the language list saved by a run that stopped before it could restore it.
    private static int RestoreLanguages() {
      if (!File.Exists(SystemState.LanguageSnapshot)) {
        Console.WriteLine("No saved language list; nothing to restore.");
        return 0;
      }

      string saved = File.ReadAllText(SystemState.LanguageSnapshot);
      SystemState.RestoreLanguages(LanguageSnapshot.Parse(saved));
      File.Delete(SystemState.LanguageSnapshot);
      Console.WriteLine("The added language is removed; the list matches the saved one.");
      return 0;
    }
  }
}
