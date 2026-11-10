using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace Rekey.Win32.Keyboard {
  /// <summary>
  /// Resyncs the translator's held modifiers and Caps Lock with the system when the hook may
  /// have missed key-ups.
  /// </summary>
  /// <remarks>
  /// Not thread-safe: the worker thread owns it.
  /// </remarks>
  internal sealed class ModifierSync {
    private readonly RawInputTranslator _translator;
    private readonly Func<long> _reattachments;
    private readonly Func<long> _dropped;
    private readonly Func<long> _written;
    private readonly Func<VIRTUAL_KEY, bool> _isDown;
    private readonly Func<bool> _capsLockOn;
    private readonly CountFollower _reattached = new();
    private readonly CountFollower _droppedSeen = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ModifierSync" /> class.
    /// </summary>
    /// <param name="translator">The translator to resync.</param>
    /// <param name="reattachments">Reads how often the hooks were installed again.</param>
    /// <param name="dropped">Reads how many inputs the ring dropped because it was full.</param>
    /// <param name="written">Reads how many inputs the hook has written to the ring.</param>
    /// <param name="isDown">Tells whether a key is down now.</param>
    /// <param name="capsLockOn">Tells whether Caps Lock is on now.</param>
    public ModifierSync(
        RawInputTranslator translator,
        Func<long> reattachments,
        Func<long> dropped,
        Func<long> written,
        Func<VIRTUAL_KEY, bool> isDown,
        Func<bool> capsLockOn) {
      _translator = translator;
      _reattachments = reattachments;
      _dropped = dropped;
      _written = written;
      _isDown = isDown;
      _capsLockOn = capsLockOn;
    }

    /// <summary>
    /// Samples the system's held keys now; inputs written before the sample no longer change
    /// the held keys when they are read.
    /// </summary>
    public void Resync() {
      // Count first: what was written before the count already shows in the sample.
      long writtenBefore = _written();
      _translator.Resync(_isDown, _capsLockOn(), writtenBefore);
    }

    /// <summary>
    /// Resyncs when key-ups may have been missed since the last look: the hooks were installed
    /// again, or the full ring dropped input. Call it after reading a key and before translating
    /// it, so a reinstall or drop that happened just before the read counts.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> if the ring dropped input, so the current Word may miss
    /// Keystrokes; otherwise, <see langword="false" />.
    /// </returns>
    public bool CatchUp() {
      bool reattached = _reattached.Changed(_reattachments());
      bool dropped = _droppedSeen.Changed(_dropped());
      if (reattached || dropped) {
        Resync();
      }

      return dropped;
    }
  }
}
