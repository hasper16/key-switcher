using System.Text;
using Windows.Win32;
using Windows.Win32.System.StationsAndDesktops;

namespace Rekey.Win32.Keyboard {
  /// <summary>
  /// Tells whether the desktop that receives input hides it from Rekey's hooks.
  /// </summary>
  /// <remarks>
  /// Low-level hooks see input only on the user's default desktop. On the secure desktop (UAC
  /// prompts, the lock screen, Ctrl+Alt+Del) input still moves the session's last-input time,
  /// so the hook watchdog must not read it as a dropped hook.
  /// </remarks>
  internal static class InputDesktop {
    private const string _default = "Default";

    /// <summary>Returns whether input now goes to a desktop the hooks cannot see.</summary>
    public static bool HidesInputNow() {
      return HidesInputFromHooks(CurrentName());
    }

    /// <summary>
    /// Returns whether a desktop with this name hides input from the hooks; a desktop Rekey
    /// cannot open or name (<see langword="null" />) counts as hiding it.
    /// </summary>
    public static bool HidesInputFromHooks(string? name) {
      return !string.Equals(name, _default, StringComparison.OrdinalIgnoreCase);
    }

    private static string? CurrentName() {
      using CloseDesktopSafeHandle desktop = PInvoke.OpenInputDesktop_SafeHandle(
          0, false, DESKTOP_ACCESS_FLAGS.DESKTOP_READOBJECTS);
      if (desktop.IsInvalid) {
        return null;
      }

      Span<byte> name = stackalloc byte[128];
      if (!PInvoke.GetUserObjectInformation(
          desktop, USER_OBJECT_INFORMATION_INDEX.UOI_NAME, name, out uint length)) {
        return null;
      }

      // UOI_NAME is a null-terminated UTF-16 string.
      string text = Encoding.Unicode.GetString(name[..(int)Math.Min(length, (uint)name.Length)]);
      return text.TrimEnd('\0');
    }
  }
}
