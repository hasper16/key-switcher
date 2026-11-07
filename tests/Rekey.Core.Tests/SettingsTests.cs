using System.Text.Json;
using Xunit;

namespace Rekey.Core.Tests {
  public sealed class SettingsTests {
    [Fact]
    public void DefaultsRoundTripThroughJsonUnchanged() {
      var defaults = new Settings();

      string json = JsonSerializer.Serialize(defaults, SettingsJsonContext.Default.Settings);
      Settings? restored = JsonSerializer.Deserialize(json, SettingsJsonContext.Default.Settings);

      Assert.Equal(defaults, restored);
    }

    [Fact]
    public void JsonCarriesSchemaVersionOne() {
      string json = JsonSerializer.Serialize(new Settings(), SettingsJsonContext.Default.Settings);

      using JsonDocument document = JsonDocument.Parse(json);
      Assert.Equal(1, document.RootElement.GetProperty("version").GetInt32());
    }
  }
}
