namespace Rekey.Win32.Focus {
  /// <summary>
  /// Tells which focus moves inside one window keep the Word: from a text control to an element
  /// that takes no text, such as a row of an autocomplete list (design D7).
  /// </summary>
  internal static class SuggestionFocus {
    private const int _button = 50000;
    private const int _comboBox = 50003;
    private const int _edit = 50004;
    private const int _menu = 50009;
    private const int _menuBar = 50010;
    private const int _menuItem = 50011;
    private const int _document = 50030;
    private const int _splitButton = 50031;

    /// <summary>
    /// Returns whether a move between two UI Automation control types keeps the Word, if the
    /// user was typing.
    /// </summary>
    /// <param name="from">The control type of the control the Word is typed in.</param>
    /// <param name="to">The control type of the newly focused element.</param>
    /// <param name="toIsPassword">Whether the newly focused element is a Password Field.</param>
    public static bool KeepsWord(int from, int to, bool toIsPassword) {
      return from is _edit or _document
          && !toIsPassword
          && to is not (_edit or _document or _comboBox or _button or _splitButton or _menu
              or _menuBar or _menuItem);
    }
  }
}
