using System.Diagnostics;
using System.IO;
using Rekey.Core;

namespace Rekey.App.Tray {
  // The name users know an app by, for "Turn off in Telegram" and the Excluded Apps list.
  internal static class AppNames {
    public static string Of(AppId app) {
      string? description = null;
      try {
        description = FileVersionInfo.GetVersionInfo(app.ExePath).FileDescription;
      } catch (FileNotFoundException) {
        // The executable is gone or out of reach; its file name still names it.
      } catch (ArgumentException) {
        // An empty path: a Store app Rekey could not trace to a file.
      }

      return Choose(description, app.ExePath.Length > 0 ? app.ExePath : app.Key);
    }

    public static string Choose(string? description, string exePath) {
      return string.IsNullOrWhiteSpace(description)
          ? Path.GetFileNameWithoutExtension(exePath)
          : description.Trim();
    }
  }
}
