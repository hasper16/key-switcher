using Rekey.Win32.Keyboard;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Xunit;

namespace Rekey.Win32.Tests.Keyboard {
  // Design D1 and D4: what the hook holds back while a Correction may be applied.
  public sealed class HoldGateTests {
    private const ushort _letterScan = 0x22;
    private const ushort _spaceScan = 0x39;
    private const ushort _zScan = 0x2C;
    private const ushort _ctrlScan = 0x1D;

    private readonly HoldGate _gate = new();
    private long _sequence;
    private long _tick = 1000;

    public HoldGateTests() {
      _gate.UsePolicy(new HookPolicy(
          [(ushort)VIRTUAL_KEY.VK_SPACE, (ushort)VIRTUAL_KEY.VK_RETURN],
          [_letterScan, _zScan]));
    }

    [Fact]
    public void TriggerKeyAfterACharacterKeyIsHeld() {
      Letter();

      Assert.Equal(Holding.Held, Space());
    }

    [Fact]
    public void TriggerKeyAfterAnotherTriggerKeyPasses() {
      Letter();
      Space();
      _gate.Release(_sequence - 1);

      Assert.Equal(Holding.None, Space());
    }

    [Fact]
    public void KeyThatIsNoTriggerPassesAfterACharacterKey() {
      Letter();

      Assert.Equal(Holding.None, Down(VIRTUAL_KEY.VK_TAB, 0x0F));
    }

    [Fact]
    public void TriggerKeyWithCtrlPasses() {
      Letter();
      Down(VIRTUAL_KEY.VK_LCONTROL, _ctrlScan);

      Assert.Equal(Holding.None, Space());
    }

    [Fact]
    public void ShiftBetweenTheCharacterAndTheTriggerKeepsTheHold() {
      Letter();
      Down(VIRTUAL_KEY.VK_LSHIFT, 0x2A);

      Assert.Equal(Holding.Held, Space());
    }

    [Fact]
    public void LetterTypedWithCtrlIsNoCharacter() {
      Down(VIRTUAL_KEY.VK_LCONTROL, _ctrlScan);
      Letter();
      Up(VIRTUAL_KEY.VK_LCONTROL, _ctrlScan);

      Assert.Equal(Holding.None, Space());
    }

    [Fact]
    public void EveryKeyEventIsHeldUntilTheWorkerReleasesTheLastHeldOne() {
      Letter();
      Space();
      Holding up = Up(VIRTUAL_KEY.VK_SPACE, _spaceScan);
      Holding next = Letter();

      Assert.Equal((Holding.Held, Holding.Held), (up, next));
    }

    [Fact]
    public void ReleasingAnEarlierHeldKeyKeepsHolding() {
      Letter();
      Space();
      long space = _sequence - 1;
      Letter();

      _gate.Release(space);

      Assert.Equal(Holding.Held, Letter());
    }

    [Fact]
    public void ReleasingTheLastHeldKeyEndsTheHold() {
      Letter();
      Space();
      Letter();
      _gate.Release(_sequence - 1);

      Assert.Equal(Holding.None, Down(VIRTUAL_KEY.VK_TAB, 0x0F));
    }

    [Fact]
    public void KeyLaterThan150MillisecondsIntoTheHoldOpensTheValve() {
      Letter();
      Space();
      _tick += 151;

      Holding late = Letter();

      Assert.Equal(Holding.None, late);
      Assert.Equal(1, _gate.Timeouts);
    }

    [Fact]
    public void OpenValveLetsKeysPassUntilTheWorkerCatchesUp() {
      Letter();
      Space();
      _tick += 151;
      Letter();

      Holding trigger = Space();

      Assert.Equal(Holding.None, trigger);
      Assert.Equal(1, _gate.Timeouts);
    }

    [Fact]
    public void KeyWithin150MillisecondsStaysHeld() {
      Letter();
      Space();
      _tick += 150;

      Assert.Equal(Holding.Held, Letter());
      Assert.Equal(0, _gate.Timeouts);
    }

    [Fact]
    public void FullRingNeverHolds() {
      Letter();

      Assert.Equal(Holding.None, Space(hasRoom: false));
    }

    [Fact]
    public void WithoutAPolicyNothingIsHeld() {
      var gate = new HoldGate();
      gate.Judge(RawInputKind.KeyDown, 0x47, _letterScan, false, 0, _tick, true);

      Holding space = gate.Judge(
          RawInputKind.KeyDown, (ushort)VIRTUAL_KEY.VK_SPACE, _spaceScan, false, 1, _tick, true);

      Assert.Equal(Holding.None, space);
    }

    [Fact]
    public void CtrlZRightAfterTheArmedTriggerIsAnUndoRequest() {
      ArmedCorrection();
      Down(VIRTUAL_KEY.VK_LCONTROL, _ctrlScan);

      Assert.Equal(Holding.UndoRequest, Down(VIRTUAL_KEY.VK_Z, _zScan));
    }

    [Fact]
    public void CtrlZAfterAnotherKeyPasses() {
      ArmedCorrection();
      Letter();
      Down(VIRTUAL_KEY.VK_LCONTROL, _ctrlScan);

      Assert.Equal(Holding.None, Down(VIRTUAL_KEY.VK_Z, _zScan));
    }

    [Fact]
    public void CtrlAltZPasses() {
      ArmedCorrection();
      Down(VIRTUAL_KEY.VK_LCONTROL, _ctrlScan);
      Down(VIRTUAL_KEY.VK_LMENU, 0x38);

      Assert.Equal(Holding.None, Down(VIRTUAL_KEY.VK_Z, _zScan));
    }

    [Fact]
    public void ZWithoutCtrlPasses() {
      ArmedCorrection();

      Assert.Equal(Holding.None, Down(VIRTUAL_KEY.VK_Z, _zScan));
    }

    [Fact]
    public void CtrlZWithNothingArmedPasses() {
      Down(VIRTUAL_KEY.VK_LCONTROL, _ctrlScan);

      Assert.Equal(Holding.None, Down(VIRTUAL_KEY.VK_Z, _zScan));
    }

    // A Word, a held Space the worker corrected and released, and the Undo armed for it.
    private void ArmedCorrection() {
      Letter();
      Space();
      long trigger = _sequence - 1;
      _gate.Release(trigger);
      _gate.ArmUndo(trigger);
      Up(VIRTUAL_KEY.VK_SPACE, _spaceScan);
    }

    private Holding Letter() {
      Holding holding = Down((VIRTUAL_KEY)0x47, _letterScan);
      Up((VIRTUAL_KEY)0x47, _letterScan);
      return holding;
    }

    private Holding Space(bool hasRoom = true) {
      return Judge(RawInputKind.KeyDown, VIRTUAL_KEY.VK_SPACE, _spaceScan, hasRoom);
    }

    private Holding Down(VIRTUAL_KEY key, ushort scanCode) {
      return Judge(RawInputKind.KeyDown, key, scanCode, hasRoom: true);
    }

    private Holding Up(VIRTUAL_KEY key, ushort scanCode) {
      return Judge(RawInputKind.KeyUp, key, scanCode, hasRoom: true);
    }

    private Holding Judge(RawInputKind kind, VIRTUAL_KEY key, ushort scanCode, bool hasRoom) {
      return _gate.Judge(kind, (ushort)key, scanCode, false, _sequence++, _tick, hasRoom);
    }
  }
}
