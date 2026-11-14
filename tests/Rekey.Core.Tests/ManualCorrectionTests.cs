using Rekey.Core.Detection;
using Rekey.Core.Languages;
using Rekey.Core.Tests.Detection;
using Xunit;

namespace Rekey.Core.Tests {
  // Spec manual-correction, engine side, with the bundled Language Profiles.
  public sealed class ManualCorrectionTests {
    private static readonly LayoutId _english = KeyMapFixtures.EnglishUs.Layout;
    private static readonly LayoutId _russian = KeyMapFixtures.Russian.Layout;
    private static readonly LayoutId _ukrainian = KeyMapFixtures.Ukrainian.Layout;
    private static readonly Settings _off = new(AutoCorrectionEnabled: false);

    [Fact]
    public void HotkeyCorrectsTheCurrentWord() {
      Typist typist = Manual().Type("ghbdtn").Hotkey();

      Assert.Equal([new ReplaceText("ghbdtn", "привет")], typist.Actions);
    }

    [Fact]
    public void AppliedManualCorrectionSwitchesLayout() {
      Typist typist = Manual().Type("ghbdtn").Hotkey().Finish();

      Assert.Equal(
          [new ReplaceText("ghbdtn", "привет"), new SwitchLayout(_russian)], typist.Actions);
    }

    [Fact]
    public void DoubtfulWordAfterSpaceIsCorrectedWithItsSpace() {
      Typist typist = Manual().Type("yt").Press(KeyKind.Space).Hotkey().Finish();

      Assert.Equal(
          [new ReplaceText("yt ", "не "), new SwitchLayout(_russian)], typist.Actions);
    }

    [Fact]
    public void WordAfterTabIsCorrectedWithItsTab() {
      Typist typist = Manual().Type("yt").Press(KeyKind.Tab).Hotkey();

      Assert.Equal([new ReplaceText("yt\t", "не\t")], typist.Actions);
    }

    [Fact]
    public void WordKeptByAutoCorrectionCanStillBeCorrected() {
      Typist typist = new Typist(new Settings()).Type("yt").Hold(KeyKind.Space).Hotkey();

      Assert.Equal([new ReplaceText("yt ", "не ")], typist.Actions);
    }

    [Fact]
    public void WordFollowedByEnterIsLeftAndTheLayoutSwitches() {
      Typist typist = Manual().Type("yt").Press(KeyKind.Enter).Hotkey();

      Assert.Equal([new SwitchLayout(_russian)], typist.Actions);
    }

    [Fact]
    public void WordFollowedByTwoSpacesIsLeft() {
      Typist typist = Manual().Type("yt").Press(KeyKind.Space).Press(KeyKind.Space).Hotkey();

      Assert.Equal([new SwitchLayout(_russian)], typist.Actions);
    }

    [Fact]
    public void LetterAfterTheSpaceStartsTheNextWord() {
      Typist typist = Manual().Type("yt").Press(KeyKind.Space).Type("ghbdtn").Hotkey();

      Assert.Equal([new ReplaceText("ghbdtn", "привет")], typist.Actions);
    }

    [Fact]
    public void PauseBeforeTheHotkeyKeepsTheWord() {
      // The double tap's own Shift presses come after the pause.
      Typist typist = Manual()
          .Type("ghbdtn")
          .Wait(TimeSpan.FromSeconds(5))
          .Press(KeyKind.Modifier, KeyModifiers.Shift)
          .Press(KeyKind.Modifier, KeyModifiers.Shift)
          .Hotkey();

      Assert.Equal([new ReplaceText("ghbdtn", "привет")], typist.Actions);
    }

    [Theory]
    [InlineData("click")]
    [InlineData("focus")]
    [InlineData("input lost")]
    [InlineData("arrow")]
    [InlineData("shortcut")]
    [InlineData("backspace")]
    [InlineData("layout switch")]
    public void BoundaryLeavesNothingToCorrect(string boundary) {
      Typist typist = Cross(boundary, Manual().Type("yt").Press(KeyKind.Space)).Hotkey();

      Assert.Equal([new SwitchLayout(_russian)], typist.Actions);
    }

    [Fact]
    public void LayoutSwitchByTheUserInsideTheWordLeavesItAlone() {
      Typist typist = Manual()
          .Type("gh")
          .SwitchLayout(KeyMapFixtures.Russian, byUser: true)
          .SwitchLayout(KeyMapFixtures.EnglishUs, byUser: true)
          .Type("bdtn")
          .Hotkey();

      Assert.Equal([new SwitchLayout(_russian)], typist.Actions);
    }

    [Fact]
    public void LayoutSwitchByTheUserBeforeTheWordKeepsItCorrectable() {
      Typist typist = Manual()
          .SwitchLayout(KeyMapFixtures.Russian, byUser: true)
          .SwitchLayout(KeyMapFixtures.EnglishUs, byUser: true)
          .Type("ghbdtn")
          .Hotkey();

      Assert.Equal([new ReplaceText("ghbdtn", "привет")], typist.Actions);
    }

    [Fact]
    public void NothingToCorrectSwitchesToTheNextLayout() {
      Typist typist = Manual().Hotkey();

      Assert.Equal([new SwitchLayout(_russian)], typist.Actions);
    }

    [Fact]
    public void NextLayoutWrapsAround() {
      Typist typist = Manual().SwitchLayout(KeyMapFixtures.Russian, byUser: false).Hotkey();

      Assert.Equal([new SwitchLayout(_english)], typist.Actions);
    }

    [Fact]
    public void HotkeyRevertsTheLastCorrection() {
      Typist typist = new Typist(new Settings())
          .Type("ghbdtn").Hold(KeyKind.Space).Finish()
          .Hotkey().Finish();

      Assert.Equal(
          [
            new ReplaceText("привет ", "ghbdtn "),
            new SwitchLayout(_english),
            new UndoPerformed("ghbdtn", "привет"),
          ],
          typist.Actions.Skip(2));
    }

    [Fact]
    public void HotkeyRevertsAManualCorrection() {
      Typist typist = Manual().Type("ghbdtn").Hotkey().Finish().Hotkey();

      Assert.Equal(new ReplaceText("привет", "ghbdtn"), typist.Actions[^1]);
    }

    [Fact]
    public void HotkeyAfterTheUndoWindowDoesNotRevert() {
      Typist typist = new Typist(new Settings())
          .Type("ghbdtn").Hold(KeyKind.Space).Finish()
          .Wait(TimeSpan.FromSeconds(6))
          .Hotkey();

      Assert.Equal(new SwitchLayout(_russian), typist.Actions[^1]);
    }

    [Fact]
    public void HotkeyAfterAnEnterTriggeredCorrectionDoesNotRevert() {
      Typist typist = new Typist(new Settings())
          .Type("ghbdtn").Hold(KeyKind.Enter).Finish()
          .Hotkey();

      Assert.Equal(new SwitchLayout(_russian), typist.Actions[^1]);
    }

    [Fact]
    public void HotkeyAfterAnotherKeyDoesNotRevert() {
      Typist typist = new Typist(new Settings())
          .Type("ghbdtn").Hold(KeyKind.Space).Finish()
          .Press(KeyKind.Navigation)
          .Hotkey();

      Assert.Equal(new SwitchLayout(_russian), typist.Actions[^1]);
    }

    [Fact]
    public void BestRenderingAmongThreeLayoutsWins() {
      Typist typist = Manual()
          .Install(KeyMapFixtures.EnglishUs, KeyMapFixtures.Russian, KeyMapFixtures.Ukrainian)
          .Type("ghbdsn").Hotkey().Finish();

      Assert.Equal(
          [new ReplaceText("ghbdsn", "привіт"), new SwitchLayout(_ukrainian)],
          typist.Actions[^2..]);
    }

    [Fact]
    public void RussianWordAmongThreeLayoutsGoesToRussian() {
      Typist typist = Manual()
          .Install(KeyMapFixtures.EnglishUs, KeyMapFixtures.Ukrainian, KeyMapFixtures.Russian)
          .Type("ghbdtn").Hotkey();

      Assert.Equal(new ReplaceText("ghbdtn", "привет"), typist.Actions[^1]);
    }

    [Fact]
    public void LayoutsWithoutProfilesTieAndTheNextOneWins() {
      // No Language Profiles: every Rendering scores the same.
      Typist typist = new Typist((_, _) => Decision.Keep, _off)
          .Install(KeyMapFixtures.EnglishUs, KeyMapFixtures.Ukrainian, KeyMapFixtures.Russian)
          .Type("ghbdsn").Hotkey();

      Assert.Equal(new ReplaceText("ghbdsn", "привіт"), typist.Actions[^1]);
    }

    [Fact]
    public void HotkeyWorksWhileAutoCorrectionIsPaused() {
      var clock = new ManualClock();
      Typist typist = new Typist(new Settings(PausedUntil: clock.Now.AddHours(1)), clock)
          .Type("ghbdtn").Hotkey();

      Assert.Equal([new ReplaceText("ghbdtn", "привет")], typist.Actions);
    }

    [Fact]
    public void HotkeyWorksInAnExcludedApp() {
      var settings = new Settings(ExcludedApps: new ExcludedAppList([Typist.App.ExePath]));
      Typist typist = new Typist(settings).Type("ghbdtn").Press(KeyKind.Space).Hotkey();

      Assert.Equal([new ReplaceText("ghbdtn ", "привет ")], typist.Actions);
    }

    [Fact]
    public void HotkeyWorksWhileRekeyPausedItself() {
      Typist typist = new Typist(new Settings()).SelfPause(true).Type("ghbdtn").Hotkey();

      Assert.Equal([new ReplaceText("ghbdtn", "привет")], typist.Actions);
    }

    [Fact]
    public void HotkeyDoesNothingInAPasswordField() {
      Typist typist = Manual().Focus(isPassword: true).Type("ghbdtn").Hotkey();

      Assert.Empty(typist.Actions);
    }

    [Fact]
    public void HotkeyDoesNothingWhileFocusIsUnknown() {
      Typist typist = Manual().Type("ghbdtn").Send(new FocusLost(TimeSpan.FromSeconds(100)))
          .Hotkey();

      Assert.Empty(typist.Actions);
    }

    [Fact]
    public void HotkeyDoesNothingInAnElevatedWindow() {
      Typist typist = Manual().Focus(isElevated: true).Hotkey();

      Assert.Empty(typist.Actions);
    }

    private static Typist Cross(string boundary, Typist typist) {
      return boundary switch {
        "click" => typist.Click(),
        "focus" => typist.Focus(),
        "input lost" => typist.Send(new InputLost(TimeSpan.FromSeconds(100))),
        "arrow" => typist.Press(KeyKind.Navigation),
        "shortcut" => typist.Chord(KeyModifiers.Control, 'c'),
        "backspace" => typist.Press(KeyKind.Backspace),
        _ => typist.SwitchLayout(KeyMapFixtures.EnglishUs, byUser: true),
      };
    }

    private static Typist Manual() {
      return new Typist(_off);
    }
  }
}
