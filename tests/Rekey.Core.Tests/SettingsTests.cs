using System.Text.Json;
using Rekey.Core.Detection;
using Xunit;

namespace Rekey.Core.Tests {
  public sealed class SettingsTests {
    [Fact]
    public void SensitivityDefaultsToBalanced() {
      Assert.Equal(Sensitivity.Balanced, new Settings().Sensitivity);
    }

    [Fact]
    public void ChangedSensitivityRoundTripsThroughJson() {
      var settings = new Settings(Sensitivity: Sensitivity.Aggressive);

      string json = JsonSerializer.Serialize(settings, SettingsJsonContext.Default.Settings);
      Settings? restored = JsonSerializer.Deserialize(json, SettingsJsonContext.Default.Settings);

      Assert.Equal(settings, restored);
    }

    [Fact]
    public void JsonNamesTheSensitivity() {
      var settings = new Settings(Sensitivity: Sensitivity.Cautious);

      string json = JsonSerializer.Serialize(settings, SettingsJsonContext.Default.Settings);

      using JsonDocument document = JsonDocument.Parse(json);
      Assert.Equal("cautious", document.RootElement.GetProperty("sensitivity").GetString());
    }

    [Fact]
    public void FileWithoutSensitivityGetsBalanced() {
      Settings? restored = JsonSerializer.Deserialize(
          "{ \"autoCorrectionEnabled\": false }", SettingsJsonContext.Default.Settings);

      Assert.Equal(Sensitivity.Balanced, restored?.Sensitivity);
    }

    [Fact]
    public void DefaultsRoundTripThroughJsonUnchanged() {
      var defaults = new Settings();

      string json = JsonSerializer.Serialize(defaults, SettingsJsonContext.Default.Settings);
      Settings? restored = JsonSerializer.Deserialize(json, SettingsJsonContext.Default.Settings);

      Assert.Equal(defaults, restored);
    }

    [Fact]
    public void WatchdogsDefaultToOn() {
      var defaults = new Settings();

      Assert.True(defaults.KeyboardWatchdogEnabled);
      Assert.True(defaults.RestartAfterFreezeEnabled);
    }

    [Fact]
    public void TurnedOffWatchdogsRoundTripThroughJson() {
      var settings = new Settings(KeyboardWatchdogEnabled: false, RestartAfterFreezeEnabled: false);

      string json = JsonSerializer.Serialize(settings, SettingsJsonContext.Default.Settings);
      Settings? restored = JsonSerializer.Deserialize(json, SettingsJsonContext.Default.Settings);

      Assert.Equal(settings, restored);
    }

    [Fact]
    public void FileWithoutWatchdogSettingsTurnsThemOn() {
      Settings? restored = JsonSerializer.Deserialize(
          "{ \"autoCorrectionEnabled\": false }", SettingsJsonContext.Default.Settings);

      Assert.True(restored?.KeyboardWatchdogEnabled);
      Assert.True(restored?.RestartAfterFreezeEnabled);
    }

    [Fact]
    public void JsonCarriesSchemaVersionOne() {
      string json = JsonSerializer.Serialize(new Settings(), SettingsJsonContext.Default.Settings);

      using JsonDocument document = JsonDocument.Parse(json);
      Assert.Equal(1, document.RootElement.GetProperty("version").GetInt32());
    }
  }
}
