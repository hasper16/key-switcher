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
      var control = new FakeControl("dtnxyz") { Context = "w ghbdtnxyz" };

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
      var control = new FakeControl("dtnxyz") { Context = "w ghbdtnxyz" };

      ReplacementMethod method =
          VerifiedReplacement.Prepare(control, "ghbdtn", () => true, canSelect: false);

      Assert.Equal(ReplacementMethod.Cancel, method);
    }

    [Fact]
    public void ReadEndingWithTheWordButLongerIsRetypedNeverSelected() {
      // A provider that moved by a larger unit than a character returned more than the Word.
      var control = new FakeControl(" ghbdtn");

      Assert.Equal(ReplacementMethod.Retype, VerifiedReplacement.Prepare(control, "ghbdtn"));
      Assert.False(control.Selected);
    }

    [Fact]
    public void ReadUnrelatedToTheWordIsUnrelated() {
      var control = new FakeControl("abcdef") { Context = "qwe abcdef" };

      Assert.Equal(ReplacementMethod.Unrelated, VerifiedReplacement.Prepare(control, "ghbdtn"));
    }

    [Fact]
    public void ContextIsReadWiderThanTheWord() {
      var control = new FakeControl("abcdef");

      VerifiedReplacement.Prepare(control, "ghbdtn");

      Assert.True(control.AskedContextLength > 6);
    }

    [Fact]
    public void FailedContextReadCancels() {
      var control = new FakeControl("dtnxyz") { ContextUnavailable = true };

      Assert.Equal(ReplacementMethod.Cancel, VerifiedReplacement.Prepare(control, "ghbdtn"));
    }

    [Fact]
    public void ContextReadThatReachesTheLimitCancelsRatherThanCountingAsUnrelated() {
      string longCompletion = "ghbdtn" + new string('x', 40);
      var control = new FakeControl("xxxxxx") { Context = longCompletion };

      Assert.Equal(ReplacementMethod.Cancel, VerifiedReplacement.Prepare(control, "ghbdtn"));
    }

    [Fact]
    public void SameWordEarlierOnTheLineIsUnrelated() {
      // Three Words typed alike on one line: an earlier copy says nothing about this one.
      var control = new FakeControl("abcdef") { Context = "ghbdtn ghbdtn abcdef" };

      Assert.Equal(ReplacementMethod.Unrelated, VerifiedReplacement.Prepare(control, "ghbdtn"));
    }

    [Fact]
    public void WordFollowedBySpaceAndOtherTextIsUnrelated() {
      var control = new FakeControl("tn xyz") { Context = "ghbdtn xyz" };

      Assert.Equal(ReplacementMethod.Unrelated, VerifiedReplacement.Prepare(control, "ghbdtn"));
    }

    [Fact]
    public void LongDocumentWithoutTheWordAtTheCaretIsUnrelated() {
      // A full-length read with spaces near the caret cannot hide an attached completion.
      string text = string.Concat(Enumerable.Repeat("lorem ipsum ", 10));
      var control = new FakeControl("ipsum ") { Context = text };

      Assert.Equal(ReplacementMethod.Unrelated, VerifiedReplacement.Prepare(control, "ghbdtn"));
    }

    [Fact]
    public void EditedRunIsRecognisedByItsLastWord() {
      var control = new FakeControl("yt pyf") { Context = "a yt pyf.xyz" };

      Assert.Equal(ReplacementMethod.Cancel, VerifiedReplacement.Prepare(control, "yt pyf."));
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

      public string? Context { get; init; }

      public int AskedContextLength { get; private set; }

      public bool ContextUnavailable { get; init; }

      public int Describes { get; private set; }

      public string? ReadContext(int length) {
        AskedContextLength = length;
        if (ContextUnavailable || (Context ?? _text) is not { } full) {
          return null;
        }

        return full.Length <= length ? full : full[^length..];
      }

      public bool FocusUnchanged() {
        return !FocusMoved;
      }

      public void Deselect(Deselection when) {
        Deselected = when;
      }

      public ReplacementTrace Describe(string expected) {
        Describes++;
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
