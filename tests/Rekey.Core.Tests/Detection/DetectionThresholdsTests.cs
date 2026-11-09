using Rekey.Core.Detection;
using Xunit;

namespace Rekey.Core.Tests.Detection {
  public sealed class DetectionThresholdsTests {
    [Fact]
    public void EachSensitivityResolvesToItsOwnRecord() {
      DetectionThresholds[] records =
          [.. Enum.GetValues<Sensitivity>().Select(DetectionThresholds.For)];

      Assert.Equal(3, records.Distinct().Count());
    }

    [Fact]
    public void OnlyAggressiveCorrectsShortWordsAndStrongLetterEvidence() {
      Assert.True(DetectionThresholds.For(Sensitivity.Aggressive).CorrectsShortWords);
      Assert.False(DetectionThresholds.For(Sensitivity.Balanced).CorrectsShortWords);
      Assert.False(DetectionThresholds.For(Sensitivity.Cautious).CorrectsShortWords);
      Assert.Equal(
          DecisionKind.Correct, DetectionThresholds.For(Sensitivity.Aggressive).OnStrongLetters);
      Assert.Equal(
          DecisionKind.Defer, DetectionThresholds.For(Sensitivity.Balanced).OnStrongLetters);
    }
  }
}
