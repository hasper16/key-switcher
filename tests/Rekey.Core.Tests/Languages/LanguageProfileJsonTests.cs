using System.Text;
using Rekey.Core.Languages;
using Xunit;

namespace Rekey.Core.Tests.Languages {
  public sealed class LanguageProfileJsonTests {
    // Invented words only; the shape matches design D1 of add-language-profiles.
    private const string _handWritten = """
        {
          "format": 1,
          "code": "uk",
          "displayName": "Ukrainian",
          "layoutIds": [
            "00000422",
            "00020422"
          ],
          "alphabet": "абвгґдеєжзиіїйклмнопрстуфхцчшщьюя",
          "words1": [
            "ї"
          ],
          "words2": [
            "жо"
          ],
          "commonWords": [
            "жо",
            "м'ярк"
          ],
          "bigrams": {
            "жо": 0.0123,
            "яр": 0.5
          },
          "trigrams": {
            "'яр": 1
          }
        }
        """;

    [Fact]
    public void HandWrittenProfileRoundTripsUnchanged() {
      using var input = new MemoryStream(Encoding.UTF8.GetBytes(_handWritten));

      LanguageProfileDocument document = LanguageProfileJson.Read(input);

      Assert.Equal(Language.Ukrainian, document.Code);
      string expected = _handWritten.ReplaceLineEndings("\n") + "\n";
      Assert.Equal(expected, LanguageProfileJson.Write(document));
    }
  }
}
