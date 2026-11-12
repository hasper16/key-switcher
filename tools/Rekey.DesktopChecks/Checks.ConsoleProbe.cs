using System.Drawing;
using Rekey.Core.Languages;

namespace Rekey.DesktopChecks {
  // Finds out how a console takes typed text, for Rekey's replacement there. Runs without a
  // Word Evaluator: every key is the probe's own. Reports booleans only, never text.
  internal sealed partial class Checks {
    public int RunConsoleProbe() {
      Begin(minutes: 1);
      if (StartConsole() is not { } console) {
        Expect("console probe", false, "the console window did not appear");
        return End();
      }

      Probe("Backspaces then Unicode in one call", console, SpecWords.EnglishUs, () => {
        Input.Type("ghbdtn");
        Input.TypeUnicode("привет ", backspaces: 6, oneCallEach: false);
      });
      Probe("Unicode alone in one call", console, SpecWords.EnglishUs, () => {
        Input.TypeUnicode("привет ", backspaces: 0, oneCallEach: false);
      });
      Probe("Backspaces then Unicode, one call per key", console, SpecWords.EnglishUs, () => {
        Input.Type("ghbdtn");
        Input.TypeUnicode("привет ", backspaces: 6, oneCallEach: true);
      });
      Probe("the same keys in the Russian Layout", console, SpecWords.Russian, () => {
        Input.Type("ghbdtn");
        Input.Space();
      });
      return End();
    }

    private static string Detail(bool switched, bool corrected, bool typed, bool read) {
      return $"Layout switched {switched}, corrected Word and Space shown {corrected}, "
          + $"typed Word left {typed}, buffer read {read}";
    }

    // Empties the prompt line in the Layout, types, then reports what the console shows.
    private void Probe(string variant, Point console, LayoutId layout, Action type) {
      bool switched = SwitchIn(console, layout);
      Input.Escape();
      type();
      Thread.Sleep(_settle);
      string? text = ConsoleText();
      bool corrected = text?.Contains("привет ", StringComparison.Ordinal) == true;
      bool typed = text?.Contains("ghbdtn", StringComparison.Ordinal) == true;
      Expect(
          $"console probe: {variant}",
          corrected && !typed,
          Detail(switched, corrected, typed, text is not null));
      Input.Escape();
    }
  }
}
