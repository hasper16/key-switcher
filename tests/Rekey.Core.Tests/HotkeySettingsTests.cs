using System.Text.Json;
using Xunit;

namespace Rekey.Core.Tests {
  // Spec manual-correction: "Hotkey choices".
  public sealed class HotkeySettingsTests {
    private const ushort _keyK = 0x4B;

    public static TheoryData<Hotkey> Presets => [
        Hotkey.DoubleShift, Hotkey.DoubleCtrl, Hotkey.DoubleAlt, Hotkey.CtrlSpace,
        Hotkey.WinSpace];

    [Fact]
    public void FreshSettingsUseADoubleTapOfShift() {
      Assert.Equal(Hotkey.DoubleShift, new Settings().Hotkey);
    }

    [Fact]
    public void FileWithoutHotkeyGetsADoubleTapOfShift() {
      Settings? restored = JsonSerializer.Deserialize(
          "{ \"autoCorrectionEnabled\": true }", SettingsJsonContext.Default.Settings);

      Assert.Equal(Hotkey.DoubleShift, restored?.Hotkey);
    }

    [Theory]
    [MemberData(nameof(Presets))]
    public void PresetRoundTripsThroughJson(Hotkey hotkey) {
      var settings = new Settings(Hotkey: hotkey);

      Assert.Equal(settings, RoundTrip(settings));
    }

    [Fact]
    public void CustomComboRoundTripsThroughJson() {
      var settings = new Settings(
          Hotkey: Hotkey.Custom(KeyModifiers.Control | KeyModifiers.Alt, _keyK));

      Assert.Equal(settings, RoundTrip(settings));
    }

    [Fact]
    public void JsonNamesAPreset() {
      using JsonDocument document = Serialize(new Settings(Hotkey: Hotkey.WinSpace));

      Assert.Equal("winSpace", document.RootElement.GetProperty("hotkey").GetString());
    }

    [Fact]
    public void JsonSpellsOutACustomCombo() {
      using JsonDocument document =
          Serialize(new Settings(Hotkey: Hotkey.Custom(KeyModifiers.Shift, _keyK)));

      JsonElement hotkey = document.RootElement.GetProperty("hotkey");
      Assert.Equal(["shift"], hotkey.GetProperty("modifiers").EnumerateArray()
          .Select(name => name.GetString()));
      Assert.Equal(_keyK, hotkey.GetProperty("virtualKey").GetUInt16());
    }

    [Fact]
    public void SpacePresetsAreCombosOfSpace() {
      Assert.Equal((KeyModifiers.Control, Hotkey.SpaceKey), Combo(Hotkey.CtrlSpace));
      Assert.Equal((KeyModifiers.Windows, Hotkey.SpaceKey), Combo(Hotkey.WinSpace));
    }

    [Fact]
    public void DoubleTapsNameTheirModifier() {
      Assert.Equal(KeyModifiers.Shift, Hotkey.DoubleShift.TappedModifier);
      Assert.Equal(KeyModifiers.Control, Hotkey.DoubleCtrl.TappedModifier);
      Assert.Equal(KeyModifiers.Alt, Hotkey.DoubleAlt.TappedModifier);
      Assert.Equal(KeyModifiers.None, Hotkey.CtrlSpace.TappedModifier);
    }

    [Theory]
    [InlineData(KeyModifiers.None, _keyK)]
    [InlineData(KeyModifiers.Control, 0)]
    [InlineData(KeyModifiers.Control, 0x10)]
    [InlineData(KeyModifiers.Control, 0xA2)]
    [InlineData(KeyModifiers.Alt, 0x5B)]
    [InlineData(KeyModifiers.Alt, 0x100)]
    public void CustomComboNeedsAModifierAndOneOtherKey(KeyModifiers modifiers, int key) {
      Assert.Throws<ArgumentException>(() => Hotkey.Custom(modifiers, (ushort)key));
    }

    [Theory]
    [InlineData("\"tripleShift\"")]
    [InlineData("{ \"modifiers\": [\"shift\"], \"virtualKey\": 16 }")]
    [InlineData("{ \"modifiers\": [\"hyper\"], \"virtualKey\": 75 }")]
    [InlineData("{ \"modifiers\": [\"shift\"] }")]
    public void UnreadableHotkeyMakesTheFileUnreadable(string hotkey) {
      Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(
          $"{{ \"hotkey\": {hotkey} }}", SettingsJsonContext.Default.Settings));
    }

    private static (KeyModifiers Modifiers, ushort Key) Combo(Hotkey hotkey) {
      return (hotkey.Modifiers, hotkey.VirtualKey);
    }

    private static Settings? RoundTrip(Settings settings) {
      string json = JsonSerializer.Serialize(settings, SettingsJsonContext.Default.Settings);
      return JsonSerializer.Deserialize(json, SettingsJsonContext.Default.Settings);
    }

    private static JsonDocument Serialize(Settings settings) {
      return JsonDocument.Parse(
          JsonSerializer.Serialize(settings, SettingsJsonContext.Default.Settings));
    }
  }
}
