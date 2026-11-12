using Rekey.Core;

namespace Rekey.DesktopChecks {
  // Counts what the worker saw between Word ends, for try-corrections: counts only, never text.
  internal sealed class WordEndTracer {
    private int _keys;
    private int _unconfirmed;
    private int _held;
    private int _focusReports;
    private int _wordKeys;

    // Takes one event the worker handled, with the Word's length after it; returns a line at
    // each key that ends a Word.
    public string? Add(InputEvent inputEvent, int wordKeys) {
      switch (inputEvent) {
        case FocusChanged or FocusLost:
          _focusReports++;
          return null;
        case KeyDown key:
          return AddKey(key, wordKeys);
        default:
          return null;
      }
    }

    private string? AddKey(KeyDown key, int wordKeys) {
      _keys++;
      _unconfirmed += key.FocusConfirmed ? 0 : 1;
      _held += key.Held ? 1 : 0;
      if (key.Kind is not (KeyKind.Space or KeyKind.Enter or KeyKind.Tab)) {
        _wordKeys = wordKeys;
        return null;
      }

      string line = $"Word end: keys {_keys}, unconfirmed {_unconfirmed}, held {_held}, "
          + $"Word keys before the end {_wordKeys}, focus reports {_focusReports}";
      (_keys, _unconfirmed, _held, _focusReports, _wordKeys) = (0, 0, 0, 0, 0);
      return line;
    }
  }
}
