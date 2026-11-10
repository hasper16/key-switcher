using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace Rekey.Win32.Keyboard {
  /// <summary>
  /// Reads the session's last input from Windows and the hooks' last input from
  /// <see cref="InputHook" />.
  /// </summary>
  internal sealed class SessionInputActivity : IInputActivity {
    private readonly InputHook _hook;

    public SessionInputActivity(InputHook hook) {
      _hook = hook;
    }

    /// <inheritdoc />
    public long SystemLastInputTick {
      get {
        var info = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>() };
        long now = Environment.TickCount64;
        if (!PInvoke.GetLastInputInfo(ref info)) {
          return now;
        }

        // GetLastInputInfo counts in 32 bits; widen it by its distance from the 64-bit now.
        return now - unchecked((uint)now - info.dwTime);
      }
    }

    /// <inheritdoc />
    public long HookLastInputTick => _hook.LastInputTick;
  }
}
