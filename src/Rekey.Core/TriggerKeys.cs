using System.Text.Json.Serialization;

namespace Rekey.Core {
  /// <summary>
  /// Specifies which keys are Trigger Keys.
  /// </summary>
  [Flags]
  [JsonConverter(typeof(JsonStringEnumConverter<TriggerKeys>))]
  public enum TriggerKeys {
    /// <summary>No key.</summary>
    [JsonStringEnumMemberName("none")]
    None = 0,

    /// <summary>Space.</summary>
    [JsonStringEnumMemberName("space")]
    Space = 1,

    /// <summary>Enter on the main keyboard or the numeric keypad.</summary>
    [JsonStringEnumMemberName("enter")]
    Enter = 2,

    /// <summary>Tab.</summary>
    [JsonStringEnumMemberName("tab")]
    Tab = 4,
  }
}
