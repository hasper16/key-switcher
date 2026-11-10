namespace Rekey.Win32.Keyboard {
  /// <summary>
  /// Tells the reader of a counter that another thread increments whether it moved since the
  /// last look.
  /// </summary>
  /// <remarks>
  /// The reader thread owns it; the counter keeps its single writer.
  /// </remarks>
  internal sealed class CountFollower {
    private long _seen;

    /// <summary>
    /// Returns whether the count differs from the one seen last time, and remembers it.
    /// </summary>
    public bool Changed(long count) {
      bool changed = count != _seen;
      _seen = count;
      return changed;
    }
  }
}
