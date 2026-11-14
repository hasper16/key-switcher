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
    /// <see langword="true" /> if it does, or if UI Automation is unavailable;
    /// <see langword="false" /> if focus moved, or if the control read cannot be confirmed in
    /// time.
    /// </returns>
    bool FocusUnchanged();

    /// <summary>
    /// Returns whether the focused control's selection is still the given text.
    /// </summary>
    /// <param name="selected">The selected text read before.</param>
    /// <returns>
    /// <see langword="true" /> if it is, or if UI Automation is unavailable;
    /// <see langword="false" /> if the selection changed or cannot be read in time.
    /// </returns>
    bool SelectionIs(string selected);

    /// <summary>
    /// Collapses the selection asked for by <see cref="SelectRead" /> to its end, in the
    /// control that was read, without sending input; failures are ignored.
    /// </summary>
    /// <param name="when">
    /// One of the enumeration values that specifies whether to check the selection first.
    /// </param>
    void Deselect(Deselection when);

    /// <summary>
    /// Describes how the latest <see cref="Read" /> compared with the expected text and which
    /// control it read, without either text.
    /// </summary>
    /// <param name="expected">The text Rekey expected before the caret.</param>
    /// <returns>The comparison.</returns>
    ReplacementTrace Describe(string expected);

    /// <summary>
    /// Reads the characters just before the caret.
    /// </summary>
    /// <param name="length">The number of characters to read.</param>
    /// <returns>
    /// The text of the range ending at the caret, which is longer than
    /// <paramref name="length" /> when the control moves by a larger unit than a character, or
    /// <see langword="null" /> when the control does not expose it in time.
    /// </returns>
    string? Read(int length);

    /// <summary>
    /// Reads a wider stretch of text before the caret, to tell an edited Word from text that
    /// has nothing to do with it.
    /// </summary>
    /// <param name="length">The number of characters to read.</param>
    /// <returns>
    /// Up to <paramref name="length" /> characters ending at the caret, or
    /// <see langword="null" /> when the control does not expose them in time.
    /// </returns>
    string? ReadContext(int length);

    /// <summary>
    /// Selects the characters the latest <see cref="Read" /> returned, so typing replaces them.
    /// </summary>
    /// <returns><see langword="true" /> if they are selected.</returns>
    bool SelectRead();
  }
}
