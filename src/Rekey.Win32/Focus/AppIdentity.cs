using Rekey.Core;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace Rekey.Win32.Focus {
  /// <summary>
  /// Names the application of the foreground window (design D4 of add-auto-correction).
  /// </summary>
  internal static class AppIdentity {
    private const string _frameHostName = "ApplicationFrameHost.exe";
    private const string _coreWindowClass = "Windows.UI.Core.CoreWindow";

    /// <summary>
    /// Returns the application of a window owned by a process.
    /// </summary>
    /// <remarks>
    /// The frame host process owns the windows of every Store app it hosts, so for its windows
    /// the application is the hosted one.
    /// </remarks>
    /// <param name="processId">The process that owns the foreground window.</param>
    /// <param name="read">Reads the identity of a process.</param>
    /// <param name="hostedProcess">
    /// Finds the process of the app hosted in the foreground window; asked only for a frame
    /// host window.
    /// </param>
    public static AppId Resolve(
        uint processId, Func<uint, AppId> read, Func<uint?> hostedProcess) {
      AppId owner = read(processId);
      bool isFrameHost = Path.GetFileName(owner.ExePath)
          .Equals(_frameHostName, StringComparison.OrdinalIgnoreCase);
      if (!isFrameHost || hostedProcess() is not { } hosted || hosted == processId) {
        return owner;
      }

      // A hosted process that cannot be read names nothing.
      AppId app = read(hosted);
      return app.Aumid is null && app.ExePath.Length == 0 ? owner : app;
    }

    /// <summary>
    /// Returns the process of the app a frame host window shows, or <see langword="null" />
    /// when the window has no core window of an app.
    /// </summary>
    public static uint? HostedProcess(HWND frame) {
      HWND core = PInvoke.FindWindowEx(frame, HWND.Null, _coreWindowClass, null);
      if (core.IsNull) {
        return null;
      }

      _ = PInvoke.GetWindowThreadProcessId(core, out uint processId);
      return processId == 0 ? null : processId;
    }
  }
}
