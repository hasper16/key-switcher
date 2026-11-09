using System.Text.Json.Serialization;

namespace Rekey.Core.Detection {
  /// <summary>
  /// Specifies how much evidence Rekey needs before correcting a Word.
  /// </summary>
  [JsonConverter(typeof(JsonStringEnumConverter<Sensitivity>))]
  public enum Sensitivity {
    /// <summary>Corrects only on a common-word match and defers otherwise.</summary>
    [JsonStringEnumMemberName("cautious")]
    Cautious,

    /// <summary>
    /// Corrects on a common-word match and defers when only letter-frequency evidence exists.
    /// </summary>
    [JsonStringEnumMemberName("balanced")]
    Balanced,

    /// <summary>Also corrects on strong letter-frequency evidence and on short words.</summary>
    [JsonStringEnumMemberName("aggressive")]
    Aggressive,
  }
}
