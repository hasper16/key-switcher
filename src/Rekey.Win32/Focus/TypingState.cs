using Rekey.Core;

namespace Rekey.Win32.Focus {
  /// <summary>
  /// Remembers whether the last key-down or click the worker read was typing, to tell a focus
  /// move caused by typing from one caused by navigation (design D7).
  /// </summary>
  /// <remarks>Not thread-safe: the worker owns it.</remarks>
  internal sealed class TypingState {
    private const KeyModifiers _shortcut =
        KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Windows;

    private bool _typing;
    private long _actionsRead;

    /// <summary>
    /// Returns whether the input read so far could have moved focus only by typing.
    /// </summary>
    /// <param name="actionsWritten">The key-downs and clicks the hook has written.</param>
    public Typing Now(long actionsWritten) {
      if (actionsWritten != _actionsRead) {
        return Typing.Waiting;
      }

      return _typing ? Typing.Yes : Typing.No;
    }

    /// <summary>
    /// Returns whether a focus move seen just before a key is handled was caused by typing.
    /// </summary>
    /// <param name="kind">The key's kind.</param>
    /// <param name="modifiers">The modifiers held with it.</param>
    /// <param name="delivered">
    /// Whether the key already reached the app; a held key did not, so it cannot have moved
    /// focus.
    /// </param>
    /// <param name="actions">The count the ring gave the key.</param>
    /// <param name="actionsWritten">The key-downs and clicks the hook has written.</param>
    public Typing ForKey(
        KeyKind kind, KeyModifiers modifiers, bool delivered, long actions, long actionsWritten) {
      if (actionsWritten != actions) {
        return Typing.Waiting;
      }

      bool typing = _typing && (!delivered || IsTyping(kind, modifiers) is not false);
      return typing ? Typing.Yes : Typing.No;
    }

    /// <summary>Records a key-down the worker read.</summary>
    public void KeyRead(KeyKind kind, KeyModifiers modifiers, long actions) {
      _actionsRead = actions;
      _typing = IsTyping(kind, modifiers) ?? _typing;
    }

    /// <summary>Records a mouse-button press the worker read.</summary>
    public void ClickRead(long actions) {
      _actionsRead = actions;
      _typing = false;
    }

    // Null for a modifier pressed alone: it moves no focus inside a window and ends no Word.
    private static bool? IsTyping(KeyKind kind, KeyModifiers modifiers) {
      if (kind == KeyKind.Modifier) {
        return null;
      }

      return kind is KeyKind.Character or KeyKind.Backspace && (modifiers & _shortcut) == 0;
    }
  }
}
