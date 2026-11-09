using Rekey.Core.Detection;
using Rekey.Core.Languages;
using Rekey.Core.Tests.Detection;
using Xunit;

namespace Rekey.Core.Tests {
  // Spec keyboard-capture: "Typing gap ends the Word".
  public sealed class TypingGapTests {
    private static readonly LayoutId _russian = KeyMapFixtures.Russian.Layout;

    [Fact]
    public void PauseOverTwoSecondsStartsANewWord() {
      Typist typist = new Typist().Type("ghb").Wait(TimeSpan.FromSeconds(3)).Type("dtn");

      Assert.Equal("dtn", typist.Word);
    }

    [Fact]
    public void PauseOfTwoSecondsKeepsTheWord() {
      Typist typist = new Typist().Type("ghb").Wait(TimeSpan.FromSeconds(2)).Type("dtn");

      Assert.Equal("ghbdtn", typist.Word);
    }

    [Fact]
    public void PauseOverTwoSecondsDropsDeferredWords() {
      var typist = new Typist();
      typist.Engine.DeferredWords.Add(Decision.Defer(_russian, "ыф"));

      typist.Type("ghb").Wait(TimeSpan.FromSeconds(3)).Type("dtn");

      Correction? next = typist.Engine.DeferredWords.Add(Decision.Correct(_russian, "вапро"));
      Assert.Equal(["вапро"], next?.Texts);
    }

    [Fact]
    public void ShortPauseKeepsDeferredWords() {
      var typist = new Typist();
      typist.Engine.DeferredWords.Add(Decision.Defer(_russian, "ыф"));

      typist.Type("ghb").Wait(TimeSpan.FromSeconds(1)).Type("dtn");

      Correction? next = typist.Engine.DeferredWords.Add(Decision.Correct(_russian, "вапро"));
      Assert.Equal(["ыф", "вапро"], next?.Texts);
    }

    [Fact]
    public void ModifierAfterThePauseCountsAsTheNextKeystroke() {
      Typist typist = new Typist()
          .Type("ghb")
          .Wait(TimeSpan.FromSeconds(3))
          .Press(KeyKind.Modifier, KeyModifiers.Shift)
          .Type("D");

      Assert.Equal("D", typist.Word);
    }
  }
}
