using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Rekey.Win32.Focus {
  /// <summary>
  /// Identifies the window that receives the user's typing: the focused window of the
  /// foreground window's thread.
  /// </summary>
  /// <param name="Window">The focused window, or the foreground window when none has focus.</param>
  /// <param name="ThreadId">The thread that owns <paramref name="Window" />.</param>
  /// <param name="ProcessId">The process that owns the foreground window.</param>
  internal readonly record struct FocusTarget(HWND Window, uint ThreadId, uint ProcessId) {
    /// <summary>
    /// Returns the window that receives typing now, or <see langword="null" /> when there is no
    /// foreground window, as while the desktop switches.
    /// </summary>
    public static FocusTarget? Read() {
      HWND foreground = PInvoke.GetForegroundWindow();
      if (foreground.IsNull) {
        return null;
      }

      uint foregroundThread = PInvoke.GetWindowThreadProcessId(foreground, out uint processId);
      var info = new GUITHREADINFO { cbSize = (uint)Marshal.SizeOf<GUITHREADINFO>() };

      // A console's foreground window belongs to its host process; the focused window tells
      // which thread's Layout the user is typing with.
      HWND focus = PInvoke.GetGUIThreadInfo(foregroundThread, ref info) && !info.hwndFocus.IsNull
          ? info.hwndFocus
          : foreground;
      return new FocusTarget(focus, PInvoke.GetWindowThreadProcessId(focus), processId);
    }
  }
}
