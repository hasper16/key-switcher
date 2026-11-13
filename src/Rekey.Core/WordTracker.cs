using Rekey.Core.Detection;

namespace Rekey.Core {
  /// <summary>
  /// Holds the Keystrokes of the current Word and applies the keys that build and end it.
  /// </summary>
  internal sealed class WordTracker {
    private const KeyModifiers _shortcutModifiers =
        KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Windows;

    private readonly List<Keystroke> _keystrokes = [];

    // Set when a Keystroke of the current Word was dropped; the rest of that Word is not kept.
    private bool _spoiled;

    public IReadOnlyList<Keystroke> Keystrokes => _keystrokes;

    // Whether the user switched Layout themselves since the Word began.
    public bool LayoutSwitchedByUser { get; private set; }

    // Whether the Word is not kept because one of its keys could not be confirmed outside a
    // Password Field.
    public bool Unconfirmed { get; private set; }

    public void Add(KeyDown keyDown) {
      if (EndsWord(keyDown)) {
        Clear();
        return;
      }

      if (_spoiled) {
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
      _spoiled = false;
      LayoutSwitchedByUser = false;
      Unconfirmed = false;
    }

    public void MarkLayoutSwitchedByUser() {
      LayoutSwitchedByUser = true;
    }

    // Drops a key whose focus is unknown. A key that ends the Word still ends it; any other key
    // spoils the Word, because keeping the rest would leave a Word missing a Keystroke.
    public void Drop(KeyDown keyDown) {
      bool wasUnconfirmed = Unconfirmed;
      Clear();
      _spoiled = !EndsWord(keyDown);

      // An earlier unconfirmed key of the same Word still explains why it is not kept.
      Unconfirmed = _spoiled && (wasUnconfirmed || !keyDown.FocusConfirmed);
    }

    // Keystrokes were lost somewhere in the current Word, so none of it is kept until it ends.
    public void Spoil() {
      Clear();
      _spoiled = true;
    }

    internal static bool EndsWord(KeyDown keyDown) {
      return keyDown.Kind switch {
        KeyKind.Space or KeyKind.Enter or KeyKind.Tab or KeyKind.Navigation => true,
        KeyKind.Modifier => false,
        _ => (keyDown.Modifiers & _shortcutModifiers) != 0,
      };
    }
  }
}
