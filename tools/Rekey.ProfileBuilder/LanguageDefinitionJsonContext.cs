using System.Text.Json.Serialization;

namespace Rekey.ProfileBuilder {
  [JsonSourceGenerationOptions(
      PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
      RespectNullableAnnotations = true,
      RespectRequiredConstructorParameters = true)]
  [JsonSerializable(typeof(LanguageDefinition))]
  internal sealed partial class LanguageDefinitionJsonContext : JsonSerializerContext {
  }
}
