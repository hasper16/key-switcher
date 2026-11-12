using Rekey.Win32.Replacement;
using Xunit;

namespace Rekey.Win32.Tests.Replacement {
  // Says how a read differs from the Word without carrying either text.
  public sealed class ReplacementTraceTests {
    [Fact]
    public void ReadEndingWithTheWordShowsTheMatchingEnd() {
      ReplacementTrace trace = ReplacementTrace.Of("xxghbdtn", "ghbdtn", Facts());

      Assert.Equal((8, 6, 0, 6, true), Shape(trace));
    }

    [Fact]
    public void ReadStartingWithTheWordShowsTheMatchingStart() {
      ReplacementTrace trace = ReplacementTrace.Of("ghbdtn ", "ghbdtn", Facts());

      Assert.Equal((7, 6, 6, 0, true), Shape(trace));
    }

    [Fact]
    public void EmptyReadSharesNothing() {
      ReplacementTrace trace = ReplacementTrace.Of(string.Empty, "ghbdtn", Facts());

      Assert.Equal((0, 6, 0, 0, false), Shape(trace));
    }

    [Fact]
    public void TextFormCarriesNoText() {
      string text = ReplacementTrace.Of("ghbdtn ", "ghbdtn", Facts()).ToString();

      Assert.DoesNotContain("ghbdtn", text, StringComparison.Ordinal);
    }

    private static ReaderFacts Facts() {
      return new ReaderFacts("Chrome", "Chrome_RenderWidgetHostHWND", 50004, -6);
    }

    private static (int Read, int Expected, int Start, int End, bool Inside) Shape(
        ReplacementTrace trace) {
      return (trace.ReadLength, trace.ExpectedLength, trace.SameStart, trace.SameEnd,
          trace.ExpectedInside);
    }
  }
}
