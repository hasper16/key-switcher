using Rekey.Core.Detection;
using Rekey.Core.Languages;
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

    [Fact]
    public void LayoutChangeSetsTheCurrentLayout() {
      var engine = new Engine();

      engine.Handle(new LayoutChanged(TimeSpan.FromSeconds(1), new LayoutId(0x419)));

      Assert.Equal(new LayoutId(0x419), engine.CurrentLayout);
    }

    [Fact]
    public void LayoutChangeKeepsTheWord() {
      Typist typist = new Typist().Type("ghb");

      typist.Send(new LayoutChanged(TimeSpan.FromSeconds(11), new LayoutId(0x419)));

      Assert.Equal("ghb", typist.Word);
    }
  }
}
