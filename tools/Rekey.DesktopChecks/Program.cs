using System.IO;

namespace Rekey.DesktopChecks {
  /// <summary>
  /// Runs the desktop checks of change add-keyboard-capture-and-layouts on this machine.
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

      using var checks = new Checks();
      return checks.Run();
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
