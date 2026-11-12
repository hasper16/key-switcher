using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace Rekey.Win32 {
  /// <summary>
  /// Turns the strings UI Automation returns into managed strings and frees them.
  /// </summary>
  internal static class UiaStrings {
    public static string Take(BSTR text) {
      try {
        return text.ToString() ?? string.Empty;
      } finally {
        Marshal.FreeBSTR((nint)text);
      }
    }
  }
}
