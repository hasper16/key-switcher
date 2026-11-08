using Rekey.Core.Detection;
using Rekey.Core.Languages;
using Rekey.Core.Tests.Languages;
using Xunit;

namespace Rekey.Core.Tests.Detection {
  public sealed class RenderingScorerTests {
    // Invented words; "zx" is a common word but not a listed two-letter word.
    private static readonly LanguageProfile _profile = TestProfiles.Load("""
        {
          "format": 1, "code": "en", "displayName": "Test",
          "layoutIds": ["00000409"], "alphabet": "abcdefghijklmnopqrstuvwxyz",
          "words1": ["o"], "words2": ["ka"],
          "commonWords": ["zorbit", "plim", "zx", "quandle"],
          "bigrams": { "zo": 0.1, "or": 0.1 },
          "trigrams": { "zor": 0.01 }
        }
        """);

    [Theory]
    [InlineData("zorbit")]
    [InlineData("Quandle")]
    [InlineData("o")]
    [InlineData("KA")]
    public void ListedWordsAreWords(string text) {
      Assert.True(RenderingScorer.Score(_profile, text).IsWord);
    }

    [Fact]
    public void ContractionSplitByTheWordListIsAWord() {
      Assert.True(RenderingScorer.Score(_profile, "plim'zorbit").IsWord);
    }

    [Theory]
    [InlineData("zorbin")]
    [InlineData("zx")]
    [InlineData("x")]
    [InlineData("plim'zorbin")]
    [InlineData("plim'")]
    public void OtherTextIsNotAWord(string text) {
      Assert.False(RenderingScorer.Score(_profile, text).IsWord);
    }

    [Fact]
    public void NGramScoreIsTheMeanLogShareOfLetterPairsAndTriples() {
      double expected = (Math.Log(0.1) + Math.Log(0.1) + Math.Log(0.01)) / 3;

      Assert.Equal(expected, RenderingScorer.NGramScore(_profile, "zor"), 6);
    }

    [Fact]
    public void UnseenLetterGroupsScoreTheFloor() {
      Assert.Equal(Math.Log(1e-6), RenderingScorer.NGramScore(_profile, "qqq"), 6);
    }

    [Fact]
    public void NGramScoreIgnoresCase() {
      Assert.Equal(
          RenderingScorer.NGramScore(_profile, "zor"), RenderingScorer.NGramScore(_profile, "ZOR"));
    }

    [Fact]
    public void MostFrequentWordScoresTwoAndLeastFrequentScoresOne() {
      Assert.Equal(2, RenderingScorer.Score(_profile, "zorbit").Value, 6);
      Assert.Equal(1, RenderingScorer.Score(_profile, "quandle").Value, 6);
    }

    [Fact]
    public void MoreFrequentWordScoresHigher() {
      Assert.True(
          RenderingScorer.Score(_profile, "plim").Value
          > RenderingScorer.Score(_profile, "quandle").Value);
    }

    [Fact]
    public void ShortWordOutsideTheCommonWordsGetsNoRankBonus() {
      Assert.Equal(1, RenderingScorer.Score(_profile, "ka").Value, 6);
    }

    [Fact]
    public void NonWordScoresBelowOneAndRisesWithLetterFrequencies() {
      double frequent = RenderingScorer.Score(_profile, "zorzor").Value;
      double unseen = RenderingScorer.Score(_profile, "qxqxqx").Value;

      Assert.InRange(unseen, 0, frequent);
      Assert.InRange(frequent, unseen, 1);
      Assert.True(frequent > unseen);
    }
  }
}
