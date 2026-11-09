using Rekey.Core.Detection;
using Xunit;

namespace Rekey.Core.Tests {
  public sealed class KeyDownTests {
    [Fact]
    public void TextFormOmitsTheKeystroke() {
      var keyDown = new KeyDown(
          TimeSpan.FromSeconds(1),
          new Keystroke(0x27, Shift: true, CapsLock: false),
          KeyKind.Character,
          KeyModifiers.Shift);

      string text = keyDown.ToString();

      Assert.DoesNotContain("Keystroke", text, StringComparison.Ordinal);
      Assert.DoesNotContain("39", text, StringComparison.Ordinal);
      Assert.Contains("Shift", text, StringComparison.Ordinal);
    }
  }
}
