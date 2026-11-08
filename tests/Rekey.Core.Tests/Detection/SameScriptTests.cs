using Rekey.Core.Detection;
using Rekey.Core.Languages;
using Rekey.Core.Tests.Languages;
using Xunit;
using static Rekey.Core.Tests.Detection.DetectionWorld;

namespace Rekey.Core.Tests.Detection {
  // "привыт" comes from the spec; the other words are invented.
  public sealed class SameScriptTests {
    private const string _cyrillic = "абвгдежзийклмнопрстуфхцчшщьыэюяіїє";

    [Fact]
    public void UkrainianTypedInRussianLayoutIsCorrected() {
      Assert.Equal(
          Decision.Correct(KeyMapFixtures.Ukrainian.Layout, "привіт"),
          Decide(KeyMapFixtures.Russian, "привыт"));
    }

    [Fact]
    public void WordWithoutADifferingKeyIsKept() {
      WrongLayoutDetector detector = Detector("дорба", bigrams: "{}");

      Assert.Equal(Decision.Keep, DecideInRussian(detector, "дорба", Sensitivity.Aggressive));
    }

    [Fact]
    public void NonWordTargetIsKeptEvenUnderAggressive() {
      // Frequent letter groups make "сіпо" strong letter evidence, but it is not a word.
      WrongLayoutDetector detector = Detector(
          "дорба", bigrams: """{ "сі": 0.5, "іп": 0.5, "по": 0.5 }""");

      Assert.Equal(Decision.Keep, DecideInRussian(detector, "сыпо", Sensitivity.Aggressive));
    }

    private static Decision DecideInRussian(
        WrongLayoutDetector detector, string typed, Sensitivity sensitivity) {
      return detector.Decide(
          Keys(KeyMapFixtures.Russian, typed), KeyMapFixtures.Russian.Layout, sensitivity);
    }

    private static WrongLayoutDetector Detector(string ukrainianWord, string bigrams) {
      return new WrongLayoutDetector([
          new DetectionLayout(KeyMapFixtures.Russian, Invented("ru", "00000419", "ывапр", "{}")),
          new DetectionLayout(
              KeyMapFixtures.Ukrainian, Invented("uk", "00000422", ukrainianWord, bigrams))]);
    }

    private static LanguageProfile Invented(
        string code, string klid, string word, string bigrams) {
      return TestProfiles.Load($$"""
          {
            "format": 1, "code": "{{code}}", "displayName": "{{code}}",
            "layoutIds": ["{{klid}}"], "alphabet": "{{_cyrillic}}",
            "words1": [], "words2": [], "commonWords": ["{{word}}"],
            "bigrams": {{bigrams}}, "trigrams": { "сіп": 0.5, "іпо": 0.5 }
          }
          """);
    }
  }
}
