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

      using var checks = new Checks();
      return checks.Run();
    }
  }
}
