using System.Text.Json.Serialization;

namespace Rekey.Core.Words {
  /// <summary>
  /// Specifies who put an entry on a word list.
  /// </summary>
  [JsonConverter(typeof(JsonStringEnumConverter<WordSource>))]
  public enum WordSource {
    /// <summary>The user added it; it is never dropped to make room.</summary>
    [JsonStringEnumMemberName("user")]
    User,

    /// <summary>Rekey learned it; it can be dropped to make room.</summary>
    [JsonStringEnumMemberName("auto")]
    Auto,
  }
}
