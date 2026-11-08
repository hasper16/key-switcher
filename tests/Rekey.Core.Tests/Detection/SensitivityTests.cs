using Rekey.Core.Detection;
using Rekey.Core.Tests.Languages;
using Xunit;
using static Rekey.Core.Tests.Detection.DetectionWorld;

namespace Rekey.Core.Tests.Detection {
  public sealed class SensitivityTests {
    // Invented: "plimo" is not an English word, but its letter groups are very frequent.
    private static readonly WrongLayoutDetector _detector = new([
        new DetectionLayout(KeyMapFixtures.EnglishUs, TestProfiles.Load("""
            {
              "format": 1, "code": "en", "displayName": "en",
              "layoutIds": ["00000409"], "alphabet": "abcdefghijklmnopqrstuvwxyz",
              "words1": [], "words2": [], "commonWords": ["zorbit"],
              "bigrams": { "pl": 0.5, "li": 0.5, "im": 0.5, "mo": 0.5 },
              "trigrams": { "pli": 0.5, "lim": 0.5, "imo": 0.5 }
            }
            """)),
        new DetectionLayout(KeyMapFixtures.Russian, Russian)]);

    [Theory]
    [InlineData(Sensitivity.Cautious, DecisionKind.Defer)]
    [InlineData(Sensitivity.Balanced, DecisionKind.Defer)]
    [InlineData(Sensitivity.Aggressive, DecisionKind.Correct)]
    public void RareWordWithStrongLetterEvidence(Sensitivity sensitivity, DecisionKind expected) {
      Decision decision = _detector.Decide(
          Keys(KeyMapFixtures.Russian, "здшьщ"), KeyMapFixtures.Russian.Layout, sensitivity);

      Assert.Equal(expected, decision.Kind);
      Assert.Equal("plimo", decision.Text);
    }
  }
}
