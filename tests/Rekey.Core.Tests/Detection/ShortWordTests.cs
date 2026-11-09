using Rekey.Core.Detection;
using Xunit;
using static Rekey.Core.Tests.Detection.DetectionWorld;

namespace Rekey.Core.Tests.Detection {
  // Spec examples of wrong-layout-detection; real words come from the spec, not from users.
  public sealed class ShortWordTests {
    [Fact]
    public void ShortWordIsDeferredUnderBalanced() {
      Decision decision = Decide(KeyMapFixtures.EnglishUs, "yt", Sensitivity.Balanced);

      // "не" reads the same in Russian and Ukrainian, so either can confirm it.
      Assert.Equal(DecisionKind.Defer, decision.Kind);
      Assert.Equal(
          [new(KeyMapFixtures.Russian.Layout, "не"), new(KeyMapFixtures.Ukrainian.Layout, "не")],
          decision.Targets);
    }

    [Fact]
    public void ShortWordIsCorrectedUnderAggressive() {
      Assert.Equal(
          Decision.Correct(KeyMapFixtures.Russian.Layout, "не"),
          Decide(KeyMapFixtures.EnglishUs, "yt", Sensitivity.Aggressive));
    }
  }
}
