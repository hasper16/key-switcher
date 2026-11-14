using Xunit;

namespace Rekey.Core.Tests {
  // Spec manual-correction: "Double tap recognition". Ticks are milliseconds.
  public sealed class DoubleTapTests {
    private readonly DoubleTap _shift = new(KeyModifiers.Shift);
    private long _now = 1_000;

    [Fact]
    public void TwoQuickTapsFire() {
      Assert.False(Tap(KeyModifiers.Shift, 50));
      Wait(100);

      Assert.True(Tap(KeyModifiers.Shift, 50));
    }

    [Fact]
    public void SecondReleaseAtTheEndOfTheWindowStillFires() {
      Tap(KeyModifiers.Shift, 50);
      Wait(290);

      Assert.True(Tap(KeyModifiers.Shift, 100));
    }

    [Fact]
    public void SlowTapsDoNotFire() {
      Tap(KeyModifiers.Shift, 50);
      Wait(350);

      Assert.False(Tap(KeyModifiers.Shift, 51));
    }

    [Fact]
    public void ThirdTapStartsANewPair() {
      Tap(KeyModifiers.Shift, 50);
      Tap(KeyModifiers.Shift, 50);

      Assert.False(Tap(KeyModifiers.Shift, 50));
      Assert.True(Tap(KeyModifiers.Shift, 50));
    }

    [Fact]
    public void SlowTapStillStartsAPair() {
      Tap(KeyModifiers.Shift, 50);
      Wait(1_000);
      Tap(KeyModifiers.Shift, 50);

      Assert.True(Tap(KeyModifiers.Shift, 50));
    }

    [Fact]
    public void CapitalLetterInBetweenResets() {
      // Shift held for "G", then one tap.
      Press(KeyModifiers.Shift, down: true);
      Press(KeyModifiers.None, down: true);
      Press(KeyModifiers.None, down: false);
      Press(KeyModifiers.Shift, down: false);

      Assert.False(Tap(KeyModifiers.Shift, 50));
    }

    [Fact]
    public void KeyBetweenTapsResets() {
      Tap(KeyModifiers.Shift, 50);
      Press(KeyModifiers.None, down: true);

      Assert.False(Tap(KeyModifiers.Shift, 50));
    }

    [Fact]
    public void OtherModifierBetweenTapsResets() {
      Tap(KeyModifiers.Shift, 50);
      Tap(KeyModifiers.Control, 50);

      Assert.False(Tap(KeyModifiers.Shift, 50));
    }

    [Fact]
    public void LongPressIsNotATap() {
      Tap(KeyModifiers.Shift, 600);

      Assert.False(Tap(KeyModifiers.Shift, 50));
    }

    [Fact]
    public void AutoRepeatWhileHeldIsOnePress() {
      Press(KeyModifiers.Shift, down: true);
      Press(KeyModifiers.Shift, down: true);
      Press(KeyModifiers.Shift, down: false);

      Assert.True(Tap(KeyModifiers.Shift, 50));
    }

    [Fact]
    public void ReleaseWithoutAPressIsNotATap() {
      Press(KeyModifiers.Shift, down: false);

      Assert.False(Tap(KeyModifiers.Shift, 50));
    }

    [Fact]
    public void KeyUpOfAnotherKeyKeepsThePair() {
      Tap(KeyModifiers.Shift, 50);
      Press(KeyModifiers.None, down: false);

      Assert.True(Tap(KeyModifiers.Shift, 50));
    }

    [Fact]
    public void DetectorForCtrlIgnoresShift() {
      var control = new DoubleTap(KeyModifiers.Control);

      Assert.False(control.Observe(KeyModifiers.Shift, down: true, _now));
      Assert.False(control.Observe(KeyModifiers.Shift, down: false, _now + 10));
      Assert.False(control.Observe(KeyModifiers.Shift, down: true, _now + 20));
      Assert.False(control.Observe(KeyModifiers.Shift, down: false, _now + 30));
    }

    // A press and release of one key, held for the given milliseconds; returns whether the
    // release fired.
    private bool Tap(KeyModifiers key, long held) {
      Press(key, down: true);
      Wait(held);
      bool fired = Press(key, down: false);
      Wait(10);
      return fired;
    }

    private bool Press(KeyModifiers key, bool down) {
      return _shift.Observe(key, down, _now);
    }

    private void Wait(long milliseconds) {
      _now += milliseconds;
    }
  }
}
