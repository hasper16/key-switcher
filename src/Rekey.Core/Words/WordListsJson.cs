using System.Text.Json;

namespace Rekey.Core.Words {
  /// <summary>
  /// Reads and writes the <c>words.json</c> contract (design D1 of add-word-learning).
  /// </summary>
  internal static class WordListsJson {
    private const int _version = 1;

    public static byte[] Write(WordListsDocument document) {
      var file = new WordListsFile(_version, [.. document.Protected], [.. document.Learned]);
      return JsonSerializer.SerializeToUtf8Bytes(file, WordListsJsonContext.Default.WordListsFile);
    }

    // Null when the text is not a version 1 file; entries without text are skipped.
    public static WordListsDocument? Parse(ReadOnlySpan<byte> json) {
      WordListsFile? file;
      try {
        file = JsonSerializer.Deserialize(json, WordListsJsonContext.Default.WordListsFile);
      } catch (JsonException) {
        return null;
      }

      if (file is not { Version: _version, Protected: { } words, Learned: { } learned }) {
        return null;
      }

      return new WordListsDocument(
          [.. words.Where(word => !string.IsNullOrEmpty(word?.Text))],
          [.. learned.Where(word => !string.IsNullOrEmpty(word?.Text))]);
    }

    internal sealed record WordListsFile(
        int Version, List<ProtectedWord>? Protected, List<LearnedWord>? Learned);
  }
}
