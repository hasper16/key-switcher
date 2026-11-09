using Rekey.Core.Detection;

namespace Rekey.Core {
  /// <summary>
  /// Holds the Keystrokes of the current Word and applies the keys that build and end it.
  /// </summary>
  internal sealed class WordTracker {
    private const KeyModifiers _shortcutModifiers =
        KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Windows;

    private readonly List<Keystroke> _keystrokes = [];

    public IReadOnlyList<Keystroke> Keystrokes => _keystrokes;

    public void Add(KeyDown keyDown) {
      if (EndsWord(keyDown)) {
        Clear();
        return;
      }

      if (keyDown.Kind == KeyKind.Character) {
        _keystrokes.Add(keyDown.Keystroke);
      } else if (keyDown.Kind == KeyKind.Backspace && _keystrokes.Count > 0) {
        _keystrokes.RemoveAt(_keystrokes.Count - 1);
      } else {
        // Modifiers and other keys pressed alone neither build nor end the Word.
      }
    }

    public void Clear() {
      _keystrokes.Clear();
    }

    private static bool EndsWord(KeyDown keyDown) {
      return keyDown.Kind switch {
        KeyKind.Space or KeyKind.Enter or KeyKind.Tab or KeyKind.Navigation => true,
        KeyKind.Modifier => false,
        _ => (keyDown.Modifiers & _shortcutModifiers) != 0,
      };
    }
  }
}
