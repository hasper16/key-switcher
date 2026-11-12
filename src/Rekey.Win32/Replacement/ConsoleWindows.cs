using Windows.Win32;

namespace Rekey.Win32.Replacement {
  /// <summary>
  /// Tells console windows apart by the foreground window's class (design D3).
  /// </summary>
  internal static class ConsoleWindows {
    // conhost: the desktop probe showed Backspaces and Unicode characters arriving.
    private const string _conhost = "ConsoleWindowClass";

    // Windows Terminal and the hidden window of a pseudo console: not yet proven.
    private static readonly string[] _unproven = [
        "CASCADIA_HOSTING_WINDOW_CLASS", "PseudoConsoleWindow"];

    /// <summary>
    /// Returns how a window with the given top-level class takes a replacement.
    /// </summary>
    public static TextTarget TargetOf(string className) {
      if (className == _conhost) {
        return TextTarget.Console;
      }

      return _unproven.Contains(className, StringComparer.Ordinal)
          ? TextTarget.Unsupported
          : TextTarget.Editable;
    }

    /// <summary>
    /// Returns how the foreground window takes a replacement; call it on the worker thread.
    /// </summary>
    public static TextTarget ForegroundTarget() {
      Span<char> name = stackalloc char[64];
      int length = PInvoke.GetClassName(PInvoke.GetForegroundWindow(), name);
      return TargetOf(name[..Math.Max(length, 0)].ToString());
    }
  }
}
