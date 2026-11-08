using Rekey.Core.Detection;
using Xunit;
using static Rekey.Core.Tests.Detection.DetectionWorld;

namespace Rekey.Core.Tests.Detection {
  // Spec and design examples of wrong-layout-detection; real words come from the spec.
  public sealed class EdgePunctuationTests {
    [Fact]
    public void ExclamationMarkFollowsTheCorrectedWord() {
      Assert.Equal(
          Decision.Correct(KeyMapFixtures.Russian.Layout, "привет!"),
          Decide(KeyMapFixtures.EnglishUs, "ghbdtn!"));
    }

    [Fact]
    public void PunctuationIsTypedFromTheTargetLayout() {
      Assert.Equal(
          Decision.Correct(KeyMapFixtures.Russian.Layout, "привет,"),
          Decide(KeyMapFixtures.EnglishUs, "ghbdtn?"));
    }

    [Fact]
    public void DigitAtTheEdgeKeepsTheWord() {
      Assert.Equal(Decision.Keep, Decide(KeyMapFixtures.EnglishUs, "ghbdtn1"));
    }

    [Fact]
    public void PunctuationAloneIsKept() {
      Assert.Equal(Decision.Keep, Decide(KeyMapFixtures.EnglishUs, "!!!"));
    }
  }
}
