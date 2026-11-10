using System.Drawing;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace Rekey.DesktopChecks {
  // Injects keys and clicks the way a keyboard and mouse send them, with real scan codes.
  internal static class Input {
    private static readonly TimeSpan _keyGap = TimeSpan.FromMilliseconds(40);

    // US positions of the letters the checks type; the text is invented.
    private static readonly Dictionary<char, ushort> _scanCodes = new() {
      ['q'] = 0x10,
      ['w'] = 0x11,
      ['e'] = 0x12,
      ['r'] = 0x13,
      ['t'] = 0x14,
      ['y'] = 0x15,
      ['a'] = 0x1E,
      ['s'] = 0x1F,
      ['d'] = 0x20,
      ['f'] = 0x21,
      ['g'] = 0x22,
      ['h'] = 0x23,
    };

    public static void Type(string letters) {
      foreach (char letter in letters) {
        Press((VIRTUAL_KEY)char.ToUpperInvariant(letter), _scanCodes[letter]);
      }
    }

    public static void Press(VIRTUAL_KEY key, ushort scanCode, nuint extraInfo = 0) {
      Key(key, scanCode, up: false, extraInfo);
      Key(key, scanCode, up: true, extraInfo);
      Thread.Sleep(_keyGap);
    }

    public static void Key(VIRTUAL_KEY key, ushort scanCode, bool up, nuint extraInfo = 0) {
      var input = new INPUT {
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
      Send(input);
    }

    public static void Space() {
      Press(VIRTUAL_KEY.VK_SPACE, 0x39);
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
      Key(VIRTUAL_KEY.VK_LCONTROL, 0x1D, up: false);
      Press(VIRTUAL_KEY.VK_W, 0x11);
      Key(VIRTUAL_KEY.VK_LCONTROL, 0x1D, up: true);
    }

    public static void Click(Point at) {
      PInvoke.SetCursorPos(at.X, at.Y);
      Thread.Sleep(_keyGap);
      Mouse(MOUSE_EVENT_FLAGS.MOUSEEVENTF_LEFTDOWN);
      Mouse(MOUSE_EVENT_FLAGS.MOUSEEVENTF_LEFTUP);
      Thread.Sleep(TimeSpan.FromMilliseconds(300));
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
