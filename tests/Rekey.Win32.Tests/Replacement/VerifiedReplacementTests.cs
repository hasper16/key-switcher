using Rekey.Win32.Replacement;
using Xunit;

namespace Rekey.Win32.Tests.Replacement {
  // Spec text-replacement "Verified replacement" and design D3, with a fake control.
  public sealed class VerifiedReplacementTests {
    [Fact]
    public void MatchingTextIsSelectedAndTypedOver() {
      var control = new FakeControl("ghbdtn");

      ReplacementMethod method = VerifiedReplacement.Prepare(control, "ghbdtn");

      Assert.Equal(ReplacementMethod.SelectAndType, method);
      Assert.True(control.Selected);
    }

    [Fact]
    public void TextChangedByAutocompleteCancelsTheReplacement() {
      var control = new FakeControl("dtnxyz");

      ReplacementMethod method = VerifiedReplacement.Prepare(control, "ghbdtn");

      Assert.Equal(ReplacementMethod.Cancel, method);
      Assert.False(control.Selected);
    }

    [Fact]
    public void ControlThatExposesNoTextIsRetyped() {
      Assert.Equal(
          ReplacementMethod.Retype, VerifiedReplacement.Prepare(new FakeControl(null), "ghbdtn"));
    }

    [Fact]
    public void ControlThatCannotSelectIsRetyped() {
      var control = new FakeControl("ghbdtn") { CanSelect = false };

      Assert.Equal(ReplacementMethod.Retype, VerifiedReplacement.Prepare(control, "ghbdtn"));
    }

    [Fact]
    public void SelectionThatTimedOutIsAbandoned() {
      // The app may still apply the selection, and Backspaces would then erase too much.
      var control = new FakeControl("ghbdtn") { CanSelect = false, TimedOut = true };

      Assert.Equal(ReplacementMethod.Abandon, VerifiedReplacement.Prepare(control, "ghbdtn"));
    }

    [Fact]
    public void NoReaderMeansRetype() {
      Assert.Equal(ReplacementMethod.Retype, VerifiedReplacement.Prepare(null, "ghbdtn"));
    }

    [Fact]
    public void ReaderIsAskedForTheExpectedLength() {
      var control = new FakeControl("ghbdtn");

      VerifiedReplacement.Prepare(control, "ghbdtn");

      Assert.Equal(6, control.AskedLength);
    }

    internal sealed class FakeControl : ITextBeforeCaret {
      private readonly string? _text;

      public FakeControl(string? text) {
        _text = text;
      }

      public bool CanSelect { get; init; } = true;

      public bool Selected { get; private set; }

      public int AskedLength { get; set; }

      public bool TimedOut { get; init; }

      public string? Read(int length) {
        AskedLength = length;
        return _text;
      }

      public bool SelectRead() {
        Selected = CanSelect;
        return CanSelect;
      }
    }
  }
}
