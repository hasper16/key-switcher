using System.Text.Json.Serialization;

namespace Rekey.Core {
  [JsonSourceGenerationOptions(
      PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
      WriteIndented = true)]
  [JsonSerializable(typeof(Settings))]
  internal sealed partial class SettingsJsonContext : JsonSerializerContext {
  }
}
