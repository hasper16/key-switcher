using Xunit;

namespace Rekey.Core.Tests {
  // Spec auto-correction: every reason Auto Correction stays off, and their order (design D1).
  public sealed class AutoCorrectionGateTests {
    private static readonly DateTimeOffset _now = new(2026, 11, 12, 12, 0, 0, TimeSpan.Zero);
    private static readonly AppId _app = new(@"C:\Apps\Zorbit.exe");
    private static readonly Context _editor = new(_app, IsPasswordField: false, IsElevated: false);

    [Fact]
    public void AppliesInAnOrdinaryControl() {
      Assert.Equal(GateResult.Applies, Decide(new Settings()));
    }

    [Fact]
    public void OffWhenTurnedOff() {
      Assert.Equal(GateResult.Off, Decide(new Settings(AutoCorrectionEnabled: false)));
    }

    [Fact]
    public void PausedUntilThePauseEnds() {
      var settings = new Settings(PausedUntil: _now.AddMinutes(30));

      Assert.Equal(GateResult.Paused, Decide(settings));
      Assert.Equal(GateResult.Paused, Decide(settings, now: _now.AddMinutes(30).AddTicks(-1)));
      Assert.Equal(GateResult.Applies, Decide(settings, now: _now.AddMinutes(30)));
    }

    [Fact]
    public void SelfPausedAfterKeyboardFailures() {
      Assert.Equal(GateResult.SelfPaused, Decide(new Settings(), selfPaused: true));
    }

    [Fact]
    public void ExcludedAppIsOff() {
      var settings = new Settings(ExcludedApps: new ExcludedAppList([_app.ExePath]));

      Assert.Equal(GateResult.ExcludedApp, Decide(settings));
    }

    [Fact]
    public void PasswordFieldIsOff() {
      Context secret = _editor with { IsPasswordField = true };

      Assert.Equal(GateResult.PasswordField, Decide(new Settings(), secret));
    }

    [Fact]
    public void UnknownFocusCountsAsAPasswordField() {
      GateResult result = AutoCorrectionGate.Decide(
          new Settings(), null, selfPaused: false, layoutSwitchedByUser: false, _now);

      Assert.Equal(GateResult.PasswordField, result);
    }

    [Fact]
    public void ElevatedWindowIsOff() {
      Assert.Equal(GateResult.Elevated, Decide(new Settings(), _editor with { IsElevated = true }));
    }

    [Fact]
    public void WordWithAUserLayoutSwitchIsSkipped() {
      Assert.Equal(GateResult.LayoutSwitchedByUser, Decide(new Settings(), switched: true));
    }

    [Fact]
    public void WordWithAUserLayoutSwitchIsEvaluatedWhenTheSettingIsOff() {
      var settings = new Settings(SkipAfterManualLayoutSwitch: false);

      Assert.Equal(GateResult.Applies, Decide(settings, switched: true));
    }

    [Fact]
    public void ReasonsAreReportedInAFixedOrder() {
      // Everything at once, then one reason fewer each time.
      var everything = new Settings(
          AutoCorrectionEnabled: false,
          PausedUntil: _now.AddMinutes(1),
          ExcludedApps: new ExcludedAppList([_app.ExePath]));
      Context secret = _editor with { IsPasswordField = true, IsElevated = true };

      Assert.Equal(GateResult.Off, Decide(everything, secret, true, true));
      Settings on = everything with { AutoCorrectionEnabled = true };
      Assert.Equal(GateResult.Paused, Decide(on, secret, true, true));
      Settings resumed = on with { PausedUntil = null };
      Assert.Equal(GateResult.SelfPaused, Decide(resumed, secret, true, true));
      Assert.Equal(GateResult.ExcludedApp, Decide(resumed, secret, false, true));
      var included = new Settings();
      Assert.Equal(GateResult.PasswordField, Decide(included, secret, false, true));
      Context elevated = secret with { IsPasswordField = false };
      Assert.Equal(GateResult.Elevated, Decide(included, elevated, false, true));
      Assert.Equal(GateResult.LayoutSwitchedByUser, Decide(included, _editor, false, true));
    }

    private static GateResult Decide(
        Settings settings,
        Context? focus = null,
        bool selfPaused = false,
        bool switched = false,
        DateTimeOffset? now = null) {
      return AutoCorrectionGate.Decide(
          settings, focus ?? _editor, selfPaused, switched, now ?? _now);
    }
  }
}
