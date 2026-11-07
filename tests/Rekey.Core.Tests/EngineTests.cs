using Xunit;

namespace Rekey.Core.Tests {
  public sealed class EngineTests {
    [Fact]
    public void KeyDownYieldsNoActions() {
      var keyDown = new KeyDown(TimeSpan.FromSeconds(1), 'q', KeyModifiers.None);

      IReadOnlyList<EngineAction> actions =
          Engine.Handle(keyDown, new Context(IsPasswordField: false));

      Assert.Empty(actions);
    }
  }
}
