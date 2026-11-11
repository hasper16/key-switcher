namespace Rekey.Win32.Keyboard {
  /// <summary>
  /// Passes raw input from the hook thread to the worker thread without locks or allocation.
  /// </summary>
  /// <remarks>
  /// Exactly one thread writes and exactly one thread reads. The writer owns the tail and the
  /// drop count; the reader owns the head.
  /// </remarks>
  internal sealed class InputRing {
    private readonly RawInput[] _items;
    private readonly long _mask;
    private long _head;
    private long _tail;
    private long _dropped;
    private long _actions;

    public InputRing(int capacity) {
      if (capacity <= 0 || (capacity & (capacity - 1)) != 0) {
        throw new ArgumentOutOfRangeException(
            nameof(capacity), capacity, "The capacity must be a power of two.");
      }

      _items = new RawInput[capacity];
      _mask = capacity - 1;
    }

    /// <summary>Gets the number of inputs waiting to be read.</summary>
    public int Count => (int)(Volatile.Read(ref _tail) - Volatile.Read(ref _head));

    /// <summary>
    /// Gets the number of inputs written so far, which is also the sequence of the next one.
    /// </summary>
    public long Written => Volatile.Read(ref _tail);

    /// <summary>
    /// Gets the number of key-downs and mouse-button presses written so far: the inputs that can
    /// move focus.
    /// </summary>
    public long ActionsWritten => Volatile.Read(ref _actions);

    /// <summary>Gets the number of inputs dropped because the ring was full.</summary>
    public long Dropped => Volatile.Read(ref _dropped);

    /// <summary>Adds an input; called only by the writer thread.</summary>
    public bool TryWrite(in RawInput input) {
      long tail = _tail;
      if (tail - Volatile.Read(ref _head) == _items.Length) {
        Volatile.Write(ref _dropped, _dropped + 1);
        return false;
      }

      long actions = input.Kind == RawInputKind.KeyUp ? _actions : _actions + 1;
      _items[tail & _mask] = input with { Sequence = tail, Actions = actions };
      Volatile.Write(ref _actions, actions);
      Volatile.Write(ref _tail, tail + 1);
      return true;
    }

    /// <summary>Takes the oldest input; called only by the reader thread.</summary>
    public bool TryRead(out RawInput input) {
      long head = _head;
      if (head == Volatile.Read(ref _tail)) {
        input = default;
        return false;
      }

      input = _items[head & _mask];
      Volatile.Write(ref _head, head + 1);
      return true;
    }
  }
}
