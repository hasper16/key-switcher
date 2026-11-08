using System.Text.Json.Serialization;

namespace Rekey.Core.Detection {
  [JsonSourceGenerationOptions(
      PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
      RespectNullableAnnotations = true,
      RespectRequiredConstructorParameters = true)]
  [JsonSerializable(typeof(KeyMapDocument))]
  internal sealed partial class KeyMapJsonContext : JsonSerializerContext {
  }
}
