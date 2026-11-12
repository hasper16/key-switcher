using Rekey.Core;
using Rekey.Core.Detection;
using Xunit;

namespace Rekey.DesktopChecks.Tests {
  // One line per Word end for try-corrections, with counts only.
  public sealed class WordEndTracerTests {
    private readonly WordEndTracer _tracer = new();

    [Fact]
    public void CharacterKeysPrintNothing() {
      Assert.Null(_tracer.Add(Key(KeyKind.Character), wordKeys: 1));
    }

    [Fact]
    public void WordEndCountsTheKeysSinceTheLastOne() {
      _tracer.Add(Key(KeyKind.Character), wordKeys: 1);
      _tracer.Add(Key(KeyKind.Character, confirmed: false), wordKeys: 0);
      _tracer.Add(new FocusChanged(TimeSpan.Zero, "app", false, false), wordKeys: 0);

      string? line = _tracer.Add(Key(KeyKind.Space, held: true), wordKeys: 0);

      Assert.Equal(
          "Word end: keys 3, unconfirmed 1, held 1, Word keys before the end 0, focus reports 1",
          line);
    }

    [Fact]
    public void CountsStartAgainAfterAWordEnd() {
      _tracer.Add(Key(KeyKind.Character), wordKeys: 1);
      _tracer.Add(Key(KeyKind.Space), wordKeys: 0);
      _tracer.Add(Key(KeyKind.Character), wordKeys: 1);

      string? line = _tracer.Add(Key(KeyKind.Enter), wordKeys: 0);

      Assert.Equal(
          "Word end: keys 2, unconfirmed 0, held 0, Word keys before the end 1, focus reports 0",
          line);
    }

    private static KeyDown Key(KeyKind kind, bool confirmed = true, bool held = false) {
      return new KeyDown(
          TimeSpan.Zero, new Keystroke(0x22, false, false), kind, 0, confirmed, held);
    }
  }
}
