using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using Rekey.Core.Detection;
using Rekey.Core.Languages;
using Windows.Win32;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace Rekey.Win32 {
  /// <summary>
  /// Reads what the printable keys of a Layout type, using <c>ToUnicodeEx</c>.
  /// </summary>
  public static class KeyMapReader {
    private const byte _keyDown = 0x80;
    private const byte _toggledOn = 0x01;

    // Bit 2 of ToUnicodeEx flags: leave the keyboard state alone, so a dead key read here does
    // not change the next character the user types.
    private const uint _keepKeyboardState = 0x4;

    /// <summary>
    /// Gets the scan codes of the 47 printable keys of a standard keyboard, from the key left of
    /// 1 through the slash key.
    /// </summary>
    public static IReadOnlyList<ushort> PrintableScanCodes { get; } = [
        0x29, .. Range(0x02, 0x0D), .. Range(0x10, 0x1B), 0x2B, .. Range(0x1E, 0x28),
        .. Range(0x2C, 0x35)];

    /// <summary>
    /// Reads the printable keys of a Layout.
    /// </summary>
    /// <remarks>
    /// A Layout that is not loaded in the session is loaded for the read and unloaded afterwards.
    /// </remarks>
    /// <param name="layout">The Layout to read.</param>
    /// <returns>The Key Map of the Layout.</returns>
    /// <exception cref="Win32Exception">Windows cannot load the Layout.</exception>
    public static KeyMap Read(LayoutId layout) {
      int loadedBefore = LoadedLayoutCount();
      string klid = layout.Value.ToString("X8", CultureInfo.InvariantCulture);
      using UnloadKeyboardLayoutSafeHandle hkl = PInvoke.LoadKeyboardLayout(
          klid, ACTIVATE_KEYBOARD_LAYOUT_FLAGS.KLF_NOTELLSHELL);
      if (hkl.IsInvalid) {
        throw new Win32Exception(Marshal.GetLastPInvokeError());
      }

      bool loadedHere = LoadedLayoutCount() > loadedBefore;
      try {
        return new KeyMap(layout, PrintableScanCodes.Select(scanCode => ReadKey(hkl, scanCode)));
      } finally {
        // The handle unloads the Layout when disposed; keep a Layout the session already had.
        if (!loadedHere) {
          hkl.SetHandleAsInvalid();
        }
      }
    }

    private static IEnumerable<ushort> Range(ushort first, ushort last) {
      return Enumerable.Range(first, last - first + 1).Select(code => (ushort)code);
    }

    private static int LoadedLayoutCount() {
      return PInvoke.GetKeyboardLayoutList([]);
    }

    private static KeyMapEntry ReadKey(SafeHandle hkl, ushort scanCode) {
      uint virtualKey = PInvoke.MapVirtualKeyEx(
          scanCode, MAP_VIRTUAL_KEY_TYPE.MAPVK_VSC_TO_VK, hkl);
      (string normal, bool deadNormal) = Type(hkl, virtualKey, scanCode, shift: false, caps: false);
      (string shifted, bool deadShifted) =
          Type(hkl, virtualKey, scanCode, shift: true, caps: false);
      (string capsLocked, _) = Type(hkl, virtualKey, scanCode, shift: false, caps: true);
      bool capsLock = capsLocked == shifted && capsLocked != normal;
      return new KeyMapEntry(scanCode, normal, shifted, capsLock, deadNormal, deadShifted);
    }

    private static (string Text, bool Dead) Type(
        SafeHandle hkl, uint virtualKey, ushort scanCode, bool shift, bool caps) {
      Span<byte> state = stackalloc byte[256];
      state.Clear();
      if (shift) {
        state[(int)VIRTUAL_KEY.VK_SHIFT] = _keyDown;
      }

      if (caps) {
        state[(int)VIRTUAL_KEY.VK_CAPITAL] = _toggledOn;
      }

      Span<char> buffer = stackalloc char[8];
      int written = PInvoke.ToUnicodeEx(
          virtualKey, scanCode, state, buffer, _keepKeyboardState, hkl);
      return written < 0 ? (buffer[..1].ToString(), true) : (buffer[..written].ToString(), false);
    }
  }
}
