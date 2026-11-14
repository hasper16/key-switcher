using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Rekey.Win32 {
  /// <summary>
  /// Reads display facts the tray icon needs.
  /// </summary>
  public static class Display {
    /// <summary>
    /// Returns the size of a small icon, such as a tray icon, in pixels at the system DPI.
    /// </summary>
    /// <returns>The width and height of a small icon, in pixels.</returns>
    public static int SmallIconSize() {
      // WPF makes the process system-DPI aware, so the metric is already scaled.
      int size = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXSMICON);
      return size > 0 ? size : 16;
    }
  }
}
