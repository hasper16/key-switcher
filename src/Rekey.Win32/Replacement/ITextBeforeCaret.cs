namespace Rekey.Win32.Replacement {
  /// <summary>
  /// Reads and selects the text just before the caret of the focused control.
  /// </summary>
  internal interface ITextBeforeCaret {
    /// <summary>
    /// Gets a value indicating whether the latest <see cref="Read" /> ran out of time.
    /// </summary>
    bool TimedOut { get; }

    /// <summary>
    /// Reads the characters just before the caret.
    /// </summary>
    /// <param name="length">The number of characters to read.</param>
    /// <returns>
    /// Up to <paramref name="length" /> characters, or <see langword="null" /> when the control
    /// does not expose them in time.
    /// </returns>
    string? Read(int length);

    /// <summary>
    /// Selects the characters the latest <see cref="Read" /> returned, so typing replaces them.
    /// </summary>
    /// <returns><see langword="true" /> if they are selected.</returns>
    bool SelectRead();
  }
}
