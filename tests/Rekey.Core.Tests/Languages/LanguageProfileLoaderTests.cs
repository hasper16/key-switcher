using System.Text;
using Rekey.Core.Languages;
using Xunit;

namespace Rekey.Core.Tests.Languages {
  public sealed class LanguageProfileLoaderTests {
    [Fact]
    public void ValidProfileLoadsNormalised() {
      LanguageProfile profile = Load(ProfileJson());

      Assert.Equal(Language.Russian, profile.Language);
      Assert.Equal("Russian", profile.DisplayName);
      Assert.True(profile.LayoutIds.SetEquals([new(0x00000419), new(0x00010419)]));
      Assert.True(profile.ContainsShortWord("Ю"));
      Assert.True(profile.ContainsShortWord("ЖЁ"));
      Assert.False(profile.ContainsShortWord("жа"));
      Assert.True(profile.ContainsCommonWord("КРЫВЁЛ"));
      Assert.Equal(1, profile.CommonWordRanks["крывел"]);
      Assert.Equal(0.25f, profile.Bigrams["ры"]);
      Assert.Equal(0.125f, profile.Trigrams["рыв"]);
    }

    [Fact]
    public void LaterDuplicateAfterNormalisingKeepsTheBetterRank() {
      LanguageProfile profile = Load(ProfileJson(
          "\"commonWords\": [\"жо\", \"крывёл\"]",
          "\"commonWords\": [\"Жо\", \"крывёл\", \"ЖО\"]"));

      Assert.Equal(0, profile.CommonWordRanks["жо"]);
    }

    [Theory]
    [InlineData("{ \"format\": 1, ")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("")]
    public void InvalidJsonThrows(string json) {
      Assert.Throws<ProfileLoadException>(() => Load(json));
    }

    [Theory]
    [InlineData("\"words2\": [\"жё\"],")]
    [InlineData("\"displayName\": \"Russian\",")]
    [InlineData("\"bigrams\": { \"ры\": 0.25 },")]
    public void MissingRequiredFieldThrows(string field) {
      Assert.Throws<ProfileLoadException>(() => Load(ProfileJson(field, string.Empty)));
    }

    [Theory]
    [InlineData("\"format\": 1", "\"format\": 2")]
    [InlineData("\"code\": \"ru\"", "\"code\": \"de\"")]
    [InlineData("\"code\": \"ru\"", "\"code\": 7")]
    [InlineData("\"alphabet\": \"абв\"", "\"alphabet\": null")]
    [InlineData("\"00010419\"", "\"10419\"")]
    [InlineData("\"00010419\"", "\"0001041G\"")]
    [InlineData("\"layoutIds\": [\"00000419\", \"00010419\"]", "\"layoutIds\": []")]
    [InlineData("\"words1\": [\"ю\"]", "\"words1\": [\"ю\", null]")]
    [InlineData("\"words2\": [\"жё\"]", "\"words2\": [null]")]
    [InlineData("\"commonWords\": [\"жо\", \"крывёл\"]", "\"commonWords\": [null]")]
    [InlineData("\"00010419\"", "null")]
    [InlineData("{ \"ры\": 0.25 }", "{ \"ры\": 0.25, \"РЫ\": 0.5 }")]
    [InlineData("{ \"рыв\": 0.125 }", "{ \"рыв\": 0.125, \"рЫв\": 0.5 }")]
    public void InvalidValueThrows(string valid, string invalid) {
      Assert.Throws<ProfileLoadException>(() => Load(ProfileJson(valid, invalid)));
    }

    private static LanguageProfile Load(string json) {
      using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
      return LanguageProfileLoader.Load(stream);
    }

    // Invented words only.
    private static string ProfileJson(string? find = null, string? replacement = null) {
      const string json = """
          {
            "format": 1,
            "code": "ru",
            "displayName": "Russian",
            "layoutIds": ["00000419", "00010419"],
            "alphabet": "абв",
            "words1": ["ю"],
            "words2": ["жё"],
            "commonWords": ["жо", "крывёл"],
            "bigrams": { "ры": 0.25 },
            "trigrams": { "рыв": 0.125 }
          }
          """;
      return find is null ? json : json.Replace(find, replacement, StringComparison.Ordinal);
    }
  }
}
