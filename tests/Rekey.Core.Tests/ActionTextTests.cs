using Xunit;

namespace Rekey.Core.Tests {
  // Privacy guardrail: actions that carry typed text print only its length.
  public sealed class ActionTextTests {
    [Fact]
    public void ReplaceTextPrintsLengthsOnly() {
      string text = new ReplaceText("ghbdtn", "привет").ToString();

      Assert.DoesNotContain("ghbdtn", text, StringComparison.Ordinal);
      Assert.DoesNotContain("привет", text, StringComparison.Ordinal);
      Assert.Contains("6", text, StringComparison.Ordinal);
    }

    [Fact]
    public void UndoPerformedPrintsLengthsOnly() {
      string text = new UndoPerformed("ghbdtn", "привет").ToString();

      Assert.DoesNotContain("ghbdtn", text, StringComparison.Ordinal);
      Assert.DoesNotContain("привет", text, StringComparison.Ordinal);
    }
  }
}
