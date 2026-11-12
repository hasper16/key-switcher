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
    private readonly Func<Typing> _typingNow;
    private readonly PasswordFieldProbe _passwords = new();

    // Windows holds only a function pointer to the delegate.
    private readonly WINEVENTPROC _onEvent;
    private UnhookWinEventSafeHandle? _foregroundHook;
    private UnhookWinEventSafeHandle? _focusHook;
    private bool _blind;
    private HWND _reportedWindow;
    private IUIAutomationElement? _reportedElement;

    // An element that took focus from the reported text control while the user typed, such as
    // a row of an autocomplete list; keys still reach the text control (design D7).
    private IUIAutomationElement? _setAside;

    /// <summary>
    /// Gets why focus last counted as moved, for diagnostics: codes and class names, never
    /// text; read on the worker thread.
    /// </summary>
    public string LastMove { get; private set; } = "none";

    /// <summary>
    /// Gets the text control the Word is typed in while an element that takes no text holds
    /// UI Automation focus (design D7); otherwise, <see langword="null" />.
    /// </summary>
    public IUIAutomationElement? TypingElement => _setAside is null ? null : _reportedElement;

    /// <summary>
    /// Gets how many focus moves kept the Word so far (design D7), for diagnostics; read on the
    /// worker thread.
    /// </summary>
    public int SetAsideMoves { get; private set; }

    public FocusTracker(Action<InputEvent> report, Func<Typing> typingNow) {
      _report = report;
      _typingNow = typingNow;
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
    /// Returns whether keyboard focus is now in another window than the one last reported.
    /// </summary>
    /// <remarks>
    /// Reads only the focused window, which is cheap; <see cref="CatchUp" /> also compares the
    /// control inside it.
    /// </remarks>
    public bool WindowChanged() {
      return FocusTarget.Read()?.Window != _reportedWindow;
    }

    /// <summary>
    /// Reports the focus at once when the focused window or control is not the one last reported,
    /// as when a key arrives before the WinEvent of a focus move.
    /// </summary>
    /// <param name="keyTime">The key's time.</param>
    /// <param name="typing">Whether only typing could have moved focus by this key.</param>
    public void CatchUp(TimeSpan keyTime, Typing typing) {
      // A different window, or a different control inside the same window (Tab, a click, the
      // page's own script), means focus moved without a WinEvent yet.
      if (MovedSinceReport(typing)) {
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
      // Chromium apps such as VS Code raise focus events for the control that already has
      // focus once UI Automation has read their text; reporting those would end the Word.
      if (MovedSinceReport(_typingNow())) {
        ReportCurrent(EventTime(time));
      }
    }

    // Whether focus is now in another window or another control than the one last reported;
    // an unknown focus counts as moved. LastMove says why, for diagnostics.
    private bool MovedSinceReport(Typing typing) {
      if (_blind || FocusTarget.Read()?.Window is not { } window) {
        LastMove = "focus unknown";
        return true;
      }

      if (window != _reportedWindow) {
        LastMove = "other window";
        return true;
      }

      IUIAutomationElement? now = _passwords.FocusedElement();
      bool? same = _passwords.Compare(now, _reportedElement);
      if (same == true) {
        _setAside = null;
        return false;
      }

      if (same == false && KeepsWord(now, typing)) {
        return false;
      }

      LastMove = same is null
          ? "cannot compare"
          : $"other control {PasswordFieldProbe.Describe(_reportedElement)} -> "
              + PasswordFieldProbe.Describe(now);
      return true;
    }

    // Design D7: a move from the text control to an element that takes no text keeps the Word
    // while the user types; one still waiting for its cause is decided at the next key.
    private bool KeepsWord(IUIAutomationElement? now, Typing typing) {
      if (_setAside is not null && _passwords.Same(now, _setAside)) {
        return true;
      }

      if (PasswordFieldProbe.Facts(_reportedElement) is not { } from
          || PasswordFieldProbe.Facts(now) is not { } to
          || !SuggestionFocus.KeepsWord(from.ControlType, to.ControlType, to.IsPassword)) {
        return false;
      }

      if (typing == Typing.Yes) {
        _setAside = now;
        SetAsideMoves++;
      }

      return typing != Typing.No;
    }

    private void ReportCurrent(TimeSpan time) {
      _setAside = null;
      if (_blind || FocusTarget.Read() is not { } target) {
        // Keys typed now may go to a Password Field the tracker cannot see.
        _reportedWindow = HWND.Null;
        _reportedElement = null;
        _report(new FocusLost(time));
        return;
      }

      _reportedWindow = target.Window;
      _reportedElement = _passwords.FocusedElement();
      (string imagePath, bool isElevated) = ProcessFacts.Read(target.ProcessId);
      bool isPassword = !isElevated && _passwords.IsPassword(target.Window);
      _report(new FocusChanged(time, imagePath, isPassword, isElevated));
    }
  }
}
