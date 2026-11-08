using System.Text.Json;
using Rekey.Core.Detection;
using Rekey.Core.Languages;
using Rekey.Core.Tests.Languages;
using Xunit;
using static Rekey.Core.Tests.Detection.DetectionWorld;

namespace Rekey.Core.Tests.Detection {
  // Spec examples of wrong-layout-detection; real words come from the spec, not from users.
  public sealed class KeepRulesTests {
    [Fact]
    public void CorrectlyTypedWordIsKept() {
      Assert.Equal(Decision.Keep, Decide(KeyMapFixtures.EnglishUs, "hello"));
    }

    [Fact]
    public void ValidWordIsKeptWhateverItsOtherRenderings() {
      // Invented words: the keys of "plimo" in English (US) read "здшьщ" in Russian.
      var detector = new WrongLayoutDetector([
          new DetectionLayout(KeyMapFixtures.EnglishUs, Invented("en", "00000409", "plimo")),
          new DetectionLayout(KeyMapFixtures.Russian, Invented("ru", "00000419", "здшьщ"))]);
      IReadOnlyList<Keystroke> word = Keys(KeyMapFixtures.EnglishUs, "plimo");

      Assert.Equal(Decision.Keep, detector.Decide(word, KeyMapFixtures.EnglishUs.Layout, default));
    }

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("getValue")]
    public void CodeLikeWordIsKept(string typed) {
      // The Russian profile lists the Russian Rendering, so only the code rule keeps the Word.
      IReadOnlyList<Keystroke> word = Keys(KeyMapFixtures.EnglishUs, typed);
      string russian = KeyMapFixtures.Russian.Render(word)!;
      var detector = new WrongLayoutDetector([
          new DetectionLayout(KeyMapFixtures.EnglishUs, Invented("en", "00000409", "plimo")),
          new DetectionLayout(KeyMapFixtures.Russian, Invented("ru", "00000419", russian))]);

      Assert.Equal(Decision.Keep, detector.Decide(word, KeyMapFixtures.EnglishUs.Layout, default));
    }

    [Fact]
    public void WordWithNoPlausibleRenderingIsKept() {
      Assert.Equal(Decision.Keep, Decide(KeyMapFixtures.EnglishUs, "qzxv"));
    }

    [Fact]
    public void WordInALayoutWithoutAProfileIsKept() {
      var german = new KeyMap(new LayoutId(0x00000407), KeyMapFixtures.EnglishUs.Entries);

      Assert.Equal(
          Decision.Keep,
          Detector.Decide(Keys(KeyMapFixtures.EnglishUs, "ghbdtn"), german.Layout, default));
    }

    [Fact]
    public void WordWithAnUnrenderableKeyIsKept() {
      IReadOnlyList<Keystroke> word =
          [.. Keys(KeyMapFixtures.EnglishUs, "ghbdtn"), new(0x56, false, false)];

      Assert.Equal(
          Decision.Keep, Detector.Decide(word, KeyMapFixtures.EnglishUs.Layout, default));
    }

    [Fact]
    public void AmbiguousWordIsKept() {
      // Invented words: the keys of "сыпа" in Russian read "сіпа" in Ukrainian and "csgf" in
      // English.
      IReadOnlyList<Keystroke> word = Keys(KeyMapFixtures.Russian, "сыпа");
      WrongLayoutDetector ambiguous = InventedDetector(ukrainianWord: "сіпа");
      WrongLayoutDetector clear = InventedDetector(ukrainianWord: "ліпа");

      Assert.Equal(
          Decision.Keep, ambiguous.Decide(word, KeyMapFixtures.EnglishUs.Layout, default));
      Assert.Equal(
          Decision.Correct(KeyMapFixtures.Russian.Layout, "сыпа"),
          clear.Decide(word, KeyMapFixtures.EnglishUs.Layout, default));
    }

    private static WrongLayoutDetector InventedDetector(string ukrainianWord) {
      return new WrongLayoutDetector([
          new DetectionLayout(KeyMapFixtures.EnglishUs, Invented("en", "00000409", "plimo")),
          new DetectionLayout(KeyMapFixtures.Russian, Invented("ru", "00000419", "сыпа")),
          new DetectionLayout(
              KeyMapFixtures.Ukrainian, Invented("uk", "00000422", ukrainianWord))]);
    }

    private static LanguageProfile Invented(string code, string klid, string word) {
      string alphabet =
          code == "en" ? "abcdefghijklmnopqrstuvwxyz" : "абвгдежзийклмнопрстуфхцчшщьыэюяіїє";
      return TestProfiles.Load($$"""
          {
            "format": 1, "code": "{{code}}", "displayName": "{{code}}",
            "layoutIds": ["{{klid}}"], "alphabet": "{{alphabet}}",
            "words1": [], "words2": [], "commonWords": [{{JsonSerializer.Serialize(word)}}],
            "bigrams": {}, "trigrams": {}
          }
          """);
    }
  }
}
