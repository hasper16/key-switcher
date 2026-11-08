using Rekey.Core.Detection;
using Xunit;

namespace Rekey.Core.Tests.Detection {
  // Scores are placed relative to each Sensitivity's thresholds, so tuning keeps these valid.
  public sealed class DecisionPolicyTests {
    [Theory]
    [InlineData(Sensitivity.Cautious, DecisionKind.Correct)]
    [InlineData(Sensitivity.Balanced, DecisionKind.Correct)]
    [InlineData(Sensitivity.Aggressive, DecisionKind.Correct)]
    public void ClearWordTarget(Sensitivity sensitivity, DecisionKind expected) {
      DetectionThresholds t = DetectionThresholds.For(sensitivity);

      Assert.Equal(expected, Decide(t, current: 0, Word(1 + t.MinWordMargin)));
    }

    [Theory]
    [InlineData(Sensitivity.Cautious, DecisionKind.Defer)]
    [InlineData(Sensitivity.Balanced, DecisionKind.Correct)]
    [InlineData(Sensitivity.Aggressive, DecisionKind.Correct)]
    public void WordTargetWithASmallMargin(Sensitivity sensitivity, DecisionKind expected) {
      DetectionThresholds t = DetectionThresholds.For(sensitivity);
      double current = 0.95;

      Assert.Equal(expected, Decide(t, current, Word(current + 0.29)));
    }

    [Theory]
    [InlineData(Sensitivity.Cautious, DecisionKind.Defer)]
    [InlineData(Sensitivity.Balanced, DecisionKind.Defer)]
    [InlineData(Sensitivity.Aggressive, DecisionKind.Correct)]
    public void StrongLetterEvidenceOnly(Sensitivity sensitivity, DecisionKind expected) {
      DetectionThresholds t = DetectionThresholds.For(sensitivity);

      Assert.Equal(expected, Decide(t, current: 0, NonWord(t.StrongLetterScore)));
    }

    [Theory]
    [InlineData(Sensitivity.Cautious, DecisionKind.Defer)]
    [InlineData(Sensitivity.Balanced, DecisionKind.Keep)]
    [InlineData(Sensitivity.Aggressive, DecisionKind.Keep)]
    public void WeakLetterEvidenceOnly(Sensitivity sensitivity, DecisionKind expected) {
      DetectionThresholds t = DetectionThresholds.For(sensitivity);
      double weak = (t.GibberishCeiling + t.StrongLetterScore) / 2;

      Assert.Equal(expected, Decide(t, current: 0, NonWord(weak)));
    }

    [Theory]
    [InlineData(Sensitivity.Cautious, DecisionKind.Defer)]
    [InlineData(Sensitivity.Balanced, DecisionKind.Defer)]
    [InlineData(Sensitivity.Aggressive, DecisionKind.Correct)]
    public void ShortWordTarget(Sensitivity sensitivity, DecisionKind expected) {
      DetectionThresholds t = DetectionThresholds.For(sensitivity);

      Assert.Equal(expected, Decide(t, current: 0, Word(2), isShort: true));
    }

    [Theory]
    [InlineData(Sensitivity.Cautious)]
    [InlineData(Sensitivity.Balanced)]
    [InlineData(Sensitivity.Aggressive)]
    public void ShortNonWordTargetIsDeferred(Sensitivity sensitivity) {
      DetectionThresholds t = DetectionThresholds.For(sensitivity);

      Assert.Equal(DecisionKind.Defer, Decide(t, current: 0, NonWord(1), isShort: true));
    }

    private static DecisionKind Decide(
        DetectionThresholds thresholds,
        double current,
        RenderingScore target,
        bool isShort = false) {
      return DecisionPolicy.Decide(
          new RenderingScore(false, current), target, isShort, thresholds);
    }

    private static RenderingScore Word(double value) {
      return new RenderingScore(true, value);
    }

    private static RenderingScore NonWord(double value) {
      return new RenderingScore(false, value);
    }
  }
}
