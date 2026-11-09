using Rekey.Core;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;

namespace Rekey.Win32.Focus {
  /// <summary>
  /// Reports a <see cref="FocusChanged" /> whenever the foreground window or the focused control
  /// changes.
  /// </summary>
  /// <remarks>
  /// The WinEvent hooks are out-of-context, so Windows calls back on the thread that installed
  /// them, through its message loop: create, start and dispose the tracker on the worker thread.
  /// </remarks>
  internal sealed class FocusTracker : IDisposable {
    private readonly Action<FocusChanged> _report;
    private readonly PasswordFieldProbe _passwords = new();

    // Windows holds only a function pointer to the delegate.
    private readonly WINEVENTPROC _onEvent;
    private UnhookWinEventSafeHandle? _foregroundHook;
    private UnhookWinEventSafeHandle? _focusHook;

    public FocusTracker(Action<FocusChanged> report) {
      _report = report;
      _onEvent = OnEvent;
    }

    /// <summary>
    /// Installs the hooks and reports the current focus.
    /// </summary>
    public void Start() {
      _foregroundHook = Hook(PInvoke.EVENT_SYSTEM_FOREGROUND);
      _focusHook = Hook(PInvoke.EVENT_OBJECT_FOCUS);
      ReportCurrent();
    }

    /// <inheritdoc />
    public void Dispose() {
      _foregroundHook?.Dispose();
      _focusHook?.Dispose();
    }

    private UnhookWinEventSafeHandle Hook(uint winEvent) {
      return PInvoke.SetWinEventHook(
          winEvent, winEvent, null, _onEvent, 0, 0, PInvoke.WINEVENT_OUTOFCONTEXT);
    }

    private void OnEvent(
        HWINEVENTHOOK hook,
        uint winEvent,
        HWND window,
        int objectId,
        int childId,
        uint threadId,
        uint time) {
      ReportCurrent();
    }

    private void ReportCurrent() {
      if (FocusTarget.Read() is not { } target) {
        return;
      }

      (string imagePath, bool isElevated) = ProcessFacts.Read(target.ProcessId);
      bool isPassword = !isElevated && _passwords.IsPassword(target.Window);
      _report(new FocusChanged(
          TimeSpan.FromMilliseconds(Environment.TickCount64), imagePath, isPassword, isElevated));
    }
  }
}
