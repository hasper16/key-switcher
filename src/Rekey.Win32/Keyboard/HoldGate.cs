using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace Rekey.Win32.Keyboard {
  /// <summary>
  /// Decides on the hook thread which key events to hold back while the worker may replace the
  /// text before them (design D1 and D4).
  /// </summary>
  /// <remarks>
  /// <see cref="Judge" /> runs on the hook thread only and owns every field but three: the
  /// worker alone writes the released sequence, the armed Undo and the policy. Nothing
  /// allocates or locks.
  /// </remarks>
  internal sealed class HoldGate {
    // Design D1: typing is never held back longer than this.
    private const long _valveMilliseconds = 150;
    private const long _nothing = -1;

    // Written by the worker only.
    private long _released = _nothing;
    private long _undoArmed = _nothing;
    private HookPolicy _policy = HookPolicy.HoldNothing;
    private HotkeyDetector? _hotkey;

    // Written by the hook thread only.
    private long _timeouts;
    private long _lastClick = _nothing;
    private long _lastHeld = _nothing;
    private long _valveOpenFor = _nothing;
    private long _holdStart;
    private long _lastKeyDown = _nothing;
    private bool _lastWasCharacter;
    private Modifier _modifiers;

    [Flags]
    private enum Modifier : byte {
      None = 0,
      ControlLeft = 1,
      ControlRight = 2,
      AltLeft = 4,
      AltRight = 8,
      WindowsLeft = 16,
      WindowsRight = 32,
      Shift = 64,
      Control = ControlLeft | ControlRight,
      Alt = AltLeft | AltRight,
      Windows = WindowsLeft | WindowsRight,
    }

    /// <summary>
    /// Gets the number of times the hook stopped holding because the worker took too long.
    /// </summary>
    public long Timeouts => Volatile.Read(ref _timeouts);

    /// <summary>
    /// Records a mouse click at a ring sequence; called by the hook thread only.
    /// </summary>
    /// <remarks>
    /// Clicks are not held, so a click during a hold reaches the application before the held
    /// keys and may move the caret.
    /// </remarks>
    public void NoteClick(long sequence) {
      Volatile.Write(ref _lastClick, sequence);
    }

    /// <summary>
    /// Returns whether a mouse click came after the key at a ring sequence.
    /// </summary>
    public bool ClickedAfter(long sequence) {
      return Volatile.Read(ref _lastClick) > sequence;
    }

    /// <summary>
    /// Replaces the policy; called by the worker only.
    /// </summary>
    public void UsePolicy(HookPolicy policy) {
      Volatile.Write(ref _policy, policy);
    }

    /// <summary>
    /// Makes the hook recognise the Hotkey with the detector, or no Hotkey.
    /// </summary>
    /// <param name="detector">
    /// A detector built for the hook thread alone, or <see langword="null" />.
    /// </param>
    public void UseHotkey(HotkeyDetector? detector) {
      Volatile.Write(ref _hotkey, detector);
    }

    /// <summary>
    /// Records that every held key up to a ring sequence reached the application; called by
    /// the worker only, in ring order.
    /// </summary>
    public void Release(long sequence) {
      Volatile.Write(ref _released, sequence);
    }

    /// <summary>
    /// Makes a Ctrl+Z right after the Trigger Key at a ring sequence a possible Undo; called by
    /// the worker only.
    /// </summary>
    public void ArmUndo(long triggerSequence) {
      Volatile.Write(ref _undoArmed, triggerSequence);
    }

    /// <summary>
    /// Decides whether to hold one key event back; called by the hook thread only.
    /// </summary>
    /// <param name="kind">A key-down or key-up.</param>
    /// <param name="virtualKey">The virtual-key code.</param>
    /// <param name="scanCode">The scan code.</param>
    /// <param name="extended">Whether the key is an extended key.</param>
    /// <param name="sequence">The ring sequence the event gets.</param>
    /// <param name="tick">The milliseconds since system start.</param>
    /// <param name="hasRoom">Whether the ring can take the event.</param>
    /// <returns>Whether to hold the event, and as what.</returns>
    public Holding Judge(
        RawInputKind kind,
        ushort virtualKey,
        ushort scanCode,
        bool extended,
        long sequence,
        long tick,
        bool hasRoom) {
      bool down = kind == RawInputKind.KeyDown;
      Modifier modifier = ModifierOf(virtualKey);
      HotkeyVerdict hotkey = Volatile.Read(ref _hotkey)?.Observe(virtualKey, down, tick, hasRoom)
          ?? HotkeyVerdict.None;
      Holding holding = hotkey switch {
        HotkeyVerdict.Fire => Holding.Hotkey,
        HotkeyVerdict.Tap => Holding.HotkeyTap,
        HotkeyVerdict.Swallow => Holding.Swallowed,
        _ => Decide(down, virtualKey, tick, hasRoom),
      };
      if (holding is not (Holding.None or Holding.Swallowed)) {
        if (_lastHeld <= Volatile.Read(ref _released)) {
          _holdStart = tick;
        }

        _lastHeld = sequence;
      }

      if (modifier != Modifier.None) {
        _modifiers = down ? _modifiers | modifier : _modifiers & ~modifier;
      }

      // A double tap's release asked for the Manual Correction, so Ctrl+Z right after it may
      // undo that Correction.
      if (holding == Holding.HotkeyTap) {
        _lastKeyDown = sequence;
      } else if (modifier != Modifier.None) {
        // Modifiers neither type nor break the run of an Undo.
      } else if (down) {
        _lastKeyDown = sequence;
        _lastWasCharacter = !HoldsShortcut()
            && Volatile.Read(ref _policy).IsCharacter(scanCode, extended);
      } else {
        // Key-ups of other keys change nothing.
      }

      return holding;
    }

    private static Modifier ModifierOf(ushort virtualKey) {
      return (VIRTUAL_KEY)virtualKey switch {
        VIRTUAL_KEY.VK_LCONTROL => Modifier.ControlLeft,
        VIRTUAL_KEY.VK_RCONTROL => Modifier.ControlRight,
        VIRTUAL_KEY.VK_LMENU => Modifier.AltLeft,
        VIRTUAL_KEY.VK_RMENU => Modifier.AltRight,
        VIRTUAL_KEY.VK_LWIN => Modifier.WindowsLeft,
        VIRTUAL_KEY.VK_RWIN => Modifier.WindowsRight,
        VIRTUAL_KEY.VK_LSHIFT or VIRTUAL_KEY.VK_RSHIFT => Modifier.Shift,
        _ => Modifier.None,
      };
    }

    private Holding Decide(bool down, ushort virtualKey, long tick, bool hasRoom) {
      if (_lastHeld > Volatile.Read(ref _released)) {
        return ContinueHold(tick, hasRoom);
      }

      if (!down || !hasRoom) {
        return Holding.None;
      }

      if (IsUndoRequest(virtualKey)) {
        return Holding.UndoRequest;
      }

      bool trigger = _lastWasCharacter
          && !HoldsShortcut()
          && Volatile.Read(ref _policy).IsTrigger(virtualKey);
      return trigger ? Holding.Held : Holding.None;
    }

    // While held keys wait for the worker, every key waits behind them, so none overtakes
    // another; the valve lets typing through once the worker is too slow.
    private Holding ContinueHold(long tick, bool hasRoom) {
      if (_valveOpenFor == _lastHeld) {
        return Holding.None;
      }

      if (tick - _holdStart > _valveMilliseconds) {
        _valveOpenFor = _lastHeld;
        Volatile.Write(ref _timeouts, _timeouts + 1);
        return Holding.None;
      }

      return hasRoom ? Holding.Held : Holding.None;
    }

    private bool IsUndoRequest(ushort virtualKey) {
      long armed = Volatile.Read(ref _undoArmed);
      return virtualKey == (ushort)VIRTUAL_KEY.VK_Z
          && (_modifiers & Modifier.Control) != 0
          && (_modifiers & (Modifier.Alt | Modifier.Windows)) == 0
          && armed != _nothing
          && _lastKeyDown == armed;
    }

    private bool HoldsShortcut() {
      return (_modifiers & (Modifier.Control | Modifier.Alt | Modifier.Windows)) != 0;
    }
  }
}
