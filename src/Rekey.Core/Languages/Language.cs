using System.Text.Json.Serialization;

namespace Rekey.Core.Languages {
  /// <summary>
  /// Specifies a natural language Rekey can recognise.
  /// </summary>
  /// <remarks>
  /// Each value names the ISO 639-1 code that a Language Profile file carries.
  /// </remarks>
  [JsonConverter(typeof(JsonStringEnumConverter<Language>))]
  public enum Language {
    /// <summary>English.</summary>
    [JsonStringEnumMemberName("en")]
    English,

    /// <summary>Russian.</summary>
    [JsonStringEnumMemberName("ru")]
    Russian,

    /// <summary>Ukrainian.</summary>
    [JsonStringEnumMemberName("uk")]
    Ukrainian,
  }
}
