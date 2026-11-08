using Rekey.Core.Detection;
using Xunit;
using static Rekey.Core.Tests.Detection.DetectionWorld;

namespace Rekey.Core.Tests.Detection {
  // Spec examples of wrong-layout-detection; real words come from the spec, not from users.
  public sealed class ShortWordTests {
    [Fact]
    public void ShortWordIsDeferredUnderBalanced() {
      Assert.Equal(
          Decision.Defer(KeyMapFixtures.Russian.Layout, "не"),
          Decide(KeyMapFixtures.EnglishUs, "yt", Sensitivity.Balanced));
    }

    [Fact]
    public void ShortWordIsCorrectedUnderAggressive() {
      Assert.Equal(
          Decision.Correct(KeyMapFixtures.Russian.Layout, "не"),
          Decide(KeyMapFixtures.EnglishUs, "yt", Sensitivity.Aggressive));
    }
  }
}
