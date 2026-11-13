using Rekey.Core.Detection;
using Rekey.Core.Tests.Detection;
using Xunit;

namespace Rekey.Core.Tests {
  // Spec auto-correction, engine side, with the bundled Language Profiles; the Words are the
  // spec's own examples.
  public sealed class AutoCorrectionTests : IDisposable {
    private static readonly ReplaceText _correction = new("ghbdtn", "привет");
    private static readonly Settings _withTab =
        new(TriggerKeys: TriggerKeys.Space | TriggerKeys.Enter | TriggerKeys.Tab);

    private readonly DirectoryInfo _folder = Directory.CreateTempSubdirectory("rekey-pause-");

    public void Dispose() {
      _folder.Delete(recursive: true);
    }

    [Fact]
    public void SpaceEndsAWrongLayoutWordAndCorrectsIt() {
      Typist typist = Default().Type("ghbdtn").Hold(KeyKind.Space);

      Assert.Equal([_correction], typist.Actions);
    }

    [Fact]
    public void AppliedCorrectionSwitchesToTheLayoutOfTheRendering() {
      Typist typist = Default().Type("ghbdtn").Hold(KeyKind.Space).Finish();

      Assert.Equal([_correction, new SwitchLayout(KeyMapFixtures.Russian.Layout)], typist.Actions);
    }

    [Fact]
    public void WordInTheRightLayoutIsKept() {
      Typist typist = Default().Type("hello").Hold(KeyKind.Space);

      Assert.Empty(typist.Actions);
    }

    [Fact]
    public void EnterIsATriggerKeyByDefault() {
      Typist typist = Default().Type("ghbdtn").Hold(KeyKind.Enter);

      Assert.Equal([_correction], typist.Actions);
    }

    [Fact]
    public void TabIsNotATriggerKeyByDefault() {
      Typist typist = Default().Type("ghbdtn").Hold(KeyKind.Tab);

      Assert.Empty(typist.Actions);
      Assert.Empty(typist.Engine.Word);
    }

    [Fact]
    public void EnabledTabCorrectsTheWord() {
      Typist typist = new Typist(_withTab).Type("ghbdtn").Hold(KeyKind.Tab);

      Assert.Equal([_correction], typist.Actions);
    }

    [Fact]
    public void DisabledEnterCorrectsNothing() {
      Typist typist = new Typist(new Settings(TriggerKeys: TriggerKeys.Space))
          .Type("ghbdtn").Hold(KeyKind.Enter);

      Assert.Empty(typist.Actions);
    }

    [Fact]
    public void SpaceThatIsNotATriggerKeyEndsTheWordWithoutEvaluation() {
      // Spec keyboard-capture: "ghb", Space, "dtn" leaves "dtn".
      Typist typist = new Typist(new Settings(TriggerKeys: TriggerKeys.Enter))
          .Type("ghb").Hold(KeyKind.Space).Type("dtn");

      Assert.Empty(typist.Actions);
      Assert.Equal("dtn", typist.Word);
    }

    [Fact]
    public void TurnedOffCorrectsNothing() {
      Typist typist = new Typist(new Settings(AutoCorrectionEnabled: false))
          .Type("ghbdtn").Hold(KeyKind.Space);

      Assert.Empty(typist.Actions);
      Assert.Equal(GateResult.Off, typist.Engine.Gate);
    }

    [Fact]
    public void TurningItOnAgainCorrects() {
      Typist typist = new Typist(new Settings(AutoCorrectionEnabled: false))
          .Type("ghbdtn").Hold(KeyKind.Space)
          .Use(new Settings())
          .Type("ghbdtn").Hold(KeyKind.Space);

      Assert.Equal([_correction], typist.Actions);
    }

    [Fact]
    public void ExcludedAppCorrectsNothingButKeepsTheWord() {
      // The Word stays known, so Manual Correction can still correct it there.
      var settings = new Settings(ExcludedApps: new ExcludedAppList([Typist.App.ExePath]));
      Typist typist = new Typist(settings).Type("ghbdtn");

      Assert.Equal("ghbdtn", typist.Word);
      Assert.Equal(GateResult.ExcludedApp, typist.Engine.Gate);
      Assert.Empty(typist.Hold(KeyKind.Space).Actions);
    }

    [Fact]
    public void OtherAppThanTheExcludedOneIsCorrected() {
      var settings = new Settings(ExcludedApps: new ExcludedAppList([Typist.App.ExePath]));
      Typist typist = new Typist(settings)
          .FocusOn(new AppId(@"C:\Apps\Zorbit.exe")).Type("ghbdtn").Hold(KeyKind.Space);

      Assert.Equal([_correction], typist.Actions);
    }

    [Fact]
    public void SelfPauseCorrectsNothingUntilItEnds() {
      Typist typist = Default().SelfPause(true).Type("ghbdtn").Hold(KeyKind.Space);

      Assert.Empty(typist.Actions);
      Assert.Equal(GateResult.SelfPaused, typist.Engine.Gate);

      typist.SelfPause(false).Type("ghbdtn").Hold(KeyKind.Space);

      Assert.Equal([_correction], typist.Actions);
    }

    [Fact]
    public void ElevatedWindowReportsItsReason() {
      Assert.Equal(GateResult.Elevated, Default().Focus(isElevated: true).Engine.Gate);
    }

    [Fact]
    public void PasswordFieldReportsItsReason() {
      Assert.Equal(GateResult.PasswordField, Default().Focus(isPassword: true).Engine.Gate);
    }

    [Fact]
    public void SensitivityComesFromTheSettings() {
      // "yt" is a short Word: deferred under Balanced, corrected under Aggressive.
      Typist balanced = Default().Type("yt").Hold(KeyKind.Space);
      Typist aggressive = new Typist(new Settings(Sensitivity: Sensitivity.Aggressive))
          .Type("yt").Hold(KeyKind.Space);

      Assert.Empty(balanced.Actions);
      Assert.Equal([new ReplaceText("yt", "не")], aggressive.Actions);
    }

    [Fact]
    public void HeldTriggerKeysFollowTheGate() {
      Typist typist = new Typist(_withTab);

      Assert.Equal(_withTab.TriggerKeys, typist.Engine.HeldTriggerKeys);
      Assert.Equal(TriggerKeys.None, typist.SelfPause(true).Engine.HeldTriggerKeys);
      Assert.Equal(_withTab.TriggerKeys, typist.SelfPause(false).Engine.HeldTriggerKeys);
      Assert.Equal(TriggerKeys.None, typist.Focus(isPassword: true).Engine.HeldTriggerKeys);
    }

    [Fact]
    public void WordWithAUserLayoutSwitchIsNotCorrected() {
      Typist typist = Default().SwitchLayout(KeyMapFixtures.Russian, byUser: false)
          .Type("ghb").SwitchLayout(KeyMapFixtures.EnglishUs, byUser: true).Type("dtn");

      Assert.Equal(GateResult.LayoutSwitchedByUser, typist.Engine.Gate);
      Assert.Empty(typist.Hold(KeyKind.Space).Actions);
    }

    [Fact]
    public void WordWithAUserLayoutSwitchIsEvaluatedWhenTheSettingIsOff() {
      Typist typist = new Typist(new Settings(SkipAfterManualLayoutSwitch: false))
          .SwitchLayout(KeyMapFixtures.Russian, byUser: false)
          .Type("ghb").SwitchLayout(KeyMapFixtures.EnglishUs, byUser: true).Type("dtn")
          .Hold(KeyKind.Space);

      Assert.Equal([_correction], typist.Actions);
    }

    [Fact]
    public void RekeysOwnLayoutSwitchDoesNotMarkTheWord() {
      Typist typist = Default().SwitchLayout(KeyMapFixtures.Russian, byUser: false)
          .Type("ghb").SwitchLayout(KeyMapFixtures.EnglishUs, byUser: false).Type("dtn")
          .Hold(KeyKind.Space);

      Assert.Equal([_correction], typist.Actions);
    }

    [Fact]
    public void WordTypedAfterRekeysOwnSwitchIsCorrectedBackWhenItIsEnglish() {
      // After the Correction the Layout is Russian; an English Word typed there reads "руддщ".
      Typist typist = Default().Type("ghbdtn").Hold(KeyKind.Space).Finish()
          .SwitchLayout(KeyMapFixtures.Russian, byUser: false);
      typist.Actions.Clear();

      typist.Type("hello").Hold(KeyKind.Space);

      Assert.Equal([new ReplaceText("руддщ", "hello")], typist.Actions);
    }

    [Fact]
    public void UserLayoutSwitchBeforeTheFirstKeyCoversTheWordTypedNext() {
      Typist typist = Default().SwitchLayout(KeyMapFixtures.Russian, byUser: false)
          .SwitchLayout(KeyMapFixtures.EnglishUs, byUser: true)
          .Type("ghbdtn").Hold(KeyKind.Space);

      Assert.Empty(typist.Actions);
    }

    [Fact]
    public void WordAfterTheSkippedOneIsCorrectedAgain() {
      Typist typist = Default().SwitchLayout(KeyMapFixtures.Russian, byUser: false)
          .SwitchLayout(KeyMapFixtures.EnglishUs, byUser: true)
          .Type("ghbdtn").Hold(KeyKind.Space)
          .Type("ghbdtn").Hold(KeyKind.Space);

      Assert.Equal([_correction], typist.Actions);
    }

    [Fact]
    public void UserLayoutSwitchLongAfterTheLastKeyStillCoversTheWordTypedAtOnce() {
      // The pause that matters is the one after the switch, not the one before it.
      Typist typist = Default().SwitchLayout(KeyMapFixtures.Russian, byUser: false)
          .Type("hello").Press(KeyKind.Space).Wait(TimeSpan.FromSeconds(10))
          .SwitchLayout(KeyMapFixtures.EnglishUs, byUser: true)
          .Type("ghbdtn").Hold(KeyKind.Space);

      Assert.Empty(typist.Actions);
    }

    [Fact]
    public void PauseAfterAUserLayoutSwitchBeforeAnyKeyStartsAnUnmarkedWord() {
      Typist typist = Default().SwitchLayout(KeyMapFixtures.Russian, byUser: false)
          .SwitchLayout(KeyMapFixtures.EnglishUs, byUser: true)
          .Wait(TimeSpan.FromSeconds(3))
          .Type("ghbdtn").Hold(KeyKind.Space);

      Assert.Equal([_correction], typist.Actions);
    }

    [Fact]
    public void UserLayoutSwitchAfterAPauseMidWordEndsThatWord() {
      // The pause already ended "ghb"; the keys after the switch are a new, marked Word.
      Typist typist = new Typist(new Settings(SkipAfterManualLayoutSwitch: false))
          .Type("ghb").Wait(TimeSpan.FromSeconds(3))
          .SwitchLayout(KeyMapFixtures.EnglishUs, byUser: true)
          .Type("dtn");

      Assert.Equal("dtn", typist.Word);
    }

    [Fact]
    public void UserLayoutSwitchMidWordDoesNotExtendTheTypingGap() {
      Typist typist = Default().Type("ghb")
          .Wait(TimeSpan.FromSeconds(1.5))
          .SwitchLayout(KeyMapFixtures.EnglishUs, byUser: true)
          .Wait(TimeSpan.FromSeconds(1.5))
          .Type("dtn");

      Assert.Equal("dtn", typist.Word);
    }

    [Fact]
    public void UserLayoutSwitchInADroppedWordDoesNotExtendTheTypingGap() {
      // The dropped Word keeps no Keystrokes but is still underway until the gap ends it.
      Typist typist = Default().TypeUnconfirmed('g')
          .Wait(TimeSpan.FromSeconds(1.5))
          .SwitchLayout(KeyMapFixtures.EnglishUs, byUser: true)
          .Wait(TimeSpan.FromSeconds(1.5))
          .Type("dtn");

      Assert.Equal("dtn", typist.Word);
    }

    [Fact]
    public void TypingGapAfterAUserLayoutSwitchStartsAnUnmarkedWord() {
      Typist typist = Default().SwitchLayout(KeyMapFixtures.Russian, byUser: false)
          .SwitchLayout(KeyMapFixtures.EnglishUs, byUser: true)
          .Press(KeyKind.Modifier).Wait(TimeSpan.FromSeconds(3))
          .Type("ghbdtn").Hold(KeyKind.Space);

      Assert.Equal([_correction], typist.Actions);
    }

    [Fact]
    public void UserLayoutSwitchDropsDeferredWords() {
      // "yt" waits for the next Word; the user's switch makes it stay as typed.
      Typist typist = Corrector(new Settings(SkipAfterManualLayoutSwitch: false))
          .Type("yt").Hold(KeyKind.Space)
          .SwitchLayout(KeyMapFixtures.Russian, byUser: true)
          .SwitchLayout(KeyMapFixtures.EnglishUs, byUser: true)
          .Type("pyf.").Hold(KeyKind.Space);

      Assert.Equal([new ReplaceText("pyf.", "знаю")], typist.Actions);
    }

    [Fact]
    public void RekeysOwnLayoutSwitchKeepsDeferredWords() {
      Typist typist = Corrector(new Settings())
          .Type("yt").Hold(KeyKind.Space)
          .SwitchLayout(KeyMapFixtures.Russian, byUser: false)
          .SwitchLayout(KeyMapFixtures.EnglishUs, byUser: false)
          .Type("pyf.").Hold(KeyKind.Space);

      Assert.Equal([new ReplaceText("yt pyf.", "не знаю")], typist.Actions);
    }

    [Fact]
    public void PauseEndsOnItsOwn() {
      var clock = new ManualClock();
      var typist = new Typist(new Settings(PausedUntil: clock.Now.AddMinutes(30)), clock);

      typist.Type("ghbdtn").Hold(KeyKind.Space);
      clock.Advance(TimeSpan.FromMinutes(29));
      typist.Type("ghbdtn").Hold(KeyKind.Space);

      Assert.Empty(typist.Actions);
      Assert.Equal(GateResult.Paused, typist.Engine.Gate);
      Assert.Equal(TriggerKeys.None, typist.Engine.HeldTriggerKeys);

      clock.Advance(TimeSpan.FromMinutes(1));

      Assert.Equal(GateResult.Applies, typist.Engine.Gate);
      Assert.Equal([_correction], typist.Type("ghbdtn").Hold(KeyKind.Space).Actions);
    }

    [Fact]
    public void PauseSurvivesASettingsReload() {
      // Spec: Rekey is restarted 10 minutes into a 30-minute pause.
      var clock = new ManualClock();
      new SettingsStore(_folder.FullName).Save(
          new Settings(PausedUntil: clock.Now.AddMinutes(30)));
      clock.Advance(TimeSpan.FromMinutes(10));

      var typist = new Typist(new SettingsStore(_folder.FullName).Load(), clock);

      Assert.Empty(typist.Type("ghbdtn").Hold(KeyKind.Space).Actions);
      clock.Advance(TimeSpan.FromMinutes(20));
      Assert.Equal([_correction], typist.Type("ghbdtn").Hold(KeyKind.Space).Actions);
    }

    [Fact]
    public void CountsWordsEndedAndEvaluated() {
      Typist typist = Default().Type("ghbdtn").Hold(KeyKind.Space).Finish()
          .Type("hello").Hold(KeyKind.Space)
          .Type("hello").Press(KeyKind.Space);

      Assert.Equal(new WordCounts(3, 2, 0), typist.Engine.WordCounts);
    }

    [Fact]
    public void CountsAWordDroppedForAnUnconfirmedKey() {
      Typist typist = Default().Type("ghb").TypeUnconfirmed('d').Type("tn").Hold(KeyKind.Space);

      Assert.Empty(typist.Actions);
      Assert.Equal(new WordCounts(1, 0, 1), typist.Engine.WordCounts);
    }

    [Fact]
    public void WordAfterTheDroppedOneIsNotCountedAsDropped() {
      Typist typist = Default().Type("gh").TypeUnconfirmed('b').Hold(KeyKind.Space)
          .Type("ghbdtn").Hold(KeyKind.Space);

      Assert.Equal(new WordCounts(2, 1, 1), typist.Engine.WordCounts);
    }

    [Fact]
    public void CountsNoWordsWhileAutoCorrectionDoesNotApply() {
      Typist typist = new Typist(new Settings(AutoCorrectionEnabled: false))
          .Type("hello").Hold(KeyKind.Space);

      Assert.Equal(default, typist.Engine.WordCounts);
    }

    [Fact]
    public void CountsNoWordForATriggerKeyAfterNothing() {
      Typist typist = Default().Hold(KeyKind.Space).Press(KeyKind.Enter);

      Assert.Equal(default, typist.Engine.WordCounts);
    }

    private static Typist Default() {
      return new Typist(new Settings());
    }

    // Corrects every Word to Russian, except that "yt" waits for the next Word.
    private static Typist Corrector(Settings settings) {
      return new Typist(
          (word, _) => {
            string text = KeyMapFixtures.Russian.Render(word)!;
            return KeyMapFixtures.EnglishUs.Render(word) == "yt"
                ? Decision.Defer(KeyMapFixtures.Russian.Layout, text)
                : Decision.Correct(KeyMapFixtures.Russian.Layout, text);
          },
          settings);
    }
  }
}
