using System.Collections.Frozen;
using Rekey.Core;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;

namespace Rekey.Win32.Focus {
  /// <summary>
  /// Follows the application of the foreground window for the tray menu, leaving out Rekey's
  /// own windows and the taskbar a click on the tray icon brings to the front.
  /// </summary>
  /// <remarks>
  /// Create, start and dispose it on a thread that runs a message loop, such as the UI thread;
  /// <see cref="Changed" /> is raised on that thread.
  /// </remarks>
  public sealed class ForegroundApps : IDisposable {
    private static readonly FrozenSet<string> _taskbarClasses = FrozenSet.Create(
        StringComparer.Ordinal,
        "Shell_TrayWnd",
        "Shell_SecondaryTrayWnd",
        "NotifyIconOverflowWindow",
        "TopLevelWindowForOverflowXamlIsland",
        "Xaml_WindowedPopupClass");

    // Windows holds only a function pointer to the delegate.
    private readonly WINEVENTPROC _onEvent;
    private UnhookWinEventSafeHandle? _hook;

    /// <summary>
    /// Initializes a new instance of the <see cref="ForegroundApps" /> class.
    /// </summary>
    public ForegroundApps() {
      _onEvent = OnEvent;
    }

    /// <summary>
    /// Occurs when another application's window comes to the front, with that application.
    /// </summary>
    public event Action<AppId>? Changed;

    /// <summary>
    /// Returns whether a window of the given class is an application the user types in rather
    /// than a part of the taskbar.
    /// </summary>
    /// <param name="className">The window class name.</param>
    /// <returns>
    /// <see langword="true" /> if the window counts as an application; otherwise,
    /// <see langword="false" />.
    /// </returns>
    public static bool CountsAsApp(string className) {
      return !_taskbarClasses.Contains(className);
    }

    /// <summary>
    /// Installs the hook and reports the application in front now.
    /// </summary>
    public void Start() {
      _hook = PInvoke.SetWinEventHook(
          PInvoke.EVENT_SYSTEM_FOREGROUND,
          PInvoke.EVENT_SYSTEM_FOREGROUND,
          null,
          _onEvent,
          0,
          0,
          PInvoke.WINEVENT_OUTOFCONTEXT | PInvoke.WINEVENT_SKIPOWNPROCESS);
      Report(PInvoke.GetForegroundWindow());
    }

    /// <inheritdoc />
    public void Dispose() {
      _hook?.Dispose();
    }

    private static string ClassOf(HWND window) {
      Span<char> name = stackalloc char[256];
      int length = PInvoke.GetClassName(window, name);
      return new string(name[..Math.Max(0, length)]);
    }

    private void OnEvent(
        HWINEVENTHOOK hook,
        uint winEvent,
        HWND window,
        int objectId,
        int childId,
        uint threadId,
        uint time) {
      Report(window);
    }

    private void Report(HWND window) {
      if (window.IsNull || !CountsAsApp(ClassOf(window))) {
        return;
      }

      _ = PInvoke.GetWindowThreadProcessId(window, out uint processId);
      if (processId == 0 || processId == (uint)Environment.ProcessId) {
        return;
      }

      AppId app = AppIdentity.Resolve(
          processId,
          id => ProcessFacts.Read(id).App,
          () => AppIdentity.HostedProcess(window));
      if (app.ExePath.Length > 0 || app.Aumid is not null) {
        Changed?.Invoke(app);
      }
    }
  }
}
