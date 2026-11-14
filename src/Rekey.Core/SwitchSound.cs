using System.Text.Json.Serialization;

namespace Rekey.Core {
  /// <summary>
  /// Specifies the sound Rekey plays after a Correction when the switch sound is on.
  /// </summary>
  [JsonConverter(typeof(JsonStringEnumConverter<SwitchSound>))]
  public enum SwitchSound {
    /// <summary>
    /// A short key click.
    /// </summary>
    [JsonStringEnumMemberName("keyClick")]
    KeyClick,

    /// <summary>
    /// A soft tick.
    /// </summary>
    [JsonStringEnumMemberName("softTick")]
    SoftTick,

    /// <summary>
    /// A short chime.
    /// </summary>
    [JsonStringEnumMemberName("chime")]
    Chime,
  }
}
