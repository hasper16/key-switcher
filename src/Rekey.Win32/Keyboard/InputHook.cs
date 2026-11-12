using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Rekey.Win32.Keyboard {
  /// <summary>
  /// Runs the low-level keyboard and mouse hooks on a thread of their own and copies what they
  /// see into an <see cref="InputRing" /> for the worker thread.
  /// </summary>
  /// <remarks>
  /// Windows removes a low-level hook whose callback is too slow, so the callbacks only copy the
  /// input, signal the worker and return: no allocation, no locks, no logging. The hook thread is
  /// the only writer of the ring's tail, of <see cref="LastInputTick" /> and of the slow-callback
  /// count. The keyboard callback may also hold a key back from the application, as
  /// <see cref="HoldGate" /> decides; the worker sends it on.
  /// </remarks>
  [SuppressMessage(
      "Security",
      "S6640:Avoid using unsafe code blocks",
      Justification = "The hook callback reads KBDLLHOOKSTRUCT in place to avoid allocating.")]
  internal sealed unsafe class InputHook : IDisposable {
    private const uint _reinstallMessage = PInvoke.WM_APP + 1;

    private readonly InputRing _ring;
    private readonly HoldGate _gate;
    private readonly EventWaitHandle _signal;
    private readonly SlowCallbackMeter _meter;
    private readonly TimeProvider _time;

    // The delegates live as long as the hooks; Windows holds only a function pointer to them.
    private readonly HOOKPROC _keyboardProc;
    private readonly HOOKPROC _mouseProc;
    private readonly Thread _thread;
    private readonly ManualResetEventSlim _installed = new();
    private uint _threadId;
    private int _installError;
    private long _lastInputTick;
    private long _reattachments;
#if DEBUG
    private long _stallRequests;
    private long _stallsDone;
    private long _stallMilliseconds;
#endif
    private UnhookWindowsHookExSafeHandle? _keyboardHook;
    private UnhookWindowsHookExSafeHandle? _mouseHook;

    public InputHook(
        InputRing ring,
        HoldGate gate,
        EventWaitHandle signal,
        SlowCallbackMeter meter,
        TimeProvider time) {
      _ring = ring;
      _gate = gate;
      _signal = signal;
      _meter = meter;
      _time = time;
      _keyboardProc = OnKeyboard;
      _mouseProc = OnMouse;
      _thread = new Thread(Run) {
        Name = "Rekey hook",
        IsBackground = true,
        Priority = ThreadPriority.AboveNormal,
      };
    }

    /// <summary>
    /// Gets the system tick (milliseconds since start, as <c>GetTickCount64</c> counts) when a
    /// hook last saw any input, including mouse moves and Rekey's own injected keys.
    /// </summary>
    public long LastInputTick => Volatile.Read(ref _lastInputTick);

    /// <summary>
    /// Gets the number of times both hooks were installed again successfully; key-ups typed while
    /// they were gone never reached them.
    /// </summary>
    public long Reattachments => Volatile.Read(ref _reattachments);

    /// <summary>
    /// Starts the hook thread and returns once both hooks are installed.
    /// </summary>
    /// <exception cref="Win32Exception">Windows refused a hook.</exception>
    public void Start() {
      // A callback compiled on its first call can exceed the hook's time budget.
      RuntimeHelpers.PrepareDelegate(_keyboardProc);
      RuntimeHelpers.PrepareDelegate(_mouseProc);
      _thread.Start();
      _installed.Wait();
      if (_installError != 0) {
        throw new Win32Exception(_installError);
      }
    }

    /// <summary>
    /// Asks the hook thread to remove both hooks and install them again.
    /// </summary>
    public void Reinstall() {
      PInvoke.PostThreadMessage(_threadId, _reinstallMessage, default, default);
    }

    /// <inheritdoc />
    public void Dispose() {
      // Bounded, as a stalled callback can hold the thread; it is a background thread.
      if (_thread.IsAlive) {
        PInvoke.PostThreadMessage(_threadId, PInvoke.WM_QUIT, default, default);
        if (!_thread.Join(TimeSpan.FromSeconds(2))) {
          return;
        }
      }

      _installed.Dispose();
    }

#if DEBUG
    /// <summary>
    /// Makes the next keyboard callback wait longer than Windows allows, so Windows removes the
    /// hook; for the desktop checks of the hook watchdog. Debug builds only.
    /// </summary>
    /// <param name="length">How long the callback waits.</param>
    public void StallNextKeyForCheck(TimeSpan length) {
      Volatile.Write(ref _stallMilliseconds, (long)length.TotalMilliseconds);
      Volatile.Write(ref _stallRequests, _stallRequests + 1);
    }

#endif
    private void Run() {
      _threadId = PInvoke.GetCurrentThreadId();

      // Creates the thread's message queue before anyone posts to it.
      PInvoke.PeekMessage(out _, HWND.Null, 0, 0, PEEK_MESSAGE_REMOVE_TYPE.PM_NOREMOVE);
      _installError = Install();
      _installed.Set();
      if (_installError != 0) {
        return;
      }

      while (PInvoke.GetMessage(out MSG message, HWND.Null, 0, 0) > 0) {
        if (message.message == _reinstallMessage) {
          Uninstall();
          if (Install() == 0) {
            Volatile.Write(ref _reattachments, _reattachments + 1);
          }
        }
      }

      Uninstall();
    }

    private int Install() {
      using FreeLibrarySafeHandle module = PInvoke.GetModuleHandle((string?)null);
      UnhookWindowsHookExSafeHandle? keyboard = null;
      UnhookWindowsHookExSafeHandle? mouse = null;
      try {
        keyboard = PInvoke.SetWindowsHookEx(
            WINDOWS_HOOK_ID.WH_KEYBOARD_LL, _keyboardProc, module, 0);
        if (keyboard.IsInvalid) {
          return Marshal.GetLastPInvokeError();
        }

        mouse = PInvoke.SetWindowsHookEx(WINDOWS_HOOK_ID.WH_MOUSE_LL, _mouseProc, module, 0);
        if (mouse.IsInvalid) {
          return Marshal.GetLastPInvokeError();
        }

        (_keyboardHook, _mouseHook) = (keyboard, mouse);
        (keyboard, mouse) = (null, null);
        return 0;
      } finally {
        // Half a pair would keep the watchdog content while clicks go unseen; whatever was not
        // handed over is removed, so the watchdog retries.
        keyboard?.Dispose();
        mouse?.Dispose();
      }
    }

    private void Uninstall() {
      _keyboardHook?.Dispose();
      _mouseHook?.Dispose();
    }

    private LRESULT OnKeyboard(int code, WPARAM wParam, LPARAM lParam) {
#if DEBUG
      StallIfRequested();
#endif
      long started = _time.GetTimestamp();
      if (code == PInvoke.HC_ACTION) {
        KBDLLHOOKSTRUCT* info = (KBDLLHOOKSTRUCT*)lParam.Value;
        Volatile.Write(ref _lastInputTick, Environment.TickCount64);
        bool injected = (info->flags & KBDLLHOOKSTRUCT_FLAGS.LLKHF_INJECTED) != 0;
        if (!injected || info->dwExtraInfo != InjectedInput.Tag) {
          uint message = (uint)wParam.Value;
          RawInputKind kind = message is PInvoke.WM_KEYDOWN or PInvoke.WM_SYSKEYDOWN
              ? RawInputKind.KeyDown
              : RawInputKind.KeyUp;
          bool extended = (info->flags & KBDLLHOOKSTRUCT_FLAGS.LLKHF_EXTENDED) != 0;
          var vk = (ushort)info->vkCode;
          var scanCode = (ushort)info->scanCode;
          long tick = LastInputTick;
          Holding holding = _gate.Judge(
              kind, vk, scanCode, extended, _ring.Written, tick, _ring.HasRoom);
          Enqueue(new RawInput(kind, vk, scanCode, extended, tick, Holding: holding));
          if (holding != Holding.None) {
            // Held back from the application; the worker re-injects it in order.
            _meter.Record(started, _time.GetTimestamp());
            return (LRESULT)1;
          }
        }
      }

      _meter.Record(started, _time.GetTimestamp());
      return PInvoke.CallNextHookEx(null, code, wParam, lParam);
    }

    private LRESULT OnMouse(int code, WPARAM wParam, LPARAM lParam) {
      long started = _time.GetTimestamp();
      if (code == PInvoke.HC_ACTION) {
        Volatile.Write(ref _lastInputTick, Environment.TickCount64);

        // Moves and wheel turns only prove the hook is alive; button presses end the Word.
        if ((uint)wParam.Value is PInvoke.WM_LBUTTONDOWN or PInvoke.WM_RBUTTONDOWN
            or PInvoke.WM_MBUTTONDOWN or PInvoke.WM_XBUTTONDOWN) {
          _gate.NoteClick(_ring.Written);
          Enqueue(new RawInput(RawInputKind.MouseDown, 0, 0, false, LastInputTick));
        }
      }

      _meter.Record(started, _time.GetTimestamp());
      return PInvoke.CallNextHookEx(null, code, wParam, lParam);
    }

#if DEBUG
    // The caller writes the requests; only this thread writes the stalls done.
    private void StallIfRequested() {
      if (Volatile.Read(ref _stallRequests) != _stallsDone) {
        _stallsDone = Volatile.Read(ref _stallRequests);
        Thread.Sleep(TimeSpan.FromMilliseconds(Volatile.Read(ref _stallMilliseconds)));
      }
    }

#endif
    private void Enqueue(in RawInput input) {
      if (_ring.TryWrite(input)) {
        _signal.Set();
      }
    }
  }
}
