using System.Text.Json.Serialization;

namespace Rekey.Core.Languages {
  [JsonSourceGenerationOptions(
      PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
      RespectNullableAnnotations = true,
      RespectRequiredConstructorParameters = true)]
  [JsonSerializable(typeof(Language))]
  [JsonSerializable(typeof(LanguageProfileDocument))]
  internal sealed partial class LanguageProfileJsonContext : JsonSerializerContext {
  }
}
