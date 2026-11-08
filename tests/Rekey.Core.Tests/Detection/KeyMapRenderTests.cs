using Rekey.Core.Detection;
using Rekey.Core.Languages;
using Xunit;

namespace Rekey.Core.Tests.Detection {
  public sealed class KeyMapRenderTests {
    [Theory]
    [InlineData("ghbdtn", "привет")]
    [InlineData(",tp", "без")]
    [InlineData("Ghbdtn", "Привет")]
    [InlineData("ghbdtn?", "привет,")]
    public void RendersEnglishKeystrokesInRussian(string typed, string expected) {
      IReadOnlyList<Keystroke> word = Type(KeyMapFixtures.EnglishUs, typed);

      Assert.Equal(expected, KeyMapFixtures.Russian.Render(word));
    }

    [Fact]
    public void CapsLockRendersCapitals() {
      IReadOnlyList<Keystroke>? word = KeyMapFixtures.EnglishUs.Type("GHBDTN", capsLock: true);

      Assert.NotNull(word);
      Assert.All(word, keystroke => Assert.False(keystroke.Shift));
      Assert.Equal("ПРИВЕТ", KeyMapFixtures.Russian.Render(word));
    }

    [Fact]
    public void ShiftWithCapsLockRendersSmallLetters() {
      IReadOnlyList<Keystroke> word = [new(0x22, Shift: true, CapsLock: true)];

      Assert.Equal("п", KeyMapFixtures.Russian.Render(word));
    }

    [Fact]
    public void CapsLockLeavesDigitKeysAlone() {
      IReadOnlyList<Keystroke> word = [new(0x02, Shift: false, CapsLock: true)];

      Assert.Equal("1", KeyMapFixtures.Russian.Render(word));
    }

    [Fact]
    public void RendersTheSameKeystrokesInEveryLayout() {
      IReadOnlyList<Keystroke> word = Type(KeyMapFixtures.Ukrainian, "привіт");

      Assert.Equal("привыт", KeyMapFixtures.Russian.Render(word));
      Assert.Equal("ghbdsn", KeyMapFixtures.EnglishUs.Render(word));
    }

    [Fact]
    public void DeadKeyIsNotRendered() {
      var map = new KeyMap(new LayoutId(0x00000407), [
          new KeyMapEntry(0x29, "^", "°", false, DeadNormal: true, DeadShifted: false),
          new KeyMapEntry(0x10, "q", "Q", true, false, false)]);

      Assert.Equal("°q", map.Render([new(0x29, true, false), new(0x10, false, false)]));
      Assert.Null(map.Render([new(0x29, false, false), new(0x10, false, false)]));
    }

    [Fact]
    public void AltGrKeystrokeIsNotRendered() {
      Assert.Null(KeyMapFixtures.Russian.Render([new(0x16, false, false, AltGr: true)]));
    }

    [Fact]
    public void UnknownKeyIsNotRendered() {
      Assert.Null(KeyMapFixtures.Russian.Render([new(0x56, false, false)]));
    }

    [Fact]
    public void TextWithAKeyTheLayoutLacksCannotBeTyped() {
      Assert.Null(KeyMapFixtures.EnglishUs.Type("п", capsLock: false));
    }

    private static IReadOnlyList<Keystroke> Type(KeyMap map, string text) {
      return map.Type(text, capsLock: false) ?? throw new ArgumentException(text);
    }
  }
}
