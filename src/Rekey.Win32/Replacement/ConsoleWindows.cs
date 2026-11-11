using Windows.Win32;

namespace Rekey.Win32.Replacement {
  /// <summary>
  /// Tells console windows apart, where a UI Automation selection is the terminal's own: the
  /// shell's line editor never sees it, so typing does not replace the selected text.
  /// </summary>
  internal static class ConsoleWindows {
    // conhost, Windows Terminal, and the hidden window of a pseudo console.
    private static readonly string[] _classes = [
        "ConsoleWindowClass", "CASCADIA_HOSTING_WINDOW_CLASS", "PseudoConsoleWindow"];

    /// <summary>
    /// Returns whether a top-level window class belongs to a console.
    /// </summary>
    public static bool IsConsole(string className) {
      return _classes.Contains(className, StringComparer.Ordinal);
    }

    /// <summary>
    /// Returns whether the foreground window is a console; call it on the worker thread.
    /// </summary>
    public static bool ForegroundIsConsole() {
      Span<char> name = stackalloc char[64];
      int length = PInvoke.GetClassName(PInvoke.GetForegroundWindow(), name);
      return IsConsole(name[..Math.Max(length, 0)].ToString());
    }
  }
}
