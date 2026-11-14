using System.Text.Json.Serialization;

namespace Rekey.Core.Words {
  [JsonSourceGenerationOptions(
      PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
      WriteIndented = true)]
  [JsonSerializable(typeof(WordListsJson.WordListsFile))]
  internal sealed partial class WordListsJsonContext : JsonSerializerContext {
  }
}
