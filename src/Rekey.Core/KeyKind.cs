namespace Rekey.Core {
  /// <summary>
  /// Specifies what a pressed key means for the current Word.
  /// </summary>
  public enum KeyKind {
    /// <summary>A key that types a character in some active Layout and extends the Word.</summary>
    Character,

    /// <summary>Backspace, which removes the last Keystroke of the Word.</summary>
    Backspace,

    /// <summary>Space, which ends the Word.</summary>
    Space,

    /// <summary>Enter on the main keyboard or the numeric keypad, which ends the Word.</summary>
    Enter,

    /// <summary>Tab, which ends the Word.</summary>
    Tab,

    /// <summary>
    /// An arrow key, Home, End, Page Up, Page Down, Delete or Escape, which moves the caret or
    /// edits around it and so ends the Word.
    /// </summary>
    Navigation,

    /// <summary>Shift, Ctrl, Alt or a Windows logo key pressed on its own.</summary>
    Modifier,

    /// <summary>Any other key, such as a function key or Caps Lock.</summary>
    Other,
  }
}
