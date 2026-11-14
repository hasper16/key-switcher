using Rekey.Core;
using Rekey.Win32.Keyboard;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Xunit;

namespace Rekey.Win32.Tests.Keyboard {
  // Design D0 and D1 of add-manual-correction: which keys perform the Hotkey on the hook
  // thread, and which of them the application still gets.
  public sealed class HotkeyDetectorTests {
    private static readonly Hotkey _ctrlAltK =
        Hotkey.Custom(KeyModifiers.Control | KeyModifiers.Alt, (ushort)VIRTUAL_KEY.VK_K);

    private readonly HashSet<VIRTUAL_KEY> _physicallyDown = [];
    private long _tick = 1_000;

    [Fact]
    public void ExactComboFiresAndSwallowsItsKey() {
      HotkeyDetector detector = For(_ctrlAltK);
      Down(detector, VIRTUAL_KEY.VK_LCONTROL);
      Down(detector, VIRTUAL_KEY.VK_LMENU);

      Assert.Equal(HotkeyVerdict.Fire, Down(detector, VIRTUAL_KEY.VK_K));
    }

    [Fact]
    public void RightHandModifiersCountToo() {
      HotkeyDetector detector = For(_ctrlAltK);
      Down(detector, VIRTUAL_KEY.VK_RCONTROL);
      Down(detector, VIRTUAL_KEY.VK_RMENU);

      Assert.Equal(HotkeyVerdict.Fire, Down(detector, VIRTUAL_KEY.VK_K));
    }

    [Fact]
    public void ComboKeyRepeatsAndReleaseAreSwallowed() {
      HotkeyDetector detector = For(_ctrlAltK);
      Down(detector, VIRTUAL_KEY.VK_LCONTROL);
      Down(detector, VIRTUAL_KEY.VK_LMENU);
      Down(detector, VIRTUAL_KEY.VK_K);

      Assert.Equal(HotkeyVerdict.Swallow, Down(detector, VIRTUAL_KEY.VK_K));
      Assert.Equal(HotkeyVerdict.Swallow, Up(detector, VIRTUAL_KEY.VK_K));
      Assert.Equal(HotkeyVerdict.None, Up(detector, VIRTUAL_KEY.VK_LMENU));
    }

    [Fact]
    public void ReleaseAfterModifiersWentUpIsStillSwallowed() {
      HotkeyDetector detector = For(_ctrlAltK);
      Down(detector, VIRTUAL_KEY.VK_LCONTROL);
      Down(detector, VIRTUAL_KEY.VK_LMENU);
      Down(detector, VIRTUAL_KEY.VK_K);
      Up(detector, VIRTUAL_KEY.VK_LMENU);
      Up(detector, VIRTUAL_KEY.VK_LCONTROL);

      Assert.Equal(HotkeyVerdict.Swallow, Up(detector, VIRTUAL_KEY.VK_K));
      Assert.Equal(HotkeyVerdict.None, Down(detector, VIRTUAL_KEY.VK_K));
    }

    [Fact]
    public void ExtraModifierDoesNotFire() {
      HotkeyDetector detector = For(_ctrlAltK);
      Down(detector, VIRTUAL_KEY.VK_LCONTROL);
      Down(detector, VIRTUAL_KEY.VK_LMENU);
      Down(detector, VIRTUAL_KEY.VK_LSHIFT);

      Assert.Equal(HotkeyVerdict.None, Down(detector, VIRTUAL_KEY.VK_K));
    }

    [Fact]
    public void MissingModifierDoesNotFire() {
      HotkeyDetector detector = For(_ctrlAltK);
      Down(detector, VIRTUAL_KEY.VK_LCONTROL);

      Assert.Equal(HotkeyVerdict.None, Down(detector, VIRTUAL_KEY.VK_K));
    }

    [Fact]
    public void OtherKeyWithTheModifiersDoesNotFire() {
      HotkeyDetector detector = For(_ctrlAltK);
      Down(detector, VIRTUAL_KEY.VK_LCONTROL);
      Down(detector, VIRTUAL_KEY.VK_LMENU);

      Assert.Equal(HotkeyVerdict.None, Down(detector, VIRTUAL_KEY.VK_L));
    }

    [Fact]
    public void ComboKeyAloneDoesNotFire() {
      Assert.Equal(HotkeyVerdict.None, Down(For(_ctrlAltK), VIRTUAL_KEY.VK_K));
    }

    [Fact]
    public void ModifierTheHookSawGoDownButIsUpNowDoesNotFire() {
      // A key-up lost to the secure desktop must not leave Ctrl stuck and eat the key.
      HotkeyDetector detector = For(Hotkey.CtrlSpace);
      Down(detector, VIRTUAL_KEY.VK_LCONTROL);
      _physicallyDown.Clear();

      Assert.Equal(HotkeyVerdict.None, Down(detector, VIRTUAL_KEY.VK_SPACE));
    }

    [Fact]
    public void OtherSideOfAModifierStillHolds() {
      HotkeyDetector detector = For(Hotkey.CtrlSpace);
      Down(detector, VIRTUAL_KEY.VK_LCONTROL);
      Down(detector, VIRTUAL_KEY.VK_RCONTROL);
      Up(detector, VIRTUAL_KEY.VK_LCONTROL);

      Assert.Equal(HotkeyVerdict.Fire, Down(detector, VIRTUAL_KEY.VK_SPACE));
    }

    [Fact]
    public void RepeatOfAKeyThatWentOnDoesNotFire() {
      // Space went down to the application before Ctrl, so its release must go there too.
      HotkeyDetector detector = For(Hotkey.CtrlSpace);
      Down(detector, VIRTUAL_KEY.VK_SPACE);
      Down(detector, VIRTUAL_KEY.VK_LCONTROL);

      Assert.Equal(HotkeyVerdict.None, Down(detector, VIRTUAL_KEY.VK_SPACE));
      Assert.Equal(HotkeyVerdict.None, Up(detector, VIRTUAL_KEY.VK_SPACE));
      Assert.Equal(HotkeyVerdict.Fire, Down(detector, VIRTUAL_KEY.VK_SPACE));
    }

    [Fact]
    public void RepeatOfAKeyThatCouldNotFireDoesNotFireLater() {
      HotkeyDetector detector = For(Hotkey.CtrlSpace);
      Down(detector, VIRTUAL_KEY.VK_LCONTROL);
      _tick += 40;
      detector.Observe((ushort)VIRTUAL_KEY.VK_SPACE, down: true, _tick, canFire: false);

      Assert.Equal(HotkeyVerdict.None, Down(detector, VIRTUAL_KEY.VK_SPACE));
    }

    [Fact]
    public void CtrlSpaceFires() {
      HotkeyDetector detector = For(Hotkey.CtrlSpace);
      Down(detector, VIRTUAL_KEY.VK_LCONTROL);

      Assert.Equal(HotkeyVerdict.Fire, Down(detector, VIRTUAL_KEY.VK_SPACE));
    }

    [Fact]
    public void WinSpaceSwallowsSpaceWhileWinIsDown() {
      HotkeyDetector detector = For(Hotkey.WinSpace);
      Down(detector, VIRTUAL_KEY.VK_LWIN);

      Assert.Equal(HotkeyVerdict.Fire, Down(detector, VIRTUAL_KEY.VK_SPACE));
      Assert.Equal(HotkeyVerdict.Swallow, Up(detector, VIRTUAL_KEY.VK_SPACE));
    }

    [Fact]
    public void WinAloneIsUntouched() {
      HotkeyDetector detector = For(Hotkey.WinSpace);

      Assert.Equal(HotkeyVerdict.None, Down(detector, VIRTUAL_KEY.VK_LWIN));
      Assert.Equal(HotkeyVerdict.None, Up(detector, VIRTUAL_KEY.VK_LWIN));
    }

    [Theory]
    [InlineData((ushort)VIRTUAL_KEY.VK_E)]
    [InlineData((ushort)VIRTUAL_KEY.VK_TAB)]
    public void OtherWinShortcutsAreUntouched(ushort key) {
      HotkeyDetector detector = For(Hotkey.WinSpace);
      Down(detector, VIRTUAL_KEY.VK_LWIN);

      Assert.Equal(HotkeyVerdict.None, Down(detector, (VIRTUAL_KEY)key));
    }

    [Fact]
    public void WinShiftSpaceIsUntouched() {
      // Windows switches to the previous Layout with it.
      HotkeyDetector detector = For(Hotkey.WinSpace);
      Down(detector, VIRTUAL_KEY.VK_LWIN);
      Down(detector, VIRTUAL_KEY.VK_LSHIFT);

      Assert.Equal(HotkeyVerdict.None, Down(detector, VIRTUAL_KEY.VK_SPACE));
    }

    [Fact]
    public void SpaceIsUntouchedWhenTheHotkeyIsADoubleTap() {
      HotkeyDetector detector = For(Hotkey.DoubleShift);
      Down(detector, VIRTUAL_KEY.VK_LWIN);

      Assert.Equal(HotkeyVerdict.None, Down(detector, VIRTUAL_KEY.VK_SPACE));
    }

    [Fact]
    public void DoubleTapOfShiftFiresOnTheSecondReleaseAndLetsItThrough() {
      HotkeyDetector detector = For(Hotkey.DoubleShift);
      Tap(detector, VIRTUAL_KEY.VK_LSHIFT);
      Assert.Equal(HotkeyVerdict.None, Down(detector, VIRTUAL_KEY.VK_RSHIFT));

      Assert.Equal(HotkeyVerdict.Tap, Up(detector, VIRTUAL_KEY.VK_RSHIFT));
    }

    [Fact]
    public void DoubleTapOfCtrlFires() {
      HotkeyDetector detector = For(Hotkey.DoubleCtrl);
      Tap(detector, VIRTUAL_KEY.VK_LCONTROL);
      Down(detector, VIRTUAL_KEY.VK_LCONTROL);

      Assert.Equal(HotkeyVerdict.Tap, Up(detector, VIRTUAL_KEY.VK_LCONTROL));
    }

    [Fact]
    public void DoubleTapOfAltFires() {
      HotkeyDetector detector = For(Hotkey.DoubleAlt);
      Tap(detector, VIRTUAL_KEY.VK_RMENU);
      Down(detector, VIRTUAL_KEY.VK_LMENU);

      Assert.Equal(HotkeyVerdict.Tap, Up(detector, VIRTUAL_KEY.VK_LMENU));
    }

    [Fact]
    public void ShiftForACapitalBetweenTapsDoesNotFire() {
      HotkeyDetector detector = For(Hotkey.DoubleShift);
      Down(detector, VIRTUAL_KEY.VK_LSHIFT);
      Down(detector, VIRTUAL_KEY.VK_G);
      Up(detector, VIRTUAL_KEY.VK_G);
      Up(detector, VIRTUAL_KEY.VK_LSHIFT);
      Down(detector, VIRTUAL_KEY.VK_LSHIFT);

      Assert.Equal(HotkeyVerdict.None, Up(detector, VIRTUAL_KEY.VK_LSHIFT));
    }

    private HotkeyDetector For(Hotkey hotkey) {
      return new HotkeyDetector(hotkey, key => _physicallyDown.Contains(key));
    }

    private void Tap(HotkeyDetector detector, VIRTUAL_KEY key) {
      Down(detector, key);
      Up(detector, key);
    }

    private HotkeyVerdict Down(HotkeyDetector detector, VIRTUAL_KEY key) {
      _physicallyDown.Add(key);
      _tick += 40;
      return detector.Observe((ushort)key, down: true, _tick);
    }

    private HotkeyVerdict Up(HotkeyDetector detector, VIRTUAL_KEY key) {
      _physicallyDown.Remove(key);
      _tick += 40;
      return detector.Observe((ushort)key, down: false, _tick);
    }
  }
}
