using System.Text.Json;
using Rekey.Core.Languages;
using Rekey.Core.Words;
using Xunit;

namespace Rekey.Core.Tests.Words {
  // Design D1 of add-word-learning: the words.json contract, version 1.
  public sealed class WordListsJsonTests {
    private static readonly DateTimeOffset _monday = new(2026, 11, 16, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void DocumentSurvivesARoundTrip() {
      var document = new WordListsDocument(
          [new ProtectedWord("zorbit", WordSource.User, _monday)],
          [new LearnedWord("кринж", Language.Russian, WordSource.Auto, _monday)]);

      WordListsDocument read = WordListsJson.Parse(WordListsJson.Write(document))!;

      Assert.Equal(document.Protected, read.Protected);
      Assert.Equal(document.Learned, read.Learned);
    }

    [Fact]
    public void FileUsesTheContractNames() {
      var document = new WordListsDocument(
          [new ProtectedWord("zorbit", WordSource.Auto, _monday)],
          [new LearnedWord("кринж", Language.Russian, WordSource.User, _monday)]);

      using JsonDocument json = JsonDocument.Parse(WordListsJson.Write(document));
      JsonElement root = json.RootElement;

      Assert.Equal(1, root.GetProperty("version").GetInt32());
      JsonElement entry = root.GetProperty("protected")[0];
      Assert.Equal("zorbit", entry.GetProperty("text").GetString());
      Assert.Equal("auto", entry.GetProperty("source").GetString());
      Assert.Equal(_monday, entry.GetProperty("lastUsed").GetDateTimeOffset());
      JsonElement learned = root.GetProperty("learned")[0];
      Assert.Equal("ru", learned.GetProperty("language").GetString());
      Assert.Equal("user", learned.GetProperty("source").GetString());
    }
  }
}
