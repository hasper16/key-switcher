namespace Rekey.Win32.Keyboard {
  /// <summary>
  /// Confirms that a character key went to a control that is not a Password Field.
  /// </summary>
  /// <remarks>
  /// A focus move inside one window reaches Rekey only through an asynchronous WinEvent, so the
  /// worker checks the focused control itself for every character key. The answer describes the
  /// focus at the time of the check, which matches the key's only while nothing typed or
  /// clicked after the key could have moved focus: no later input may be queued before or during
  /// the check (spec keyboard-capture, owner decision). Not thread-safe: the worker owns it.
  /// </remarks>
  internal sealed class KeyConfirmer {
    private readonly Func<bool> _isPasswordFocused;
    private readonly Func<long> _written;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyConfirmer" /> class.
    /// </summary>
    /// <param name="isPasswordFocused">
    /// Tells whether the focused control is a Password Field now; an unknown answer is
    /// <see langword="true" />.
    /// </param>
    /// <param name="written">Reads how many inputs the hook has written to the ring.</param>
    public KeyConfirmer(Func<bool> isPasswordFocused, Func<long> written) {
      _isPasswordFocused = isPasswordFocused;
      _written = written;
    }

    /// <summary>
    /// Returns whether a key just read from the ring is confirmed outside a Password Field.
    /// </summary>
    /// <param name="key">The key, with the sequence the ring gave it.</param>
    /// <returns>
    /// <see langword="true" /> if the focused control is not a Password Field and no later input
    /// was written before or during the check; otherwise, <see langword="false" />.
    /// </returns>
    public bool Confirm(in RawInput key) {
      long next = key.Sequence + 1;
      if (_written() != next) {
        return false;
      }

      bool password = _isPasswordFocused();
      return !password && _written() == next;
    }
  }
}
