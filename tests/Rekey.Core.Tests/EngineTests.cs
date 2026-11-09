using Rekey.Core.Detection;
using Xunit;

namespace Rekey.Core.Tests {
  public sealed class EngineTests {
    [Fact]
    public void KeyDownYieldsNoActions() {
      var keyDown = new KeyDown(
          TimeSpan.FromSeconds(1), new Keystroke(0x10, false, false), KeyKind.Character, 0);

      IReadOnlyList<EngineAction> actions = new Engine().Handle(keyDown);

      Assert.Empty(actions);
    }
  }
}
