using Rekey.Core;
using Rekey.Core.Detection;
using Rekey.Win32.Keyboard;
using Xunit;

namespace Rekey.Win32.Tests.Keyboard {
  public sealed class RawInputTranslatorTests {
    private const ushort _vkShiftLeft = 0xA0;
    private const ushort _vkControlLeft = 0xA2;
    private const ushort _vkAltRight = 0xA5;
    private const ushort _vkCapsLock = 0x14;

    private long _tick = 1000;

    [Fact]
    public void LetterBecomesACharacterKeyDown() {
      IReadOnlyList<InputEvent> events = Translate(Down(0x47, 0x22), Up(0x47, 0x22));

      Assert.Equal([Character(1001, 0x22)], events);
    }

    [Fact]
    public void ShiftIsReportedOnTheKeystroke() {
      IReadOnlyList<InputEvent> events = Translate(
          Down(_vkShiftLeft, 0x2A), Down(0x47, 0x22), Up(_vkShiftLeft, 0x2A), Down(0x47, 0x22));

      Assert.Equal(
          [
            new KeyDown(At(1001), new(0x2A, true, false), KeyKind.Modifier, KeyModifiers.Shift),
            Character(1002, 0x22, shift: true, modifiers: KeyModifiers.Shift),
            Character(1004, 0x22),
          ],
          events);
    }

    [Fact]
    public void ControlChordCarriesTheModifier() {
      IReadOnlyList<InputEvent> events = Translate(Down(_vkControlLeft, 0x1D), Down(0x43, 0x2E));

      Assert.Equal(Character(1002, 0x2E, modifiers: KeyModifiers.Control), events[1]);
    }

    [Fact]
    public void AltGrIsReportedOnTheKeystrokeNotAsCtrlAndAlt() {
      // Windows sends a fake left Ctrl with scan code 0x21D ahead of AltGr.
      IReadOnlyList<InputEvent> events = Translate(
          Down(_vkControlLeft, 0x21D), Down(_vkAltRight, 0x38, extended: true), Down(0x51, 0x10));

      Assert.Equal(Character(1003, 0x10, altGr: true), events[2]);
    }

    [Fact]
    public void CapsLockTogglesOnPressNotOnRepeat() {
      IReadOnlyList<InputEvent> events = Translate(
          Down(_vkCapsLock, 0x3A),
          Down(_vkCapsLock, 0x3A),
          Up(_vkCapsLock, 0x3A),
          Down(0x47, 0x22),
          Down(_vkCapsLock, 0x3A),
          Up(_vkCapsLock, 0x3A),
          Down(0x47, 0x22));

      Assert.Equal(KeyKind.Other, ((KeyDown)events[0]).Kind);
      Assert.Equal(Character(1004, 0x22, capsLock: true), events[2]);
      Assert.Equal(Character(1007, 0x22), events[4]);
    }

    [Theory]
    [InlineData(0x08, 0x0E, false, KeyKind.Backspace)]
    [InlineData(0x09, 0x0F, false, KeyKind.Tab)]
    [InlineData(0x0D, 0x1C, false, KeyKind.Enter)]
    [InlineData(0x0D, 0x1C, true, KeyKind.Enter)]
    [InlineData(0x20, 0x39, false, KeyKind.Space)]
    [InlineData(0x25, 0x4B, true, KeyKind.Navigation)]
    [InlineData(0x26, 0x48, true, KeyKind.Navigation)]
    [InlineData(0x27, 0x4D, true, KeyKind.Navigation)]
    [InlineData(0x28, 0x50, true, KeyKind.Navigation)]
    [InlineData(0x24, 0x47, true, KeyKind.Navigation)]
    [InlineData(0x23, 0x4F, true, KeyKind.Navigation)]
    [InlineData(0x21, 0x49, true, KeyKind.Navigation)]
    [InlineData(0x22, 0x51, true, KeyKind.Navigation)]
    [InlineData(0x2E, 0x53, true, KeyKind.Navigation)]
    [InlineData(0x1B, 0x01, false, KeyKind.Navigation)]
    [InlineData(0x65, 0x4C, false, KeyKind.Character)]
    [InlineData(0x6B, 0x4E, false, KeyKind.Character)]
    [InlineData(0x31, 0x02, false, KeyKind.Character)]
    [InlineData(0xBE, 0x34, false, KeyKind.Character)]
    [InlineData(0x74, 0x3F, false, KeyKind.Other)]
    [InlineData(0x2D, 0x52, true, KeyKind.Other)]
    [InlineData(0x5B, 0x5B, true, KeyKind.Modifier)]
    [InlineData(0xA1, 0x36, true, KeyKind.Modifier)]
    [InlineData(0xA3, 0x1D, true, KeyKind.Modifier)]
    [InlineData(0xA4, 0x38, false, KeyKind.Modifier)]
    public void KeysAreClassified(int virtualKey, int scanCode, bool extended, KeyKind kind) {
      IReadOnlyList<InputEvent> events =
          Translate(Down((ushort)virtualKey, (ushort)scanCode, extended));

      Assert.Equal(kind, Assert.IsType<KeyDown>(Assert.Single(events)).Kind);
    }

    [Fact]
    public void KeyOnlyAnotherActiveLayoutTypesIsACharacter() {
      // The key left of 1 types "ё" in Russian; the translator knows only Russian here.
      var translator = new RawInputTranslator(capsLockOn: false);
      translator.UseKeyMaps([KeyMapFixtures.Russian]);

      InputEvent? keyDown = translator.Translate(Down(0xC0, 0x29));

      Assert.Equal(KeyKind.Character, Assert.IsType<KeyDown>(keyDown).Kind);
    }

    [Fact]
    public void MouseButtonBecomesAClick() {
      IReadOnlyList<InputEvent> events =
          Translate(new RawInput(RawInputKind.MouseDown, 0, 0, false, 1500));

      Assert.Equal([new MouseClick(TimeSpan.FromMilliseconds(1500))], events);
    }

    private static TimeSpan At(long tick) {
      return TimeSpan.FromMilliseconds(tick);
    }

    private static KeyDown Character(
        long tick,
        ushort scanCode,
        bool shift = false,
        bool capsLock = false,
        bool altGr = false,
        KeyModifiers modifiers = KeyModifiers.None) {
      return new KeyDown(
          At(tick), new Keystroke(scanCode, shift, capsLock, altGr), KeyKind.Character, modifiers);
    }

    private static IReadOnlyList<InputEvent> Translate(params RawInput[] inputs) {
      var translator = new RawInputTranslator(capsLockOn: false);
      translator.UseKeyMaps([KeyMapFixtures.EnglishUs, KeyMapFixtures.Russian]);
      return [.. inputs.Select(input => translator.Translate(input)).OfType<InputEvent>()];
    }

    private RawInput Down(ushort virtualKey, ushort scanCode, bool extended = false) {
      return new RawInput(RawInputKind.KeyDown, virtualKey, scanCode, extended, ++_tick);
    }

    private RawInput Up(ushort virtualKey, ushort scanCode) {
      return new RawInput(RawInputKind.KeyUp, virtualKey, scanCode, false, ++_tick);
    }
  }
}
