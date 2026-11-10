using Rekey.Core;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;

namespace Rekey.Win32.Focus {
  /// <summary>
  /// Reports a <see cref="FocusChanged" />, or <see cref="FocusLost" /> when no foreground window
  /// can be read, whenever the foreground window or the focused control
  /// changes.
  /// </summary>
  /// <remarks>
  /// The WinEvent hooks are out-of-context, so Windows calls back on the thread that installed
  /// them, through its message loop: create, start and dispose the tracker on the worker thread.
  /// </remarks>
  internal sealed class FocusTracker : IDisposable {
    private readonly Action<InputEvent> _report;
    private readonly PasswordFieldProbe _passwords = new();

    // Windows holds only a function pointer to the delegate.
    private readonly WINEVENTPROC _onEvent;
    private UnhookWinEventSafeHandle? _foregroundHook;
    private UnhookWinEventSafeHandle? _focusHook;
    private bool _blind;
    private HWND _reportedWindow;

    public FocusTracker(Action<InputEvent> report) {
      _report = report;
      _onEvent = OnEvent;
    }

    /// <summary>
    /// Installs the hooks and reports the current focus.
    /// </summary>
    public void Start() {
      _foregroundHook = Hook(PInvoke.EVENT_SYSTEM_FOREGROUND);
      _focusHook = Hook(PInvoke.EVENT_OBJECT_FOCUS);

      // Without both hooks a move into a Password Field could go unseen, so focus stays unknown
      // and the engine keeps no Keystrokes.
      _blind = _foregroundHook.IsInvalid || _focusHook.IsInvalid;
      ReportCurrent(TimeSpan.FromMilliseconds(Environment.TickCount64));
    }

    /// <inheritdoc />
    public void Dispose() {
      _foregroundHook?.Dispose();
      _focusHook?.Dispose();
    }

    /// <summary>
    /// Returns whether the focused control is a Password Field now, asking UI Automation again.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> if it is one, or if the tracker cannot tell; otherwise,
    /// <see langword="false" />.
    /// </returns>
    public bool IsPasswordFocused() {
      return _blind || FocusTarget.Read() is not { } target || _passwords.IsPassword(target.Window);
    }

    /// <summary>
    /// Reports the focus at once when the focused window is not the one last reported, as when a
    /// key arrives before the WinEvent of a window change.
    /// </summary>
    /// <param name="keyTime">The time of the key about to be processed.</param>
    public void CatchUp(TimeSpan keyTime) {
      FocusTarget? target = FocusTarget.Read();
      if (target?.Window != _reportedWindow) {
        // Stamped with the key's time, so the engine drops that key: its focus is uncertain.
        ReportCurrent(keyTime);
      }
    }

    // WinEvents carry 32-bit GetTickCount time; widen it by its distance from the 64-bit now, so
    // it compares with the hook's key times even when the worker reads it late.
    private static TimeSpan EventTime(uint time) {
      long now = Environment.TickCount64;
      return TimeSpan.FromMilliseconds(now - unchecked((uint)now - time));
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
      ReportCurrent(EventTime(time));
    }

    private void ReportCurrent(TimeSpan time) {
      if (_blind || FocusTarget.Read() is not { } target) {
        // Keys typed now may go to a Password Field the tracker cannot see.
        _reportedWindow = HWND.Null;
        _report(new FocusLost(time));
        return;
      }

      _reportedWindow = target.Window;
      (string imagePath, bool isElevated) = ProcessFacts.Read(target.ProcessId);
      bool isPassword = !isElevated && _passwords.IsPassword(target.Window);
      _report(new FocusChanged(time, imagePath, isPassword, isElevated));
    }
  }
}
