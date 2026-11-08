using Xunit;

namespace Rekey.Core.Tests {
  public sealed class KeyDownTests {
    [Fact]
    public void TextFormOmitsTheTypedCharacter() {
      var keyDown = new KeyDown(TimeSpan.FromSeconds(1), 'ж', KeyModifiers.Shift);

      string text = keyDown.ToString();

      Assert.DoesNotContain("ж", text, StringComparison.Ordinal);
      Assert.Contains("Shift", text, StringComparison.Ordinal);
    }
  }
}
