using Windows.Win32;

namespace Rekey.Win32.Replacement {
  /// <summary>
  /// Tells console windows apart by the foreground window's class (design D3).
  /// </summary>
  internal static class ConsoleWindows {
    // The hidden window of a pseudo console: not proven to take typed text.
    private const string _unproven = "PseudoConsoleWindow";

    // conhost and Windows Terminal: the desktop probe, with the buffer read proven, showed
    // Backspaces and Unicode characters arriving.
    private static readonly string[] _consoles = [
        "ConsoleWindowClass", "CASCADIA_HOSTING_WINDOW_CLASS"];

    /// <summary>
    /// Returns how a window with the given top-level class takes a replacement.
    /// </summary>
    public static TextTarget TargetOf(string className) {
      if (_consoles.Contains(className, StringComparer.Ordinal)) {
        return TextTarget.Console;
      }

      return className == _unproven ? TextTarget.Unsupported : TextTarget.Editable;
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
