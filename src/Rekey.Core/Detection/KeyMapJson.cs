using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Rekey.Core.Languages;

namespace Rekey.Core.Detection {
  /// <summary>
  /// Reads and writes Key Map files, the form in which captured Layouts are kept as test data.
  /// </summary>
  internal static class KeyMapJson {
    private static readonly JsonTypeInfo<KeyMapDocument> _typeInfo = CreateTypeInfo();

    /// <summary>
    /// Reads a Key Map file.
    /// </summary>
    /// <param name="stream">The stream that holds the file.</param>
    /// <returns>The Key Map.</returns>
    /// <exception cref="InvalidDataException">
    /// The stream does not hold a valid Key Map: malformed JSON, a missing field, an invalid KLID
    /// or a repeated scan code.
    /// </exception>
    public static KeyMap Read(Stream stream) {
      KeyMapDocument document;
      try {
        document = JsonSerializer.Deserialize(stream, _typeInfo)
            ?? throw new InvalidDataException("The Key Map is null.");
      } catch (JsonException e) {
        throw new InvalidDataException("The Key Map is not valid JSON.", e);
      }

      if (!LayoutId.TryParse(document.LayoutId, out LayoutId layout)) {
        throw new InvalidDataException("The Layout id is not an 8-digit KLID.");
      }

      try {
        return new KeyMap(layout, document.Keys);
      } catch (ArgumentException e) {
        throw new InvalidDataException("A scan code is listed twice.", e);
      }
    }

    /// <summary>
    /// Returns the file text of a Key Map, ending with a newline.
    /// </summary>
    /// <param name="map">The Key Map to write.</param>
    /// <returns>The file text.</returns>
    public static string Write(KeyMap map) {
      string layoutId = map.Layout.Value.ToString("X8", CultureInfo.InvariantCulture);
      return JsonSerializer.Serialize(new KeyMapDocument(layoutId, map.Entries), _typeInfo)
          + "\n";
    }

    private static JsonTypeInfo<KeyMapDocument> CreateTypeInfo() {
      var options = new JsonSerializerOptions(KeyMapJsonContext.Default.Options) {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        NewLine = "\n",
        WriteIndented = true,
      };
      return (JsonTypeInfo<KeyMapDocument>)options.GetTypeInfo(typeof(KeyMapDocument));
    }
  }
}
