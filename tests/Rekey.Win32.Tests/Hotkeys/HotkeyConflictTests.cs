using Rekey.Core;
using Rekey.Win32.Hotkeys;
using Xunit;

namespace Rekey.Win32.Tests.Hotkeys {
  // Spec manual-correction: "Conflict warning"; design D5.
  public sealed class HotkeyConflictTests {
    private const ushort _grave = 0xC0;
    private const ushort _keyK = 0x4B;

    private static readonly KeyChord _winSpace = new(KeyModifiers.Windows, Hotkey.SpaceKey);

    private readonly Dictionary<string, string> _toggle = [];
    private readonly HashSet<KeyChord> _taken = [];

    [Fact]
    public void WinSpaceIsAlwaysAWindowsShortcut() {
      Assert.Equal([_winSpace], Shortcuts());
    }

    [Theory]
    [InlineData("1", KeyModifiers.Alt | KeyModifiers.Shift, 0)]
    [InlineData("2", KeyModifiers.Control | KeyModifiers.Shift, 0)]
    [InlineData("4", KeyModifiers.None, _grave)]
    public void LanguageHotkeyValueNamesItsShortcut(
        string value, KeyModifiers modifiers, int key) {
      _toggle["Language Hotkey"] = value;

      Assert.Contains(new KeyChord(modifiers, (ushort)key), Shortcuts());
    }

    [Fact]
    public void ValueThreeMeansNoShortcut() {
      _toggle["Language Hotkey"] = "3";
      _toggle["Layout Hotkey"] = "3";

      Assert.Equal([_winSpace], Shortcuts());
    }

    [Fact]
    public void LayoutHotkeyIsReadToo() {
      _toggle["Language Hotkey"] = "1";
      _toggle["Layout Hotkey"] = "2";

      Assert.True(Shortcuts().SetEquals([
          _winSpace,
          new KeyChord(KeyModifiers.Alt | KeyModifiers.Shift, 0),
          new KeyChord(KeyModifiers.Control | KeyModifiers.Shift, 0)]));
    }

    [Fact]
    public void OlderHotkeyValueStandsInForTheLanguageHotkey() {
      _toggle["Hotkey"] = "4";

      Assert.Contains(new KeyChord(KeyModifiers.None, _grave), Shortcuts());
    }

    [Theory]
    [InlineData("0")]
    [InlineData("x")]
    public void UnknownValueIsIgnored(string value) {
      _toggle["Language Hotkey"] = value;

      Assert.Equal([_winSpace], Shortcuts());
    }

    [Fact]
    public void CustomWinSpaceTakesOverTheWindowsShortcut() {
      Hotkey hotkey = Hotkey.Custom(KeyModifiers.Windows, Hotkey.SpaceKey);

      Assert.Equal(HotkeyConflict.WindowsLayoutSwitch, Check(hotkey));
    }

    [Fact]
    public void WinSpacePresetTakesOverTheWindowsShortcut() {
      Assert.Equal(HotkeyConflict.WindowsLayoutSwitch, Check(Hotkey.WinSpace));
    }

    [Fact]
    public void ComboAnotherAppRegisteredIsTaken() {
      Hotkey hotkey = Hotkey.Custom(KeyModifiers.Control | KeyModifiers.Alt, _keyK);
      _taken.Add(new KeyChord(KeyModifiers.Control | KeyModifiers.Alt, _keyK));

      Assert.Equal(HotkeyConflict.TakenByAnotherApp, Check(hotkey));
    }

    [Fact]
    public void FreeComboHasNoConflict() {
      Assert.Equal(
          HotkeyConflict.None,
          Check(Hotkey.Custom(KeyModifiers.Control | KeyModifiers.Alt, _keyK)));
    }

    [Fact]
    public void DoubleTapNeverConflicts() {
      _toggle["Language Hotkey"] = "1";

      Assert.Equal(HotkeyConflict.None, Check(Hotkey.DoubleShift));
    }

    private IReadOnlySet<KeyChord> Shortcuts() {
      return LayoutSwitchShortcuts.Parse(name => _toggle.GetValueOrDefault(name));
    }

    private HotkeyConflict Check(Hotkey hotkey) {
      return HotkeyConflicts.Check(hotkey, Shortcuts(), _taken.Contains);
    }
  }
}
