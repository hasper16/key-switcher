using Rekey.Core.Detection;
using Rekey.Core.Languages;
using Rekey.Win32;

namespace Rekey.DesktopChecks {
  // Corrects only the spec's own example Words, from English (US) to Russian, so the checks
  // know exactly what to expect without the scorer deciding.
  internal sealed class SpecWords {
    public static readonly LayoutId EnglishUs = new(0x00000409);
    public static readonly LayoutId Russian = new(0x00000419);

    private readonly KeyMap _english = KeyMapReader.Read(EnglishUs);
    private readonly KeyMap _russian = KeyMapReader.Read(Russian);

    // Called on the worker thread; the Key Maps never change.
    public Decision Evaluate(IReadOnlyList<Keystroke> word, LayoutId typedIn) {
      if (typedIn != EnglishUs || _russian.Render(word) is not { } text) {
        return Decision.Keep;
      }

      return _english.Render(word) switch {
        "ghbdtn" or "pyf." => Decision.Correct(Russian, text),
        "yt" => Decision.Defer(Russian, text),
        _ => Decision.Keep,
      };
    }
  }
}
