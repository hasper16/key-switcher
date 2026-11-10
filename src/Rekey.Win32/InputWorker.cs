using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Rekey.Core;
using Rekey.Core.Detection;
using Rekey.Core.Languages;
using Rekey.Win32.Focus;
using Rekey.Win32.Keyboard;
using Rekey.Win32.Layouts;
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
  /// thread waits on the hook's signal and on its own message queue at once, and runs its timed
  /// checks between waits.
  /// </remarks>
  public sealed class InputWorker : IDisposable, IWorkerPulse {
    private const int _ringCapacity = 4096;
    private const uint _switchMessage = PInvoke.WM_APP + 1;
    private const uint _resumeMessage = PInvoke.WM_APP + 2;
    private const uint _freezeMessage = PInvoke.WM_APP + 3;
    private const uint _idleWaitMilliseconds = 250;

    // Design D6: a pending Layout Switch is checked every 25 ms.
    private const uint _switchWaitMilliseconds = 25;

    // Design D5: Windows does not tell other processes about added Layouts, so poll.
    private static readonly TimeSpan _layoutPollPeriod = TimeSpan.FromSeconds(2);

    private readonly InputRing _ring = new(_ringCapacity);
    private readonly AutoResetEvent _signal = new(initialState: false);
    private readonly SlowCallbackMeter _meter;
    private readonly InputHook _hook;
    private readonly Action<Incident> _recordIncident;
    private readonly TimeProvider _time;
    private readonly RawInputTranslator _translator;
    private readonly LayoutList _layouts = new(SystemLayouts.ReadHandles, SystemLayouts.Read);
    private readonly Dictionary<LayoutId, KeyMap?> _keyMaps = [];
    private readonly LayoutSwitcher _switcher;
    private readonly ForegroundLayout _foregroundLayout;
    private readonly HookWatchdog _hookWatchdog;
    private readonly Thread _thread;
    private readonly ManualResetEventSlim _started = new();
    private Engine _engine = new();
    private uint _threadId;
    private Exception? _startError;
    private long _heartbeat;
    private long _lastLayoutPoll;

    /// <summary>
    /// Initializes a new instance of the <see cref="InputWorker" /> class.
    /// </summary>
    /// <param name="settings">The settings that turn the keyboard watchdog on or off.</param>
    /// <param name="recordIncident">
    /// The sink that records Incidents; it is called on the worker thread.
    /// </param>
    /// <param name="time">The clock; <see cref="TimeProvider.System" /> outside tests.</param>
    public InputWorker(Settings settings, Action<Incident> recordIncident, TimeProvider time) {
      _recordIncident = recordIncident;
      _time = time;
      _meter = new SlowCallbackMeter(time);
      _hook = new InputHook(_ring, _signal, _meter, time);
      bool capsLockOn = (PInvoke.GetKeyState((int)VIRTUAL_KEY.VK_CAPITAL) & 1) != 0;
      _translator = new RawInputTranslator(capsLockOn);
      _foregroundLayout = new ForegroundLayout(_layouts);
      _switcher = new LayoutSwitcher(_foregroundLayout, time);
      _hookWatchdog = new HookWatchdog(new SessionInputActivity(_hook), time) {
        Enabled = settings.KeyboardWatchdogEnabled,
      };
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
    /// Occurs on the worker thread when Rekey paused itself after repeated keyboard failures,
    /// with the time the pause ends, and when the pause ends, with <see langword="null" />.
    /// </summary>
    public event Action<DateTimeOffset?>? SelfPauseChanged;

    /// <summary>
    /// Gets the number of input batches the worker finished; only the worker thread writes it.
    /// </summary>
    public long Heartbeat => Volatile.Read(ref _heartbeat);

    /// <inheritdoc />
    int IWorkerPulse.QueuedInput => _ring.Count;

    /// <summary>
    /// Starts the hook and worker threads and returns once both run.
    /// </summary>
    /// <exception cref="Win32Exception">Windows refused a low-level hook.</exception>
    public void Start() {
      _thread.Start();
      _started.Wait();
      if (_startError is not null) {
        throw _startError;
      }
    }

    /// <summary>
    /// Asks the worker to switch the foreground window to a Layout and confirm the switch; safe
    /// to call from any thread.
    /// </summary>
    /// <param name="target">The Layout to switch to.</param>
    public void SwitchLayout(LayoutId target) {
      PInvoke.PostThreadMessage(_threadId, _switchMessage, default, (nint)target.Value);
    }

    /// <summary>
    /// Ends a self-pause and reattaches to the keyboard at once; safe to call from any thread.
    /// </summary>
    public void ResumeSelfPause() {
      PInvoke.PostThreadMessage(_threadId, _resumeMessage, default, default);
    }

    /// <summary>
    /// Blocks the worker thread for a while, to check the freeze guard by hand; safe to call from
    /// any thread.
    /// </summary>
    /// <param name="length">How long the worker stays blocked.</param>
    public void FreezeForCheck(TimeSpan length) {
      PInvoke.PostThreadMessage(
          _threadId, _freezeMessage, default, (nint)length.TotalMilliseconds);
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

    // Hook timestamps count the same milliseconds since system start.
    private static TimeSpan Now() {
      return TimeSpan.FromMilliseconds(Environment.TickCount64);
    }

    private static bool IsKeyDown(VIRTUAL_KEY key) {
      return PInvoke.GetAsyncKeyState((int)key) < 0;
    }

    private static bool IsInputMethod(LayoutId layout) {
      return layout.Value >> 28 == 0xE;
    }

    [SuppressMessage(
        "Reliability",
        "S3869:SafeHandle.DangerousGetHandle should not be called",
        Justification = "Dispose joins this thread before it disposes the event.")]
    private void Run() {
      _threadId = PInvoke.GetCurrentThreadId();
      PInvoke.PeekMessage(out _, HWND.Null, 0, 0, PEEK_MESSAGE_REMOVE_TYPE.PM_NOREMOVE);
      LoadLanguages();
      try {
        _hook.Start();
      } catch (Win32Exception e) {
        _startError = e;
        _started.Set();
        return;
      }

      _started.Set();
      RefreshLayouts();
      using var focus = new FocusTracker(OnFocusChanged);
      focus.Start();
      HANDLE[] handles = [(HANDLE)_signal.SafeWaitHandle.DangerousGetHandle()];
      do {
        PInvoke.MsgWaitForMultipleObjectsEx(
            handles,
            _switcher.IsPending ? _switchWaitMilliseconds : _idleWaitMilliseconds,
            QUEUE_STATUS_FLAGS.QS_ALLINPUT,
            MSG_WAIT_FOR_MULTIPLE_OBJECTS_EX_FLAGS.MWMO_INPUTAVAILABLE);
        DrainInput();
        Poll();
        Volatile.Write(ref _heartbeat, _heartbeat + 1);
      } while (PumpMessages());
    }

    private void LoadLanguages() {
      LanguageSetLoad load = LanguageSet.LoadBundled(_time.GetUtcNow());
      foreach (Incident incident in load.Incidents) {
        _recordIncident(incident);
      }

      _engine = new Engine(load.Languages);
    }

    private void DrainInput() {
      bool any = false;
      while (_ring.TryRead(out RawInput raw)) {
        any = true;
        if (_translator.Translate(raw) is { } inputEvent) {
          Send(inputEvent);
        }
      }

      // The user may have switched Layout with the keys just read.
      if (any) {
        FollowForegroundLayout();
      }
    }

    private void Poll() {
      if (_meter.TakeIncident() is { } slow) {
        _recordIncident(slow);
      }

      if (_switcher.Check() is { } result) {
        if (result.Incident is { } incident) {
          _recordIncident(incident);
        }

        if (result.Adopted is { } adopted) {
          Send(new LayoutChanged(Now(), adopted));
        }
      }

      if (_time.GetElapsedTime(_lastLayoutPoll) >= _layoutPollPeriod) {
        RefreshLayouts();
      }

      // A switch by Win+Space or the language bar lands after the input that asked for it.
      FollowForegroundLayout();

      DateTimeOffset? pausedBefore = _hookWatchdog.SelfPausedUntil;
      Apply(_hookWatchdog.Check(_engine.Focus?.IsElevated ?? false), pausedBefore);
    }

    private void Apply(WatchdogOutcome outcome, DateTimeOffset? pausedBefore) {
      foreach (Incident incident in outcome.Incidents) {
        _recordIncident(incident);
      }

      if (outcome.Reinstall) {
        _hook.Reinstall();

        // Key-ups typed while the hook was gone never reached it.
        ResyncModifiers();
      }

      if (_hookWatchdog.SelfPausedUntil != pausedBefore) {
        SelfPauseChanged?.Invoke(_hookWatchdog.SelfPausedUntil);
      }
    }

    private void RefreshLayouts() {
      _lastLayoutPoll = _time.GetTimestamp();
      if (!_layouts.Refresh()) {
        return;
      }

      _translator.UseKeyMaps(_layouts.Layouts.Select(KeyMapOf).OfType<KeyMap>());
      Send(new LayoutsInstalledChanged(Now(), _layouts.Layouts));
      FollowForegroundLayout();
    }

    private KeyMap? KeyMapOf(LayoutId layout) {
      if (!_keyMaps.TryGetValue(layout, out KeyMap? map)) {
        try {
          map = IsInputMethod(layout) ? null : KeyMapReader.Read(layout);
        } catch (Win32Exception) {
          // A Layout Windows cannot load types nothing Rekey can read back.
          map = null;
        }

        _keyMaps[layout] = map;
      }

      return map;
    }

    private void OnFocusChanged(FocusChanged focus) {
      // Key-ups on the secure desktop or in an elevated window never reach the hook.
      ResyncModifiers();
      Send(focus);
      FollowForegroundLayout();
    }

    private void ResyncModifiers() {
      bool capsLockOn = (PInvoke.GetKeyState((int)VIRTUAL_KEY.VK_CAPITAL) & 1) != 0;
      _translator.Resync(IsKeyDown, capsLockOn);
    }

    private void FollowForegroundLayout() {
      if (_switcher.IsPending) {
        return;
      }

      LayoutId? current = _foregroundLayout.CurrentLayout();
      if (current is { } layout && layout != _engine.CurrentLayout) {
        Send(new LayoutChanged(Now(), layout));
      }
    }

    private void Send(InputEvent inputEvent) {
      foreach (EngineAction action in _engine.Handle(inputEvent)) {
        Execute(action);
      }

      Observed?.Invoke(inputEvent);
    }

    // Dispatches waiting messages; returns false once WM_QUIT arrives.
    private bool PumpMessages() {
      while (PInvoke.PeekMessage(
          out MSG message, HWND.Null, 0, 0, PEEK_MESSAGE_REMOVE_TYPE.PM_REMOVE)) {
        if (message.message == PInvoke.WM_QUIT) {
          return false;
        }

        if (message.hwnd.IsNull && HandleOwnMessage(message)) {
          continue;
        }

        PInvoke.TranslateMessage(message);
        PInvoke.DispatchMessage(message);
      }

      return true;
    }

    private bool HandleOwnMessage(in MSG message) {
      switch (message.message) {
        case _switchMessage:
          Execute(new SwitchLayout(new LayoutId((uint)message.lParam.Value)));
          return true;
        case _resumeMessage:
          Apply(_hookWatchdog.Resume(), _hookWatchdog.SelfPausedUntil);
          SelfPauseChanged?.Invoke(null);
          return true;
        case _freezeMessage:
          Thread.Sleep(TimeSpan.FromMilliseconds(message.lParam.Value));
          return true;
        default:
          return false;
      }
    }

    private void Execute(EngineAction action) {
      switch (action) {
        case RecordIncident record:
          _recordIncident(record.Incident);
          break;
        case SwitchLayout switchLayout:
          _switcher.Switch(switchLayout.Target);
          break;
        default:
          break;
      }
    }
  }
}
