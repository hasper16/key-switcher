using Rekey.Core.Languages;
using Xunit;

namespace Rekey.ProfileBuilder.Tests {
  public sealed class ProfileGeneratorTests {
    private static readonly LanguageDefinition _definition = new(
        Language.English, "English", "abcdefghijklmnopqrstuvwxyz", ["00000409"], "list.txt");

    // Invented words only.
    private static readonly string[] _frequencies = [
        "Zorbit 100",
        "zorbit 50",
        "blim 80",
        "x1y 70",
        "-blim 60",
        "glo-ra 40",
        "q 90",
        "rare 2",
        "broken line",
    ];

    private static readonly string[] _shortWords =
        ["# one and two letters", string.Empty, "Q", "zz", "ab"];

    [Fact]
    public void KeepsOnlyTheTopWords() {
      LanguageProfileDocument document = ProfileGenerator.Build(
          _definition, _frequencies, _shortWords, new BuildOptions(2, 3, 2));

      Assert.Equal(["zorbit", "q"], document.CommonWords);
    }

    [Fact]
    public void BuildsWordsShortWordsAndTopNGrams() {
      LanguageProfileDocument document = ProfileGenerator.Build(
          _definition, _frequencies, _shortWords, new BuildOptions(10, 3, 2));

      Assert.Equal(LanguageProfileDocument.CurrentFormat, document.Format);
      Assert.Equal(Language.English, document.Code);
      Assert.Equal(["00000409"], document.LayoutIds);
      Assert.Equal(["zorbit", "q", "blim", "glo-ra"], document.CommonWords);
      Assert.Equal(["q"], document.Words1);
      Assert.Equal(["ab", "zz"], document.Words2);

      // Bigrams: zorbit gives 5 pairs of weight 150, blim 3 of 80, glo-ra 3 of 40; total 1110.
      Assert.Equal(
          new Dictionary<string, double> { ["bi"] = 0.135135, ["it"] = 0.135135 },
          document.Bigrams);

      // Trigrams: zorbit gives 4 of weight 150, blim 2 of 80, glo-ra 1 of 40; total 800.
      Assert.Equal(
          new Dictionary<string, double> { ["bit"] = 0.1875, ["orb"] = 0.1875 },
          document.Trigrams);
    }

    [Fact]
    public void NGramsStayInsideLetters() {
      LanguageProfileDocument document = ProfileGenerator.Build(
          _definition,
          ["glo-ra 40", "m'pa 40", "-zu 40", "zu' 40"],
          [],
          new BuildOptions(10, 1, 100));

      Assert.Equal(["glo-ra", "m'pa"], document.CommonWords);
      Assert.Equal(["gl", "lo", "pa", "ra"], document.Bigrams.Keys);
      Assert.Equal(["glo"], document.Trigrams.Keys);
    }

    [Fact]
    public void SameInputGivesByteIdenticalFiles() {
      // "flim" ties with "blim", so only a fixed tie-break keeps the order.
      string[] frequencies = [.. _frequencies, "flim 80"];
      var options = new BuildOptions(10, 1, 3);

      string first = Write(frequencies, _shortWords, options);
      string second = Write(frequencies, _shortWords, options);
      string reordered = Write([.. frequencies.Reverse()], [.. _shortWords.Reverse()], options);

      Assert.Equal(first, second);
      Assert.Equal(first, reordered);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("1")]
    public void ShortWordOutsideTheAlphabetOrTooLongThrows(string line) {
      Assert.Throws<InvalidDataException>(() => ProfileGenerator.Build(
          _definition, [], [line], new BuildOptions()));
    }

    private static string Write(string[] frequencies, string[] shortWords, BuildOptions options) {
      return LanguageProfileJson.Write(
          ProfileGenerator.Build(_definition, frequencies, shortWords, options));
    }
  }
}
