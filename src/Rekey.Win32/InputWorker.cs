using System.Diagnostics.CodeAnalysis;
using Rekey.Core;
using Rekey.Win32.Focus;
using Rekey.Win32.Keyboard;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Rekey.Win32 {
  /// <summary>
  /// Runs the worker thread: it owns the engine, turns hook input into input events, feeds them
  /// to the engine and executes the actions it returns.
  /// </summary>
  /// <remarks>
  /// Everything the engine touches lives on this one thread, so nothing in it is shared. The
  /// thread waits on the hook's signal and on its own message queue at once.
  /// </remarks>
  public sealed class InputWorker : IDisposable {
    private const int _ringCapacity = 4096;
    private const uint _pollMilliseconds = 250;

    private readonly InputRing _ring = new(_ringCapacity);
    private readonly AutoResetEvent _signal = new(initialState: false);
    private readonly SlowCallbackMeter _meter;
    private readonly InputHook _hook;
    private readonly Action<Incident> _recordIncident;
    private readonly Engine _engine = new();
    private readonly RawInputTranslator _translator;
    private readonly Thread _thread;
    private readonly ManualResetEventSlim _started = new();
    private uint _threadId;
    private Exception? _startError;
    private long _heartbeat;

    /// <summary>
    /// Initializes a new instance of the <see cref="InputWorker" /> class.
    /// </summary>
    /// <param name="recordIncident">
    /// The sink that records Incidents; it is called on the worker thread.
    /// </param>
    /// <param name="time">The clock; <see cref="TimeProvider.System" /> outside tests.</param>
    public InputWorker(Action<Incident> recordIncident, TimeProvider time) {
      _recordIncident = recordIncident;
      _meter = new SlowCallbackMeter(time);
      _hook = new InputHook(_ring, _signal, _meter, time);
      bool capsLockOn = (PInvoke.GetKeyState((int)VIRTUAL_KEY.VK_CAPITAL) & 1) != 0;
      _translator = new RawInputTranslator(capsLockOn);
      _thread = new Thread(Run) { Name = "Rekey worker", IsBackground = true };
    }

    /// <summary>
    /// Occurs on the worker thread after the engine handled an input event.
    /// </summary>
    /// <remarks>
    /// A <see cref="KeyDown" /> prints without its Keystroke, so the text form is safe to log.
    /// </remarks>
    public event Action<InputEvent>? Observed;

    /// <summary>
    /// Gets the number of input batches the worker finished; only the worker thread writes it.
    /// </summary>
    public long Heartbeat => Volatile.Read(ref _heartbeat);

    /// <summary>
    /// Starts the hook and worker threads and returns once both run.
    /// </summary>
    /// <exception cref="System.ComponentModel.Win32Exception">
    /// Windows refused a low-level hook.
    /// </exception>
    public void Start() {
      _thread.Start();
      _started.Wait();
      if (_startError is not null) {
        throw _startError;
      }
    }

    /// <inheritdoc />
    public void Dispose() {
      if (_thread.IsAlive) {
        PInvoke.PostThreadMessage(_threadId, PInvoke.WM_QUIT, default, default);
        _thread.Join();
      }

      _hook.Dispose();
      _signal.Dispose();
      _started.Dispose();
    }

    // Dispatches waiting messages; returns false once WM_QUIT arrives.
    private static bool PumpMessages() {
      while (PInvoke.PeekMessage(
          out MSG message, HWND.Null, 0, 0, PEEK_MESSAGE_REMOVE_TYPE.PM_REMOVE)) {
        if (message.message == PInvoke.WM_QUIT) {
          return false;
        }

        PInvoke.TranslateMessage(message);
        PInvoke.DispatchMessage(message);
      }

      return true;
    }

    [SuppressMessage(
        "Reliability",
        "S3869:SafeHandle.DangerousGetHandle should not be called",
        Justification = "Dispose joins this thread before it disposes the event.")]
    private void Run() {
      _threadId = PInvoke.GetCurrentThreadId();
      PInvoke.PeekMessage(out _, HWND.Null, 0, 0, PEEK_MESSAGE_REMOVE_TYPE.PM_NOREMOVE);
      try {
        _hook.Start();
      } catch (System.ComponentModel.Win32Exception e) {
        _startError = e;
        _started.Set();
        return;
      }

      _started.Set();
      using var focus = new FocusTracker(Send);
      focus.Start();
      HANDLE[] handles = [(HANDLE)_signal.SafeWaitHandle.DangerousGetHandle()];
      do {
        PInvoke.MsgWaitForMultipleObjectsEx(
            handles,
            _pollMilliseconds,
            QUEUE_STATUS_FLAGS.QS_ALLINPUT,
            MSG_WAIT_FOR_MULTIPLE_OBJECTS_EX_FLAGS.MWMO_INPUTAVAILABLE);
        DrainInput();
        Poll();
        Volatile.Write(ref _heartbeat, _heartbeat + 1);
      } while (PumpMessages());
    }

    private void DrainInput() {
      while (_ring.TryRead(out RawInput raw)) {
        InputEvent? inputEvent = _translator.Translate(raw);
        if (inputEvent is not null) {
          Send(inputEvent);
        }
      }
    }

    private void Poll() {
      if (_meter.TakeIncident() is { } incident) {
        _recordIncident(incident);
      }
    }

    private void Send(InputEvent inputEvent) {
      foreach (EngineAction action in _engine.Handle(inputEvent)) {
        Execute(action);
      }

      Observed?.Invoke(inputEvent);
    }

    private void Execute(EngineAction action) {
      if (action is RecordIncident record) {
        _recordIncident(record.Incident);
      }
    }
  }
}
