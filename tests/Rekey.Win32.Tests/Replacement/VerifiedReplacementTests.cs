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
      var control = new FakeControl("ghbdtn") {
        CanSelect = false,
        TimedOut = true,
        SelectionPending = true,
      };

      Assert.Equal(ReplacementMethod.Abandon, VerifiedReplacement.Prepare(control, "ghbdtn"));
    }

    [Fact]
    public void TimeRunningOutBeforeSelectingMeansRetype() {
      var control = new FakeControl("ghbdtn") { CanSelect = false, TimedOut = true };

      Assert.Equal(ReplacementMethod.Retype, VerifiedReplacement.Prepare(control, "ghbdtn"));
    }

    [Fact]
    public void RequestThatTurnedStaleBeforeSelectingSelectsNothing() {
      var control = new FakeControl("ghbdtn");

      ReplacementMethod method =
          VerifiedReplacement.Prepare(control, "ghbdtn", () => false, canSelect: true);

      Assert.Equal(ReplacementMethod.Stale, method);
      Assert.False(control.Selected);
    }

    [Fact]
    public void MatchingTextInAControlWhoseSelectionTypingIgnoresIsRetyped() {
      var control = new FakeControl("ghbdtn");

      ReplacementMethod method =
          VerifiedReplacement.Prepare(control, "ghbdtn", () => true, canSelect: false);

      Assert.Equal(ReplacementMethod.Retype, method);
      Assert.False(control.Selected);
    }

    [Fact]
    public void TextThatDiffersInSuchAControlStillCancels() {
      var control = new FakeControl("dtnxyz");

      ReplacementMethod method =
          VerifiedReplacement.Prepare(control, "ghbdtn", () => true, canSelect: false);

      Assert.Equal(ReplacementMethod.Cancel, method);
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

      public bool SelectionPending { get; init; }

      public bool FocusMoved { get; init; }

      public Deselection? Deselected { get; private set; }

      public bool FocusUnchanged() {
        return !FocusMoved;
      }

      public void Deselect(Deselection when) {
        Deselected = when;
      }

      public ReplacementTrace Describe(string expected) {
        return ReplacementTrace.Of(
            _text ?? string.Empty, expected, new ReaderFacts("Fake", "Fake", 0, 0));
      }

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
