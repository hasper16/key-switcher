using Rekey.Core;
using Rekey.Win32.Keyboard;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Xunit;

namespace Rekey.Win32.Tests.Keyboard {
  // Design D2 of add-auto-correction: the hook holds the Trigger Keys the engine's gate names.
  public sealed class HookPolicyTests {
    private const ushort _letterScan = 0x22;
    private const ushort _space = (ushort)VIRTUAL_KEY.VK_SPACE;
    private const ushort _enter = (ushort)VIRTUAL_KEY.VK_RETURN;
    private const ushort _tab = (ushort)VIRTUAL_KEY.VK_TAB;

    private readonly HoldGate _gate = new();
    private long _sequence;

    [Fact]
    public void HoldsExactlyTheNamedTriggerKeys() {
      HookPolicy policy = HookPolicy.Holding(TriggerKeys.Space | TriggerKeys.Tab, [_letterScan]);

      Assert.True(policy.IsTrigger(_space));
      Assert.True(policy.IsTrigger(_tab));
      Assert.False(policy.IsTrigger(_enter));
    }

    [Fact]
    public void EnterCoversTheNumericKeypadEnter() {
      // Both Enter keys send VK_RETURN; the keypad one only adds the extended flag.
      Assert.True(HookPolicy.Holding(TriggerKeys.Enter, []).IsTrigger(_enter));
    }

    [Fact]
    public void WithNoTriggerKeyNothingIsHeldButCharactersAreStillKnown() {
      HookPolicy policy = HookPolicy.Holding(TriggerKeys.None, [_letterScan]);

      Assert.False(policy.IsTrigger(_space));
      Assert.False(policy.IsTrigger(_enter));
      Assert.False(policy.IsTrigger(_tab));
      Assert.True(policy.IsCharacter(_letterScan, extended: false));
    }

    [Fact]
    public void GateHoldsTabOnlyWhenItIsATriggerKey() {
      _gate.UsePolicy(HookPolicy.Holding(TriggerKeys.Space, [_letterScan]));
      Down(VIRTUAL_KEY.VK_G, _letterScan);

      Assert.Equal(Holding.None, Down(VIRTUAL_KEY.VK_TAB, 0x0F));

      _gate.UsePolicy(HookPolicy.Holding(TriggerKeys.Tab, [_letterScan]));
      Down(VIRTUAL_KEY.VK_G, _letterScan);

      Assert.Equal(Holding.Held, Down(VIRTUAL_KEY.VK_TAB, 0x0F));
    }

    [Fact]
    public void GateHoldsNoSpaceWhileNoTriggerKeyIsNamed() {
      // As in an Excluded App: Space goes straight on.
      _gate.UsePolicy(HookPolicy.Holding(TriggerKeys.None, [_letterScan]));
      Down(VIRTUAL_KEY.VK_G, _letterScan);

      Assert.Equal(Holding.None, Down(VIRTUAL_KEY.VK_SPACE, 0x39));
    }

    [Fact]
    public void FirstTriggerKeyAfterTheGateOpensMidWordIsHeld() {
      // The Word began while nothing was held, as when a Pause ends while the user types.
      _gate.UsePolicy(HookPolicy.Holding(TriggerKeys.None, [_letterScan]));
      Down(VIRTUAL_KEY.VK_G, _letterScan);
      _gate.UsePolicy(HookPolicy.Holding(TriggerKeys.Space, [_letterScan]));

      Assert.Equal(Holding.Held, Down(VIRTUAL_KEY.VK_SPACE, 0x39));
    }

    private Holding Down(VIRTUAL_KEY key, ushort scanCode) {
      return _gate.Judge(
          RawInputKind.KeyDown, (ushort)key, scanCode, false, _sequence++, 1000, hasRoom: true);
    }
  }
}
