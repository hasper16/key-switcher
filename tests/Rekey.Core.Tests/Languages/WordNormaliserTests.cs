using Rekey.Core.Languages;
using Xunit;

namespace Rekey.Core.Tests.Languages {
  public sealed class WordNormaliserTests {
    [Theory]
    [InlineData("Zorbit", "zorbit")]
    [InlineData("КРЫВАЛ", "крывал")]
    [InlineData("ҐАРНИК", "ґарник")]
    public void FoldsCase(string word, string expected) {
      Assert.Equal(expected, WordNormaliser.Normalise(word));
    }

    [Theory]
    [InlineData("тёпс", "тепс")]
    [InlineData("ЁЖИК", "ежик")]
    public void TreatsYoAsYe(string word, string expected) {
      Assert.Equal(expected, WordNormaliser.Normalise(word));
    }

    [Theory]
    [InlineData("м’ярк")]
    [InlineData("мʼярк")]
    [InlineData("м'ярк")]
    public void UnifiesApostropheVariants(string word) {
      Assert.Equal("м'ярк", WordNormaliser.Normalise(word));
    }

    [Fact]
    public void LeavesOtherLettersAlone() {
      Assert.Equal("ї-ґє", WordNormaliser.Normalise("Ї-Ґє"));
    }
  }
}
