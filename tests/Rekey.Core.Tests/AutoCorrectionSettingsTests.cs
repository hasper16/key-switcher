using System.Text.Json;
using Rekey.Core.Detection;
using Xunit;

namespace Rekey.Core.Tests {
  // Spec auto-correction: the settings of Auto Correction and their defaults (design D6).
  public sealed class AutoCorrectionSettingsTests {
    private static readonly DateTimeOffset _until = new(2026, 11, 12, 18, 30, 0, TimeSpan.Zero);

    [Fact]
    public void AutoCorrectionIsOnByDefault() {
      Assert.True(new Settings().AutoCorrectionEnabled);
    }

    [Fact]
    public void SpaceAndEnterAreTriggerKeysByDefaultAndTabIsNot() {
      Assert.Equal(TriggerKeys.Space | TriggerKeys.Enter, new Settings().TriggerKeys);
    }

    [Fact]
    public void NoAppIsExcludedByDefault() {
      Assert.Empty(new Settings().ExcludedApps.Entries);
    }

    [Fact]
    public void NothingIsPausedByDefault() {
      Assert.Null(new Settings().PausedUntil);
    }

    [Fact]
    public void WordsAfterAManualLayoutSwitchAreSkippedByDefault() {
      Assert.True(new Settings().SkipAfterManualLayoutSwitch);
    }

    [Fact]
    public void ChangedAutoCorrectionSettingsRoundTripThroughJson() {
      var settings = new Settings(
          AutoCorrectionEnabled: false,
          TriggerKeys: TriggerKeys.Tab | TriggerKeys.Space,
          ExcludedApps: new ExcludedAppList([@"C:\Apps\Zorbit.exe", "Zorbit.Pack_abc!App"]),
          PausedUntil: _until,
          SkipAfterManualLayoutSwitch: false);

      Settings? restored = RoundTrip(settings);

      Assert.Equal(settings, restored);
      Assert.Equal(TriggerKeys.Tab | TriggerKeys.Space, restored?.TriggerKeys);
      Assert.Equal(_until, restored?.PausedUntil);
      Assert.Equal(2, restored?.ExcludedApps.Entries.Count);
    }

    [Fact]
    public void NoTriggerKeyRoundTripsThroughJson() {
      Settings? restored = RoundTrip(new Settings(TriggerKeys: TriggerKeys.None));

      Assert.Equal(TriggerKeys.None, restored?.TriggerKeys);
    }

    [Fact]
    public void JsonNamesTheTriggerKeysAndListsTheExcludedApps() {
      var settings = new Settings(
          TriggerKeys: TriggerKeys.Space | TriggerKeys.Tab,
          ExcludedApps: new ExcludedAppList([@"C:\Apps\Zorbit.exe"]));

      string json = JsonSerializer.Serialize(settings, SettingsJsonContext.Default.Settings);

      using JsonDocument document = JsonDocument.Parse(json);
      Assert.Equal("space, tab", document.RootElement.GetProperty("triggerKeys").GetString());
      JsonElement apps = document.RootElement.GetProperty("excludedApps");
      Assert.Equal([@"C:\Apps\Zorbit.exe"], apps.EnumerateArray().Select(app => app.GetString()));
    }

    [Fact]
    public void FileWithoutAutoCorrectionSettingsGetsTheDefaults() {
      Settings? restored = JsonSerializer.Deserialize(
          "{ \"sensitivity\": \"cautious\" }", SettingsJsonContext.Default.Settings);

      Assert.Equal(new Settings(Sensitivity: Sensitivity.Cautious), restored);
    }

    [Theory]
    [InlineData("{ \"excludedApps\": \"C:\\\\Apps\\\\Zorbit.exe\" }")]
    [InlineData("{ \"excludedApps\": [7] }")]
    [InlineData("{ \"triggerKeys\": \"escape\" }")]
    public void MalformedAutoCorrectionSettingsAreRejected(string json) {
      Assert.Throws<JsonException>(
          () => JsonSerializer.Deserialize(json, SettingsJsonContext.Default.Settings));
    }

    private static Settings? RoundTrip(Settings settings) {
      string json = JsonSerializer.Serialize(settings, SettingsJsonContext.Default.Settings);
      return JsonSerializer.Deserialize(json, SettingsJsonContext.Default.Settings);
    }
  }
}
