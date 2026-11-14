namespace Rekey.Core {
  /// <summary>
  /// Recognises a double tap of one modifier: two presses and releases with no other key in
  /// between, the second release within 400 ms of the first.
  /// </summary>
  /// <remarks>
  /// Runs on the keyboard hook thread, so it never allocates. Not thread-safe: one thread feeds
  /// it every key in order.
  /// </remarks>
  public sealed class DoubleTap {
    // Design D1: the window of a double tap; also the longest press that counts as a tap.
    private const long _windowMilliseconds = 400;
    private const long _never = long.MinValue;

    private readonly KeyModifiers _modifier;
    private bool _down;
    private bool _clean;
    private long _pressedAt;
    private long _firstReleasedAt = _never;

    /// <summary>
    /// Initializes a new instance of the <see cref="DoubleTap" /> class.
    /// </summary>
    /// <param name="modifier">The modifier whose double tap to recognise.</param>
    public DoubleTap(KeyModifiers modifier) {
      _modifier = modifier;
    }

    /// <summary>
    /// Observes one key going down or up and returns whether it completes a double tap.
    /// </summary>
    /// <param name="key">
    /// The modifier the key is, or <see cref="KeyModifiers.None" /> for any other key.
    /// </param>
    /// <param name="down">
    /// <see langword="true" /> for a press or its auto-repeat; <see langword="false" /> for a
    /// release.
    /// </param>
    /// <param name="tick">The time of the event in milliseconds.</param>
    /// <returns>
    /// <see langword="true" /> when this release completes a double tap; otherwise,
    /// <see langword="false" />.
    /// </returns>
    public bool Observe(KeyModifiers key, bool down, long tick) {
      if (key != _modifier) {
        if (down) {
          _clean = false;
          _firstReleasedAt = _never;
        }

        return false;
      }

      if (down) {
        if (!_down) {
          (_down, _clean, _pressedAt) = (true, true, tick);
        }

        return false;
      }

      return _down && Release(tick);
    }

    private bool Release(long tick) {
      _down = false;
      if (!_clean || tick - _pressedAt > _windowMilliseconds) {
        _firstReleasedAt = _never;
        return false;
      }

      if (_firstReleasedAt != _never && tick - _firstReleasedAt <= _windowMilliseconds) {
        _firstReleasedAt = _never;
        return true;
      }

      _firstReleasedAt = tick;
      return false;
    }
  }
}
