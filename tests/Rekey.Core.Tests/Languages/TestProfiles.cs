using System.Text;
using Rekey.Core.Languages;

namespace Rekey.Core.Tests.Languages {
  internal static class TestProfiles {
    public static readonly LayoutId EnglishUs = new(0x00000409);
    public static readonly LayoutId EnglishInternational = new(0x00020409);
    public static readonly LayoutId Russian = new(0x00000419);
    public static readonly LayoutId Ukrainian = new(0x00000422);
    public static readonly LayoutId UkrainianEnhanced = new(0x00020422);
    public static readonly LayoutId German = new(0x00000407);

    // Invented words only; layout ids follow the shipped profiles.
    public static string Json(string code, params string[] layoutIds) {
      string ids = string.Join(", ", layoutIds.Select(id => $"\"{id}\""));
      return $$"""
          {
            "format": 1, "code": "{{code}}", "displayName": "{{code}}",
            "layoutIds": [{{ids}}], "alphabet": "x",
            "words1": [], "words2": [], "commonWords": ["zorbit"],
            "bigrams": {}, "trigrams": {}
          }
          """;
    }

    public static Stream Stream(string json) {
      return new MemoryStream(Encoding.UTF8.GetBytes(json));
    }

    public static LanguageProfile Load(string json) {
      using Stream stream = Stream(json);
      return LanguageProfileLoader.Load(stream);
    }

    public static LanguageSet ThreeLanguages() {
      return new LanguageSet([
          Load(Json("en", "00000409")),
          Load(Json("ru", "00000419")),
          Load(Json("uk", "00000422", "00020422"))]);
    }
  }
}
