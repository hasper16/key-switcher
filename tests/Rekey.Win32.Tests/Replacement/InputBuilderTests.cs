using Rekey.Win32.Keyboard;
using Rekey.Win32.Replacement;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Xunit;

namespace Rekey.Win32.Tests.Replacement {
  // Design D2: what one replacement and one re-injected key send to Windows.
  public sealed class InputBuilderTests {
    [Fact]
    public void ReplacementErasesThenTypesTheText() {
      INPUT[] inputs = InputBuilder.Replacement(erase: 2, "пр", []);

      Assert.Equal(
          [
            Key(VIRTUAL_KEY.VK_BACK, up: false), Key(VIRTUAL_KEY.VK_BACK, up: true),
            Key(VIRTUAL_KEY.VK_BACK, up: false), Key(VIRTUAL_KEY.VK_BACK, up: true),
            Unicode('п', up: false), Unicode('п', up: true),
            Unicode('р', up: false), Unicode('р', up: true),
          ],
          inputs.Select(Describe));
    }

    [Fact]
    public void ReplacementReleasesHeldShortcutModifiersAroundTheText() {
      INPUT[] inputs = InputBuilder.Replacement(
          erase: 0, "a", [VIRTUAL_KEY.VK_LCONTROL, VIRTUAL_KEY.VK_RMENU]);

      Assert.Equal(
          [
            Key(VIRTUAL_KEY.VK_LCONTROL, up: true), Key(VIRTUAL_KEY.VK_RMENU, up: true),
            Unicode('a', up: false), Unicode('a', up: true),
            Key(VIRTUAL_KEY.VK_LCONTROL, up: false), Key(VIRTUAL_KEY.VK_RMENU, up: false),
          ],
          inputs.Select(Describe));
    }

    [Fact]
    public void EveryInputCarriesTheRekeyTag() {
      INPUT[] inputs = InputBuilder.Replacement(1, "a", [VIRTUAL_KEY.VK_LWIN]);

      Assert.All(inputs, input => Assert.Equal(InjectedInput.Tag, input.Anonymous.ki.dwExtraInfo));
    }

    [Fact]
    public void HeldKeyIsReinjectedByScanCode() {
      INPUT input = InputBuilder.Reinject(
          new RawInput(RawInputKind.KeyDown, (ushort)VIRTUAL_KEY.VK_SPACE, 0x39, false, 0));

      Assert.Equal((0, (ushort)0x39, KEYBD_EVENT_FLAGS.KEYEVENTF_SCANCODE), Fields(input));
      Assert.Equal(InjectedInput.Tag, input.Anonymous.ki.dwExtraInfo);
    }

    [Fact]
    public void HeldExtendedKeyUpKeepsItsFlags() {
      INPUT input = InputBuilder.Reinject(
          new RawInput(RawInputKind.KeyUp, (ushort)VIRTUAL_KEY.VK_RCONTROL, 0x1D, true, 0));

      Assert.Equal(
          (0, (ushort)0x1D, KEYBD_EVENT_FLAGS.KEYEVENTF_SCANCODE
              | KEYBD_EVENT_FLAGS.KEYEVENTF_EXTENDEDKEY | KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP),
          Fields(input));
    }

    [Fact]
    public void HeldUndoRequestIsReinjectedAsAKeyDown() {
      INPUT input = InputBuilder.Reinject(new RawInput(
          RawInputKind.KeyDown,
          (ushort)VIRTUAL_KEY.VK_Z,
          0x2C,
          false,
          0,
          Holding: Holding.UndoRequest));

      Assert.Equal((0, (ushort)0x2C, KEYBD_EVENT_FLAGS.KEYEVENTF_SCANCODE), Fields(input));
    }

    [Fact]
    public void KeyWithoutAScanCodeIsReinjectedByVirtualKey() {
      INPUT input = InputBuilder.Reinject(new RawInput(RawInputKind.KeyDown, 0x41, 0, false, 0));

      Assert.Equal((0x41, (ushort)0, (KEYBD_EVENT_FLAGS)0), Fields(input));
    }

    private static string Key(VIRTUAL_KEY key, bool up) {
      return $"{key} {(up ? "up" : "down")}";
    }

    private static string Unicode(char character, bool up) {
      return $"U+{(int)character:X4} {(up ? "up" : "down")}";
    }

    private static string Describe(INPUT input) {
      KEYBDINPUT key = input.Anonymous.ki;
      bool up = (key.dwFlags & KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP) != 0;
      return (key.dwFlags & KEYBD_EVENT_FLAGS.KEYEVENTF_UNICODE) != 0
          ? Unicode((char)key.wScan, up)
          : Key(key.wVk, up);
    }

    private static (int Vk, ushort Scan, KEYBD_EVENT_FLAGS Flags) Fields(INPUT input) {
      KEYBDINPUT key = input.Anonymous.ki;
      return ((int)key.wVk, key.wScan, key.dwFlags);
    }
  }
}
