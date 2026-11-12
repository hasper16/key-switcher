using Rekey.Core.Detection;
using Rekey.Core.Tests.Detection;
using Xunit;

namespace Rekey.Core.Tests {
  // Spec text-replacement, engine side; the words are the spec's own examples.
  public sealed class TextReplacementTests {
    private static readonly SwitchLayout _toRussian = new(KeyMapFixtures.Russian.Layout);

    [Fact]
    public void HeldSpaceAfterAWordToCorrectReplacesIt() {
      Typist typist = Corrector().Type("ghbdtn").Hold(KeyKind.Space);

      Assert.Equal([new ReplaceText("ghbdtn", "привет")], typist.Actions);
    }

    [Fact]
    public void AppliedReplacementSwitchesTheLayout() {
      Typist typist = Corrector().Type("ghbdtn").Hold(KeyKind.Space);
      typist.Actions.Clear();

      typist.Finish();

      Assert.Equal([_toRussian], typist.Actions);
    }

    [Fact]
    public void CancelledReplacementSwitchesNothing() {
      Typist typist = Corrector().Type("ghbdtn").Hold(KeyKind.Space);
      typist.Actions.Clear();

      typist.Finish(applied: false);

      Assert.Empty(typist.Actions);
    }

    [Fact]
    public void DeferredRunIsReplacedWithItsSpaces() {
      // Spec: a Correction covers "yt pyf." with target "не знаю".
      Typist typist = Corrector()
          .Type("yt").Hold(KeyKind.Space).Type("pyf.").Hold(KeyKind.Space);

      Assert.Equal([new ReplaceText("yt pyf.", "не знаю")], typist.Actions);
    }

    [Fact]
    public void EnterEndsTheRunAfterItsWord() {
      Typist typist = Corrector()
          .Type("yt").Hold(KeyKind.Enter).Type("pyf.").Hold(KeyKind.Space);

      Assert.Equal([new ReplaceText("pyf.", "знаю")], typist.Actions);
    }

    [Fact]
    public void BackspacePastTheWordStartEndsTheRun() {
      Typist typist = Corrector().Type("yt").Hold(KeyKind.Space)
          .Press(KeyKind.Backspace).Type("pyf.").Hold(KeyKind.Space);

      Assert.Equal([new ReplaceText("pyf.", "знаю")], typist.Actions);
    }

    [Fact]
    public void ClickEndsTheRun() {
      Typist typist = Corrector().Type("yt").Hold(KeyKind.Space).Click()
          .Type("pyf.").Hold(KeyKind.Space);

      Assert.Equal([new ReplaceText("pyf.", "знаю")], typist.Actions);
    }

    [Fact]
    public void WordsAfterACancelledReplacementOnTheSameLineAreStillReplaced() {
      // The owner's VS Code check: three Words, Space after each, the first one cancelled.
      Typist typist = Corrector().Type("ghbdtn").Hold(KeyKind.Space).Finish(applied: false);
      typist.Actions.Clear();

      typist.Type("ghbdtn").Hold(KeyKind.Space).Finish(applied: false)
          .Type("ghbdtn").Hold(KeyKind.Space);

      Assert.Equal(
          [new ReplaceText("ghbdtn", "привет"), new ReplaceText("ghbdtn", "привет")],
          typist.Actions);
    }

    [Fact]
    public void TriggerKeyThatWasNotHeldReplacesNothing() {
      Typist typist = Corrector().Type("ghbdtn").Press(KeyKind.Space);

      Assert.Empty(typist.Actions);
    }

    [Fact]
    public void ModifiedTriggerKeyReplacesNothing() {
      Typist typist = Corrector().Type("ghbdtn").Hold(KeyKind.Space, KeyModifiers.Control);

      Assert.Empty(typist.Actions);
    }

    [Fact]
    public void WithoutAnEvaluatorNothingIsReplaced() {
      Typist typist = new Typist().Type("ghbdtn").Hold(KeyKind.Space);

      Assert.Empty(typist.Actions);
    }

    [Fact]
    public void UndoWithinTheWindowRestoresTheTypedText() {
      Typist typist = Corrected().Wait(TimeSpan.FromSeconds(2)).RequestUndo();

      Assert.Equal([new ReplaceText("привет ", "ghbdtn ")], typist.Actions);
    }

    [Fact]
    public void AppliedUndoSwitchesBackAndIsReported() {
      Typist typist = Corrected().RequestUndo();
      typist.Actions.Clear();

      typist.Finish();

      Assert.Equal(
          [
            new SwitchLayout(KeyMapFixtures.EnglishUs.Layout),
            new UndoPerformed("ghbdtn", "привет"),
          ],
          typist.Actions);
    }

    [Fact]
    public void UndoAfterTheWindowDoesNothing() {
      Typist typist = Corrected().Wait(TimeSpan.FromSeconds(6)).RequestUndo();

      Assert.Empty(typist.Actions);
    }

    [Fact]
    public void UndoAfterAnotherKeyDoesNothing() {
      Typist typist = Corrected().Type("r").RequestUndo();

      Assert.Empty(typist.Actions);
    }

    [Fact]
    public void UndoAfterAClickDoesNothing() {
      Typist typist = Corrected().Click().RequestUndo();

      Assert.Empty(typist.Actions);
    }

    [Fact]
    public void ModifierBeforeUndoKeepsItPossible() {
      Typist typist = Corrected().Press(KeyKind.Modifier, KeyModifiers.Control).RequestUndo();

      Assert.Equal([new ReplaceText("привет ", "ghbdtn ")], typist.Actions);
    }

    [Fact]
    public void TypingGapWithinTheWindowKeepsUndoPossible() {
      Typist typist = Corrected()
          .Wait(TimeSpan.FromSeconds(3))
          .Press(KeyKind.Modifier, KeyModifiers.Control)
          .RequestUndo();

      Assert.Equal([new ReplaceText("привет ", "ghbdtn ")], typist.Actions);
    }

    [Fact]
    public void UndoAfterAnEnterTriggeredCorrectionDoesNothing() {
      Typist typist = Corrector().Type("ghbdtn").Hold(KeyKind.Enter).Finish();
      typist.Actions.Clear();

      typist.RequestUndo();

      Assert.Empty(typist.Actions);
    }

    [Fact]
    public void UndoAfterACancelledReplacementDoesNothing() {
      Typist typist = Corrector().Type("ghbdtn").Hold(KeyKind.Space).Finish(applied: false);
      typist.Actions.Clear();

      typist.RequestUndo();

      Assert.Empty(typist.Actions);
    }

    [Fact]
    public void SecondUndoDoesNothing() {
      Typist typist = Corrected().RequestUndo().Finish();
      typist.Actions.Clear();

      typist.RequestUndo();

      Assert.Empty(typist.Actions);
    }

    // "ghbdtn" corrected and the Layout Switch done, with the actions cleared.
    private static Typist Corrected() {
      Typist typist = Corrector().Type("ghbdtn").Hold(KeyKind.Space).Finish();
      typist.Actions.Clear();
      return typist;
    }

    // Corrects every Word to Russian, except that "yt" waits for the next Word.
    private static Typist Corrector() {
      return new Typist((word, _) => {
        string text = KeyMapFixtures.Russian.Render(word)!;
        return KeyMapFixtures.EnglishUs.Render(word) == "yt"
            ? Decision.Defer(KeyMapFixtures.Russian.Layout, text)
            : Decision.Correct(KeyMapFixtures.Russian.Layout, text);
      });
    }
  }
}
