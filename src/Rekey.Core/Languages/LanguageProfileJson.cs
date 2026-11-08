using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Rekey.Core.Languages {
  /// <summary>
  /// Reads and writes Language Profile files.
  /// </summary>
  internal static class LanguageProfileJson {
    // Fixed newline and unescaped letters keep generated files byte-stable on every OS and
    // readable in review.
    private static readonly JsonTypeInfo<LanguageProfileDocument> _typeInfo = CreateTypeInfo();

    /// <summary>
    /// Reads a Language Profile file.
    /// </summary>
    /// <param name="stream">The stream that holds the file.</param>
    /// <returns>The profile as the file stores it.</returns>
    /// <exception cref="JsonException">The stream does not hold a format 1 profile.</exception>
    public static LanguageProfileDocument Read(Stream stream) {
      return JsonSerializer.Deserialize(stream, _typeInfo)
          ?? throw new JsonException("The profile is null.");
    }

    /// <summary>
    /// Returns the file text of a Language Profile, ending with a newline.
    /// </summary>
    /// <param name="document">The profile to write.</param>
    /// <returns>The file text.</returns>
    public static string Write(LanguageProfileDocument document) {
      return JsonSerializer.Serialize(document, _typeInfo) + "\n";
    }

    /// <summary>
    /// Returns the file name of a Language's profile, such as <c>ru.json</c>.
    /// </summary>
    /// <param name="language">The Language.</param>
    /// <returns>The file name.</returns>
    public static string FileNameOf(Language language) {
      string quotedCode = JsonSerializer.Serialize(
          language, LanguageProfileJsonContext.Default.Language);
      return $"{quotedCode.Trim('"')}.json";
    }

    private static JsonTypeInfo<LanguageProfileDocument> CreateTypeInfo() {
      var options = new JsonSerializerOptions(LanguageProfileJsonContext.Default.Options) {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        NewLine = "\n",
        WriteIndented = true,
      };
      return (JsonTypeInfo<LanguageProfileDocument>)options.GetTypeInfo(
          typeof(LanguageProfileDocument));
    }
  }
}
