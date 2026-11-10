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
    private readonly Func<long> _written;
    private readonly Func<VIRTUAL_KEY, bool> _isDown;
    private readonly Func<bool> _capsLockOn;
    private readonly CountFollower _followed = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ModifierSync" /> class.
    /// </summary>
    /// <param name="translator">The translator to resync.</param>
    /// <param name="reattachments">Reads how often the hooks were installed again.</param>
    /// <param name="written">Reads how many inputs the hook has written to the ring.</param>
    /// <param name="isDown">Tells whether a key is down now.</param>
    /// <param name="capsLockOn">Tells whether Caps Lock is on now.</param>
    public ModifierSync(
        RawInputTranslator translator,
        Func<long> reattachments,
        Func<long> written,
        Func<VIRTUAL_KEY, bool> isDown,
        Func<bool> capsLockOn) {
      _translator = translator;
      _reattachments = reattachments;
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
    /// Resyncs when the hooks were installed again since the last look. Call it after reading a
    /// key and before translating it, so a reinstall that finished just before the read counts.
    /// </summary>
    public void FollowReattachments() {
      if (_followed.Changed(_reattachments())) {
        Resync();
      }
    }
  }
}
