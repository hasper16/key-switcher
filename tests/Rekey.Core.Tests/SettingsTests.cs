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
    public void VerifiedReplacementDefaultsToOn() {
      Assert.True(new Settings().VerifiedReplacement);
    }

    [Fact]
    public void FileWithoutVerifiedReplacementGetsItOn() {
      Settings? restored = JsonSerializer.Deserialize(
          "{ \"autoCorrectionEnabled\": true }", SettingsJsonContext.Default.Settings);

      Assert.True(restored?.VerifiedReplacement);
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
    public void FeedbackSettingsHaveTheirDefaults() {
      var defaults = new Settings();

      Assert.True(defaults.ShowCursorIndicator);
      Assert.False(defaults.PlaySwitchSound);
      Assert.Equal(SwitchSound.KeyClick, defaults.SwitchSound);
      Assert.False(defaults.LaunchAtSignIn);
    }

    [Fact]
    public void ChangedFeedbackSettingsRoundTripThroughJson() {
      var settings = new Settings(
          ShowCursorIndicator: false,
          PlaySwitchSound: true,
          SwitchSound: SwitchSound.Chime,
          LaunchAtSignIn: true);

      string json = JsonSerializer.Serialize(settings, SettingsJsonContext.Default.Settings);
      Settings? restored = JsonSerializer.Deserialize(json, SettingsJsonContext.Default.Settings);

      Assert.Equal(settings, restored);
    }

    [Fact]
    public void JsonNamesTheSwitchSound() {
      var settings = new Settings(SwitchSound: SwitchSound.SoftTick);

      string json = JsonSerializer.Serialize(settings, SettingsJsonContext.Default.Settings);

      using JsonDocument document = JsonDocument.Parse(json);
      Assert.Equal("softTick", document.RootElement.GetProperty("switchSound").GetString());
    }

    [Fact]
    public void FileWithoutFeedbackSettingsGetsTheDefaults() {
      Settings? restored = JsonSerializer.Deserialize(
          "{ \"autoCorrectionEnabled\": false }", SettingsJsonContext.Default.Settings);

      Assert.True(restored?.ShowCursorIndicator);
      Assert.False(restored?.PlaySwitchSound);
      Assert.Equal(SwitchSound.KeyClick, restored?.SwitchSound);
      Assert.False(restored?.LaunchAtSignIn);
    }

    [Fact]
    public void PauseUntilRestartEndsWithTheSession() {
      var paused = new Settings(PausedUntil: Settings.UntilRestart);

      Assert.Null(paused.ForNewSession().PausedUntil);
    }

    [Fact]
    public void TimedPauseSurvivesANewSession() {
      DateTimeOffset until = new(2026, 11, 14, 12, 30, 0, TimeSpan.Zero);
      var paused = new Settings(PausedUntil: until);

      Assert.Equal(until, paused.ForNewSession().PausedUntil);
    }

    [Fact]
    public void JsonCarriesSchemaVersionOne() {
      string json = JsonSerializer.Serialize(new Settings(), SettingsJsonContext.Default.Settings);

      using JsonDocument document = JsonDocument.Parse(json);
      Assert.Equal(1, document.RootElement.GetProperty("version").GetInt32());
    }
  }
}
