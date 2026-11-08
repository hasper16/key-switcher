using Rekey.Core.Detection;
using Xunit;
using static Rekey.Core.Tests.Detection.DetectionWorld;

namespace Rekey.Core.Tests.Detection {
  // Spec examples of wrong-layout-detection; real words come from the spec, not from users.
  public sealed class CorrectRulesTests {
    [Fact]
    public void EnglishTypedAsRussianIsCorrected() {
      Assert.Equal(
          Decision.Correct(KeyMapFixtures.EnglishUs.Layout, "hello"),
          Decide(KeyMapFixtures.Russian, "руддщ"));
    }

    [Fact]
    public void RussianTypedAsEnglishIsCorrected() {
      Assert.Equal(
          Decision.Correct(KeyMapFixtures.Russian.Layout, "привет"),
          Decide(KeyMapFixtures.EnglishUs, "ghbdtn"));
    }

    [Fact]
    public void SameWordInTwoLanguagesIsNotAmbiguous() {
      // "без" is a word in Russian and in Ukrainian; it ranks higher in Russian.
      Assert.Equal(
          Decision.Correct(KeyMapFixtures.Russian.Layout, "без"),
          Decide(KeyMapFixtures.EnglishUs, ",tp"));
    }
  }
}
