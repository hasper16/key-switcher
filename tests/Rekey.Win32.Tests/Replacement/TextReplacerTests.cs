using Rekey.Core;
using Rekey.Win32.Replacement;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Xunit;
using static Rekey.Win32.Tests.Replacement.VerifiedReplacementTests;

namespace Rekey.Win32.Tests.Replacement {
  // Design D3: how a ReplaceText reaches the application.
  public sealed class TextReplacerTests {
    private const string _editor = @"C:\Apps\Editor.exe";
    private readonly List<INPUT[]> _sent = [];

    [Fact]
    public void VerifiedMatchTypesOnlyTheReplacement() {
      TextReplacer replacer = Replacer(new FakeControl("ghbdtn"), verify: true);

      ReplacementResult result = replacer.Replace(new ReplaceText("ghbdtn", "привет"), _editor);

      Assert.Equal(ReplacementResult.Verified, result);
      Assert.Equal(12, Assert.Single(_sent).Length);
    }

    [Fact]
    public void VerifiedReplacementOffRetypesWithoutReading() {
      var control = new FakeControl("ghbdtn");
      TextReplacer replacer = Replacer(control, verify: false);

      ReplacementResult result = replacer.Replace(new ReplaceText("ghbdtn", "привет"), _editor);

      Assert.Equal(ReplacementResult.Retyped, result);
      Assert.Equal(0, control.AskedLength);
      Assert.Equal(24, Assert.Single(_sent).Length);
    }

    [Fact]
    public void MismatchSendsNothing() {
      TextReplacer replacer = Replacer(new FakeControl("dtnxyz"), verify: true);

      ReplacementResult result = replacer.Replace(new ReplaceText("ghbdtn", "привет"), _editor);

      Assert.Equal(ReplacementResult.Mismatch, result);
      Assert.Empty(_sent);
    }

    [Fact]
    public void AppThatRanOutOfTimeTwiceIsRetypedWithoutReading() {
      var control = new FakeControl(null) { TimedOut = true };
      TextReplacer replacer = Replacer(control, verify: true);
      replacer.Replace(new ReplaceText("ghbdtn", "привет"), _editor);
      replacer.Replace(new ReplaceText("ghbdtn", "привет"), _editor);
      control.AskedLength = 0;

      replacer.Replace(new ReplaceText("ghbdtn", "привет"), _editor);

      Assert.Equal(0, control.AskedLength);
    }

    [Fact]
    public void SelectionThatTimedOutIsCollapsedThroughTheAppAndNothingSent() {
      var control = new FakeControl("ghbdtn") {
        CanSelect = false,
        TimedOut = true,
        SelectionPending = true,
      };
      TextReplacer replacer = Replacer(control, verify: true);

      ReplacementResult result = replacer.Replace(new ReplaceText("ghbdtn", "привет"), _editor);

      // The app handles this request after the pending selection, in the control that was
      // read, whatever has focus now.
      Assert.Equal(ReplacementResult.TimedOut, result);
      Assert.Equal(Deselection.Always, control.Deselected);
      Assert.Empty(_sent);
    }

    [Fact]
    public void HoldThatTurnedStaleAfterSelectingTypesNothing() {
      TextReplacer replacer = Replacer(new FakeControl("ghbdtn"), verify: true);
      int checks = 0;

      ReplacementResult result = replacer.Replace(
          new ReplaceText("ghbdtn", "привет"),
          _editor,
          () => ++checks == 1 ? null : ReplacementResult.TooLate);

      // The Word was already selected, so the selection is collapsed back to the caret.
      Assert.Equal(ReplacementResult.TooLate, result);
      Assert.Equal(
          [VIRTUAL_KEY.VK_RIGHT, VIRTUAL_KEY.VK_RIGHT],
          Assert.Single(_sent).Select(input => input.Anonymous.ki.wVk));
    }

    [Fact]
    public void HoldThatTurnedStaleBeforeSelectingSendsNothing() {
      var control = new FakeControl("ghbdtn");
      TextReplacer replacer = Replacer(control, verify: true);

      ReplacementResult result = replacer.Replace(
          new ReplaceText("ghbdtn", "привет"), _editor, () => ReplacementResult.TooLate);

      Assert.Equal(ReplacementResult.TooLate, result);
      Assert.False(control.Selected);
      Assert.Empty(_sent);
    }

    [Fact]
    public void ClickBeforeSelectingSelectsNothing() {
      var control = new FakeControl("ghbdtn");
      TextReplacer replacer = Replacer(control, verify: true);

      ReplacementResult result = replacer.Replace(
          new ReplaceText("ghbdtn", "привет"), _editor, () => ReplacementResult.Interrupted);

      Assert.Equal(ReplacementResult.Interrupted, result);
      Assert.False(control.Selected);
      Assert.Empty(_sent);
    }

    [Fact]
    public void FocusThatMovedAfterSelectingIsCleanedUpInTheReadControl() {
      // Right would land in the newly focused control, so the read control's selection is
      // collapsed through UI Automation instead.
      var control = new FakeControl("ghbdtn");
      TextReplacer replacer = Replacer(control, verify: true);
      int checks = 0;

      ReplacementResult result = replacer.Replace(
          new ReplaceText("ghbdtn", "привет"),
          _editor,
          () => ++checks == 1 ? null : ReplacementResult.FocusMoved);

      Assert.Equal(ReplacementResult.FocusMoved, result);
      Assert.Equal(Deselection.IfStillSelected, control.Deselected);
      Assert.Empty(_sent);
    }

    [Fact]
    public void ClickAfterSelectingCollapsesOnlyASelectionThatIsStillTheWord() {
      // A selection that landed after the click would let the held Space replace the Word; a
      // caret the click placed elsewhere is left where it is.
      var control = new FakeControl("ghbdtn");
      TextReplacer replacer = Replacer(control, verify: true);
      int checks = 0;

      ReplacementResult result = replacer.Replace(
          new ReplaceText("ghbdtn", "привет"),
          _editor,
          () => ++checks == 1 ? null : ReplacementResult.Interrupted);

      Assert.Equal(ReplacementResult.Interrupted, result);
      Assert.Equal(Deselection.IfStillSelected, control.Deselected);
      Assert.Empty(_sent);
    }

    [Fact]
    public void FocusMovedInsideTheWindowDuringTheCheckSendsNothing() {
      TextReplacer replacer = Replacer(new FakeControl("ghbdtn") { FocusMoved = true }, true);

      ReplacementResult result = replacer.Replace(new ReplaceText("ghbdtn", "привет"), _editor);

      Assert.Equal(ReplacementResult.FocusMoved, result);
      Assert.Empty(_sent);
    }

    [Fact]
    public void SelectionThatLandedLateStillTypesAndCountsTowardsSlow() {
      var control = new FakeControl("ghbdtn") { TimedOut = true };
      TextReplacer replacer = Replacer(control, verify: true);

      ReplacementResult first = replacer.Replace(new ReplaceText("ghbdtn", "привет"), _editor);
      replacer.Replace(new ReplaceText("ghbdtn", "привет"), _editor);
      control.AskedLength = 0;
      replacer.Replace(new ReplaceText("ghbdtn", "привет"), _editor);

      Assert.Equal(ReplacementResult.Verified, first);
      Assert.Equal(0, control.AskedLength);
    }

    [Fact]
    public void RefusedInputAfterSelectingCollapsesTheSelection() {
      var control = new FakeControl("ghbdtn");
      var replacer = new TextReplacer(control, true, () => [], _ => false);

      ReplacementResult result = replacer.Replace(new ReplaceText("ghbdtn", "привет"), _editor);

      Assert.Equal(ReplacementResult.Blocked, result);
      Assert.Equal(Deselection.IfStillSelected, control.Deselected);
    }

    [Fact]
    public void UnprovenTargetIsLeftUntouched() {
      // Nothing is erased where typing the Rendering is not proven to arrive.
      var control = new FakeControl("ghbdtn");
      var replacer = new TextReplacer(
          control, true, () => [], Send, () => TextTarget.Unsupported);

      ReplacementResult result = replacer.Replace(new ReplaceText("ghbdtn", "привет"), _editor);

      Assert.Equal(ReplacementResult.Unsupported, result);
      Assert.Equal(0, control.AskedLength);
      Assert.Empty(_sent);
    }

    [Fact]
    public void ConsoleIsCheckedThenRetypedNeverSelected() {
      // A console's UI Automation selection is the terminal's own, which typing ignores.
      var control = new FakeControl("ghbdtn");
      var replacer = new TextReplacer(control, true, () => [], Send, () => TextTarget.Console);

      ReplacementResult result = replacer.Replace(new ReplaceText("ghbdtn", "привет"), _editor);

      Assert.Equal(ReplacementResult.Retyped, result);
      Assert.Equal(6, control.AskedLength);
      Assert.False(control.Selected);
      Assert.Equal(24, Assert.Single(_sent).Length);
    }

    [Fact]
    public void MismatchLeavesATraceOfHowTheReadDiffered() {
      TextReplacer replacer = Replacer(new FakeControl("ghbxyz"), verify: true);

      replacer.Replace(new ReplaceText("ghbdtn", "привет"), _editor);

      Assert.Equal(6, replacer.DescribeMismatch()?.ExpectedLength);
      Assert.Equal(3, replacer.DescribeMismatch()?.SameStart);
    }

    [Fact]
    public void ReplacementThatMatchedLeavesNoMismatchTrace() {
      TextReplacer replacer = Replacer(new FakeControl("ghbdtn"), verify: true);

      replacer.Replace(new ReplaceText("ghbdtn", "привет"), _editor);

      Assert.Null(replacer.DescribeMismatch());
    }

    [Fact]
    public void AppWhoseReadsAreUnrelatedTwiceIsRetypedWithoutReading() {
      var control = new FakeControl("abcdef") { Context = "qwe abcdef" };
      TextReplacer replacer = Replacer(control, verify: true);

      ReplacementResult first = replacer.Replace(new ReplaceText("ghbdtn", "привет"), _editor);
      ReplacementResult second = replacer.Replace(new ReplaceText("ghbdtn", "привет"), _editor);
      control.AskedLength = 0;
      ReplacementResult third = replacer.Replace(new ReplaceText("ghbdtn", "привет"), _editor);

      Assert.Equal(
          (ReplacementResult.Mismatch, ReplacementResult.Mismatch, ReplacementResult.Retyped),
          (first, second, third));
      Assert.Equal(0, control.AskedLength);
    }

    [Fact]
    public void AutocompleteNeverMarksTheAppUnreadable() {
      var control = new FakeControl("dtnxyz") { Context = "w ghbdtnxyz" };
      TextReplacer replacer = Replacer(control, verify: true);

      replacer.Replace(new ReplaceText("ghbdtn", "привет"), _editor);
      replacer.Replace(new ReplaceText("ghbdtn", "привет"), _editor);
      ReplacementResult third = replacer.Replace(new ReplaceText("ghbdtn", "привет"), _editor);

      Assert.Equal(ReplacementResult.Mismatch, third);
      Assert.Empty(_sent);
    }

    [Fact]
    public void FailedContextReadNeverMarksTheAppUnreadable() {
      // Without the wider read Rekey cannot tell an edited Word from unrelated text.
      var control = new FakeControl("dtnxyz") { ContextUnavailable = true };
      TextReplacer replacer = Replacer(control, verify: true);

      replacer.Replace(new ReplaceText("ghbdtn", "привет"), _editor);
      replacer.Replace(new ReplaceText("ghbdtn", "привет"), _editor);
      ReplacementResult third = replacer.Replace(new ReplaceText("ghbdtn", "привет"), _editor);

      Assert.Equal(ReplacementResult.Mismatch, third);
      Assert.Empty(_sent);
    }

    [Fact]
    public void LongAutocompleteThatFillsTheContextWindowNeverMarksTheAppUnreadable() {
      var control = new FakeControl("xxxxxx") { Context = "ghbdtn" + new string('x', 40) };
      TextReplacer replacer = Replacer(control, verify: true);

      replacer.Replace(new ReplaceText("ghbdtn", "привет"), _editor);
      replacer.Replace(new ReplaceText("ghbdtn", "привет"), _editor);
      ReplacementResult third = replacer.Replace(new ReplaceText("ghbdtn", "привет"), _editor);

      Assert.Equal(ReplacementResult.Mismatch, third);
      Assert.Empty(_sent);
    }

    [Fact]
    public void MismatchIsDescribedOnlyWhenAskedSoTypingIsNotDelayed() {
      var control = new FakeControl("ghbxyz");
      TextReplacer replacer = Replacer(control, verify: true);

      replacer.Replace(new ReplaceText("ghbdtn", "привет"), _editor);

      Assert.Equal(0, control.Describes);
      Assert.NotNull(replacer.DescribeMismatch());
      Assert.Equal(1, control.Describes);
    }

    [Fact]
    public void InputThatWindowsRefusesIsReportedAsBlocked() {
      var replacer = new TextReplacer(new FakeControl("ghbdtn"), true, () => [], _ => false);

      ReplacementResult result = replacer.Replace(new ReplaceText("ghbdtn", "привет"), _editor);

      Assert.Equal(ReplacementResult.Blocked, result);
    }

    private bool Send(INPUT[] inputs) {
      _sent.Add(inputs);
      return true;
    }

    private TextReplacer Replacer(ITextBeforeCaret control, bool verify) {
      return new TextReplacer(control, verify, () => [], inputs => {
        _sent.Add(inputs);
        return true;
      });
    }
  }
}
