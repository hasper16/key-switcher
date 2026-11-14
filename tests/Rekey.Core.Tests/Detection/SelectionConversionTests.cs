using Rekey.Core.Detection;
using Xunit;

namespace Rekey.Core.Tests.Detection {
  // Design D4 of add-manual-correction: a selected text is read back to its keys in the current
  // Layout and rendered in another.
  public sealed class SelectionConversionTests {
    [Theory]
    [InlineData("ghbdtn vbh", "привет мир")]
    [InlineData("Ghbdtn", "Привет")]
    [InlineData("GHBDTN", "ПРИВЕТ")]
    [InlineData(";bdjq", "живой")]
    [InlineData("ckjdj 2024", "слово 2024")]
    [InlineData("€ghbdtn€", "€привет€")]
    public void TextTypedInEnglishReadsAsRussian(string selected, string converted) {
      Assert.Equal(
          converted,
          SelectionConversion.Convert(selected, KeyMapFixtures.EnglishUs, KeyMapFixtures.Russian));
    }

    [Fact]
    public void TextTypedInRussianReadsAsEnglish() {
      Assert.Equal(
          "hello world",
          SelectionConversion.Convert(
              "руддщ цщкдв", KeyMapFixtures.Russian, KeyMapFixtures.EnglishUs));
    }

    [Fact]
    public void UkrainianTargetTypesItsOwnLetters() {
      Assert.Equal(
          "привіт",
          SelectionConversion.Convert(
              "ghbdsn", KeyMapFixtures.EnglishUs, KeyMapFixtures.Ukrainian));
    }
  }
}
