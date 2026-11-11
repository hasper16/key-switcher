using System.Drawing;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace Rekey.DesktopChecks {
  // Injects keys and clicks the way a keyboard and mouse send them, with real scan codes.
  internal static class Input {
    private static readonly TimeSpan _keyGap = TimeSpan.FromMilliseconds(40);

    // US positions of the keys the checks type; the text is invented or a spec example.
    private static readonly Dictionary<char, (VIRTUAL_KEY Key, ushort ScanCode)> _keys = new() {
      ['q'] = (VIRTUAL_KEY.VK_Q, 0x10),
      ['w'] = (VIRTUAL_KEY.VK_W, 0x11),
      ['e'] = (VIRTUAL_KEY.VK_E, 0x12),
      ['r'] = (VIRTUAL_KEY.VK_R, 0x13),
      ['t'] = (VIRTUAL_KEY.VK_T, 0x14),
      ['y'] = (VIRTUAL_KEY.VK_Y, 0x15),
      ['p'] = (VIRTUAL_KEY.VK_P, 0x19),
      ['a'] = (VIRTUAL_KEY.VK_A, 0x1E),
      ['s'] = (VIRTUAL_KEY.VK_S, 0x1F),
      ['d'] = (VIRTUAL_KEY.VK_D, 0x20),
      ['f'] = (VIRTUAL_KEY.VK_F, 0x21),
      ['g'] = (VIRTUAL_KEY.VK_G, 0x22),
      ['h'] = (VIRTUAL_KEY.VK_H, 0x23),
      ['z'] = (VIRTUAL_KEY.VK_Z, 0x2C),
      ['x'] = (VIRTUAL_KEY.VK_X, 0x2D),
      ['b'] = (VIRTUAL_KEY.VK_B, 0x30),
      ['n'] = (VIRTUAL_KEY.VK_N, 0x31),
      ['.'] = (VIRTUAL_KEY.VK_OEM_PERIOD, 0x34),
      [' '] = (VIRTUAL_KEY.VK_SPACE, 0x39),
    };

    public static void Type(string letters, nuint extraInfo = 0) {
      foreach (char letter in letters) {
        (VIRTUAL_KEY key, ushort scanCode) = _keys[letter];
        Press(key, scanCode, extraInfo);
      }
    }

    // Sends every key in one SendInput call, faster than any typist, so the later keys arrive
    // while Rekey is still applying a Correction.
    public static void TypeAtOnce(string letters) {
      INPUT[] inputs = [.. letters.SelectMany(letter => {
        (VIRTUAL_KEY key, ushort scanCode) = _keys[letter];
        return new[] { KeyInput(key, scanCode, false, 0), KeyInput(key, scanCode, true, 0) };
      })];
      PInvoke.SendInput(inputs, Marshal.SizeOf<INPUT>());
      Thread.Sleep(_keyGap);
    }

    public static void Press(VIRTUAL_KEY key, ushort scanCode, nuint extraInfo = 0) {
      Key(key, scanCode, up: false, extraInfo);
      Key(key, scanCode, up: true, extraInfo);
      Thread.Sleep(_keyGap);
    }

    public static void Key(VIRTUAL_KEY key, ushort scanCode, bool up, nuint extraInfo = 0) {
      Send(KeyInput(key, scanCode, up, extraInfo));
    }

    public static void Space() {
      Press(VIRTUAL_KEY.VK_SPACE, 0x39);
    }

    public static void Enter() {
      Press(VIRTUAL_KEY.VK_RETURN, 0x1C);
    }

    public static void Escape() {
      Press(VIRTUAL_KEY.VK_ESCAPE, 0x01);
    }

    public static void Tab(bool shift = false) {
      if (shift) {
        Key(VIRTUAL_KEY.VK_LSHIFT, 0x2A, up: false);
      }

      Press(VIRTUAL_KEY.VK_TAB, 0x0F);
      if (shift) {
        Key(VIRTUAL_KEY.VK_LSHIFT, 0x2A, up: true);
      }
    }

    public static void ToggleCapsLock() {
      Press(VIRTUAL_KEY.VK_CAPITAL, 0x3A);
    }

    public static void CloseTab() {
      Control('w');
    }

    // Ctrl with a letter key, such as Ctrl+Z.
    public static void Control(char letter) {
      (VIRTUAL_KEY key, ushort scanCode) = _keys[letter];
      Key(VIRTUAL_KEY.VK_LCONTROL, 0x1D, up: false);
      Press(key, scanCode);
      Key(VIRTUAL_KEY.VK_LCONTROL, 0x1D, up: true);
      Thread.Sleep(_keyGap);
    }

    // Empties the focused text box.
    public static void ClearBox() {
      Control('a');
      Press(VIRTUAL_KEY.VK_DELETE, 0x53);
    }

    public static void Click(Point at) {
      PInvoke.SetCursorPos(at.X, at.Y);
      Thread.Sleep(_keyGap);
      Mouse(MOUSE_EVENT_FLAGS.MOUSEEVENTF_LEFTDOWN);
      Mouse(MOUSE_EVENT_FLAGS.MOUSEEVENTF_LEFTUP);
      Thread.Sleep(TimeSpan.FromMilliseconds(300));
    }

    private static INPUT KeyInput(VIRTUAL_KEY key, ushort scanCode, bool up, nuint extraInfo) {
      return new INPUT {
        type = INPUT_TYPE.INPUT_KEYBOARD,
        Anonymous = new INPUT._Anonymous_e__Union {
          ki = new KEYBDINPUT {
            wVk = key,
            wScan = scanCode,
            dwFlags = up ? KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP : 0,
            dwExtraInfo = extraInfo,
          },
        },
      };
    }

    private static void Mouse(MOUSE_EVENT_FLAGS flags) {
      Send(new INPUT {
        type = INPUT_TYPE.INPUT_MOUSE,
        Anonymous = new INPUT._Anonymous_e__Union { mi = new MOUSEINPUT { dwFlags = flags } },
      });
    }

    private static void Send(INPUT input) {
      PInvoke.SendInput([input], Marshal.SizeOf<INPUT>());
    }
  }
}
