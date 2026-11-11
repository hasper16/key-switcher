using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Rekey.Core;
using Rekey.Core.Detection;
using Rekey.Core.Languages;
using Rekey.Win32.Focus;
using Rekey.Win32.Keyboard;
using Rekey.Win32.Layouts;
using Rekey.Win32.Replacement;
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

    // Design D1 of add-text-replacement: a replacement starts only while the key that asked for
    // it is this fresh, so with UI Automation's 50 ms budget it lands before the hook's 150 ms
    // valve could have let later keys through.
    private const long _freshMilliseconds = 50;

    // Checked again just before the input is sent, after UI Automation: 50 ms below the valve.
    private const long _landMilliseconds = 100;

    // Design D5: Windows does not tell other processes about added Layouts, so poll.
    private static readonly TimeSpan _layoutPollPeriod = TimeSpan.FromSeconds(2);

    // How long quitting waits for the worker thread; see Dispose.
    private static readonly TimeSpan _stopWait = TimeSpan.FromSeconds(2);

    // Design D1 of add-text-replacement: the Trigger Keys held while a Word Evaluator is set.
    private static readonly ushort[] _triggerKeys = [
        (ushort)VIRTUAL_KEY.VK_SPACE, (ushort)VIRTUAL_KEY.VK_RETURN];

    private static readonly VIRTUAL_KEY[] _shortcutModifiers = [
        VIRTUAL_KEY.VK_LCONTROL, VIRTUAL_KEY.VK_RCONTROL, VIRTUAL_KEY.VK_LMENU,
        VIRTUAL_KEY.VK_RMENU, VIRTUAL_KEY.VK_LWIN, VIRTUAL_KEY.VK_RWIN];

    private readonly InputRing _ring = new(_ringCapacity);
    private readonly HoldGate _gate = new();
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
    private readonly ModifierSync _modifierSync;
    private readonly KeyConfirmer _keyConfirmer;
    private readonly WordEvaluator? _evaluate;
    private readonly bool _verifiedReplacement;
    private readonly Thread _thread;
    private readonly ManualResetEventSlim _started = new();
    private Engine _engine = new();
    private uint _threadId;
    private Exception? _startError;
    private long _heartbeat;
    private long _lastLayoutPoll;
    private bool _quitting;
    private FocusTracker? _focus;
    private TextReplacer? _replacer;
    private long _seenHoldTimeouts;

    // The held key the worker is handling, and whether a replacement it asked for was applied.
    private RawInput? _held;
    private bool _heldReplaced;

    /// <summary>
    /// Initializes a new instance of the <see cref="InputWorker" /> class.
    /// </summary>
    /// <param name="settings">The settings that turn the keyboard watchdog on or off.</param>
    /// <param name="recordIncident">
    /// The sink that records Incidents; it is called on the worker thread.
    /// </param>
    /// <param name="time">The clock; <see cref="TimeProvider.System" /> outside tests.</param>
    public InputWorker(Settings settings, Action<Incident> recordIncident, TimeProvider time)
        : this(settings, recordIncident, time, null) {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InputWorker" /> class that corrects Words.
    /// </summary>
    /// <param name="settings">
    /// The settings that turn the keyboard watchdog and Verified Replacement on or off.
    /// </param>
    /// <param name="recordIncident">
    /// The sink that records Incidents; it is called on the worker thread.
    /// </param>
    /// <param name="time">The clock; <see cref="TimeProvider.System" /> outside tests.</param>
    /// <param name="evaluate">
    /// The evaluator of Words a Trigger Key ends, called on the worker thread; with
    /// <see langword="null" />, no key is held and nothing is corrected.
    /// </param>
    public InputWorker(
        Settings settings,
        Action<Incident> recordIncident,
        TimeProvider time,
        WordEvaluator? evaluate) {
      _recordIncident = recordIncident;
      _time = time;
      _evaluate = evaluate;
      _verifiedReplacement = settings.VerifiedReplacement;
      _meter = new SlowCallbackMeter(time);
      _hook = new InputHook(_ring, _gate, _signal, _meter, time);
      bool capsLockOn = (PInvoke.GetKeyState((int)VIRTUAL_KEY.VK_CAPITAL) & 1) != 0;
      _translator = new RawInputTranslator(capsLockOn);
      _modifierSync = new ModifierSync(
          _translator,
          () => _hook.Reattachments,
          () => _ring.Dropped,
          () => _ring.Written,
          IsKeyDown,
          IsCapsLockOn);
      _keyConfirmer = new KeyConfirmer(
          () => _focus?.IsPasswordFocused() ?? true, () => _ring.ActionsWritten);
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
    /// Occurs on the worker thread after Rekey replaced text or cancelled a replacement, with
    /// how it went; it carries no text.
    /// </summary>
    public event Action<ReplacementResult>? Replaced;

    /// <summary>
    /// Occurs on the worker thread after an Undo, for the features that learn from it.
    /// </summary>
    /// <remarks>
    /// It carries typed text: never log it.
    /// </remarks>
    public event Action<UndoPerformed>? Undone;

    /// <summary>
    /// Gets the number of Keystrokes in the engine's current Word; read it only on the worker
    /// thread, as from <see cref="Observed" />.
    /// </summary>
    /// <remarks>
    /// A count only, for checking by hand that a Password Field leaves the Word empty.
    /// </remarks>
    public int WordLength => _engine.Word.Count;

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

#if DEBUG
    /// <summary>
    /// Blocks the worker thread for a while, to check the freeze guard; safe to call from any
    /// thread. Debug builds only.
    /// </summary>
    /// <param name="length">How long the worker stays blocked.</param>
    public void FreezeForCheck(TimeSpan length) {
      PInvoke.PostThreadMessage(
          _threadId, _freezeMessage, default, (nint)length.TotalMilliseconds);
    }

    /// <summary>
    /// Makes the next keyboard hook callback wait past Windows' limit, so Windows removes the
    /// hook; safe to call from any thread. Debug builds only.
    /// </summary>
    /// <param name="length">How long the callback waits.</param>
    public void StallHookForCheck(TimeSpan length) {
      _hook.StallNextKeyForCheck(length);
    }
#endif

    /// <inheritdoc />
    public void Dispose() {
      // Bounded, so quitting Rekey always works: the thread is a background thread and ends with
      // the process if it is stuck.
      bool stopped = true;
      if (_thread.IsAlive) {
        PInvoke.PostThreadMessage(_threadId, PInvoke.WM_QUIT, default, default);
        stopped = _thread.Join(_stopWait);
      }

      _hook.Dispose();
      if (stopped) {
        _signal.Dispose();
        _started.Dispose();
      }
    }

    // Hook timestamps count the same milliseconds since system start.
    private static TimeSpan Now() {
      return TimeSpan.FromMilliseconds(Environment.TickCount64);
    }

    // The worker never receives keyboard input itself, so its key state follows the system's
    // and GetKeyState reports the session's Caps Lock (checked on Windows 11).
    private static bool IsCapsLockOn() {
      return (PInvoke.GetKeyState((int)VIRTUAL_KEY.VK_CAPITAL) & 1) != 0;
    }

    private static bool IsKeyDown(VIRTUAL_KEY key) {
      return PInvoke.GetAsyncKeyState((int)key) < 0;
    }

    private static bool IsInputMethod(LayoutId layout) {
      return layout.Value >> 28 == 0xE;
    }

    private static IReadOnlyList<VIRTUAL_KEY> HeldShortcutModifiers() {
      return [.. _shortcutModifiers.Where(IsKeyDown)];
    }

    // Returns whether Windows took every input; it refuses input to a window of higher
    // integrity.
    private static bool SendKeys(INPUT[] inputs) {
      return PInvoke.SendInput(inputs, Marshal.SizeOf<INPUT>()) == inputs.Length;
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
      _replacer = new TextReplacer(
          new UiaTextBeforeCaret(), _verifiedReplacement, HeldShortcutModifiers, SendKeys);
      RefreshLayouts();
      using var focus = new FocusTracker(OnFocusChanged);
      _focus = focus;
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
      } while (!_quitting && PumpMessages());
    }

    private void LoadLanguages() {
      LanguageSetLoad load = LanguageSet.LoadBundled(_time.GetUtcNow());
      foreach (Incident incident in load.Incidents) {
        _recordIncident(incident);
      }

      _engine = new Engine(load.Languages, _evaluate);
    }

    private void DrainInput() {
      bool any = false;

      // Focus callbacks run while messages are dispatched, so dispatching before each read
      // applies a focus change, such as into a Password Field, before the keys typed after it.
      while (PumpOrQuit() && _ring.TryRead(out RawInput raw)) {
        any = true;

        // After the read, so a reinstall or drop just before it still counts for this key.
        if (_modifierSync.CatchUp()) {
          Send(new InputLost(TimeSpan.FromMilliseconds(raw.Tick)));
        }

        // WinEvents arrive asynchronously: a key typed into a new window can come first.
        if (raw.Kind == RawInputKind.KeyDown) {
          _focus?.CatchUp(TimeSpan.FromMilliseconds(raw.Tick));
        }

        InputEvent? inputEvent = _translator.Translate(raw);
        if (inputEvent is KeyDown { Kind: KeyKind.Character } keyDown) {
          // Focus may have moved inside the window before its WinEvent arrived.
          inputEvent = keyDown with { FocusConfirmed = _keyConfirmer.Confirm(raw) };
        }

        _held = raw.Holding == Holding.None ? null : raw;
        _heldReplaced = false;
        if (inputEvent is not null) {
          Send(inputEvent);
        }

        if (raw.Holding != Holding.None) {
          FinishHeld(raw);
        }

        // Progress per input, so a pass that stays busy is not taken for a freeze.
        Volatile.Write(ref _heartbeat, _heartbeat + 1);
      }

      // The user may have switched Layout with the keys just read.
      if (any) {
        FollowForegroundLayout();
      }
    }

    // Sends a held key on to the application once its replacement, if any, is done, and lets
    // the hook stop holding once every held key has gone on. An applied Undo consumes its
    // Ctrl+Z; an applied Correction makes a Ctrl+Z right after its Trigger Key an Undo.
    private void FinishHeld(in RawInput raw) {
      if (raw.Holding == Holding.Held && _heldReplaced) {
        _gate.ArmUndo(raw.Sequence);
      }

      // Released even if Windows refuses the key: holding on would only block typing until
      // the valve opens, and the key cannot be sent any other way.
      if (raw.Holding != Holding.UndoRequest || !_heldReplaced) {
        _ = SendKeys([InputBuilder.Reinject(raw)]);
      }

      _gate.Release(raw.Sequence);
      _held = null;
    }

    private void Poll() {
      if (_meter.TakeIncident() is { } slow) {
        _recordIncident(slow);
      }

      long holdTimeouts = _gate.Timeouts;
      if (holdTimeouts != _seenHoldTimeouts) {
        _seenHoldTimeouts = holdTimeouts;
        _recordIncident(new Incident(_time.GetUtcNow(), IncidentType.HoldTimeout, []));
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

      // Windows hides input aimed at an elevated window or the secure desktop from the hooks.
      bool inputHidden = (_engine.Focus?.IsElevated ?? false) || InputDesktop.HidesInputNow();
      Apply(_hookWatchdog.Check(inputHidden), pausedBefore);
    }

    private void Apply(WatchdogOutcome outcome, DateTimeOffset? pausedBefore) {
      foreach (Incident incident in outcome.Incidents) {
        _recordIncident(incident);
      }

      if (outcome.Reinstall) {
        _hook.Reinstall();
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

      List<KeyMap> keyMaps = [.. _layouts.Layouts.Select(KeyMapOf).OfType<KeyMap>()];
      _translator.UseKeyMaps(keyMaps);
      _gate.UsePolicy(_evaluate is null
          ? HookPolicy.HoldNothing
          : new HookPolicy(_triggerKeys, RawInputTranslator.CharacterScanCodes(keyMaps)));
      Send(new LayoutsInstalledChanged(Now(), _layouts.Layouts, keyMaps));
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

    private bool PumpOrQuit() {
      _quitting = _quitting || !PumpMessages();
      return !_quitting;
    }

    private void OnFocusChanged(InputEvent focus) {
      // Key-ups on the secure desktop or in an elevated window never reach the hook.
      _modifierSync.Resync();
      Send(focus);
      FollowForegroundLayout();
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
        case ReplaceText replace:
          Replace(replace);
          break;
        case UndoPerformed undone:
          Undone?.Invoke(undone);
          break;
        default:
          break;
      }
    }

    private void Replace(ReplaceText replace) {
      ReplacementResult result = TryReplace(replace);
      _heldReplaced = result is ReplacementResult.Verified or ReplacementResult.Retyped;
      if (result == ReplacementResult.Mismatch) {
        _recordIncident(new Incident(_time.GetUtcNow(), IncidentType.CorrectionCancelled, []));
      }

      Replaced?.Invoke(result);
      Send(new ReplacementFinished(Now(), _heldReplaced));
    }

    private ReplacementResult TryReplace(ReplaceText replace) {
      if (_held is not { } held || _replacer is null || _engine.Focus?.AppId is not { } app) {
        return ReplacementResult.TooLate;
      }

      if (Stale(held, _freshMilliseconds) is { } stale) {
        return stale;
      }

      return _replacer.Replace(replace, app, () => Stale(held, _landMilliseconds));
    }

    // Why the held key can no longer be replaced before, or null if it still can: the valve
    // may have let later keys through, or a click, which is never held, or the app itself may
    // have moved focus since the key was read.
    private ReplacementResult? Stale(in RawInput held, long maxAge) {
      if (Environment.TickCount64 - held.Tick >= maxAge) {
        return ReplacementResult.TooLate;
      }

      bool moved = _gate.ClickedAfter(held.Sequence) || (_focus?.WindowChanged() ?? true);
      return moved ? ReplacementResult.Interrupted : null;
    }
  }
}
