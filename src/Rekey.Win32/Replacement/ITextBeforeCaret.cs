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
    /// Gets a value indicating whether the latest <see cref="SelectRead" /> asked the app to
    /// select and ran out of time, so the selection may still land.
    /// </summary>
    bool SelectionPending { get; }

    /// <summary>
    /// Returns whether the control the latest <see cref="Read" /> read still has focus.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> if it does, or if nothing was read; <see langword="false" /> if
    /// focus moved or the app cannot tell in time.
    /// </returns>
    bool FocusUnchanged();

    /// <summary>
    /// Collapses the selection asked for by <see cref="SelectRead" /> to its end, in the
    /// control that was read, without sending input; failures are ignored.
    /// </summary>
    /// <param name="when">
    /// One of the enumeration values that specifies whether to check the selection first.
    /// </param>
    void Deselect(Deselection when);

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
