using System.Collections.Frozen;
using Rekey.Core;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace Rekey.Win32.Keyboard {
  /// <summary>
  /// Tells the hook which keys it may hold back; the worker replaces the whole snapshot.
  /// </summary>
  internal sealed class HookPolicy {
    private readonly FrozenSet<ushort> _triggerKeys;
    private readonly FrozenSet<ushort> _characterScanCodes;

    /// <summary>
    /// Initializes a new instance of the <see cref="HookPolicy" /> class.
    /// </summary>
    /// <param name="triggerKeys">The virtual-key codes of the Trigger Keys to hold.</param>
    /// <param name="characterScanCodes">
    /// The scan codes of the keys that type a character in an active Layout.
    /// </param>
    public HookPolicy(IEnumerable<ushort> triggerKeys, IEnumerable<ushort> characterScanCodes) {
      _triggerKeys = triggerKeys.ToFrozenSet();
      _characterScanCodes = characterScanCodes.ToFrozenSet();
    }

    /// <summary>
    /// Gets the policy that holds nothing.
    /// </summary>
    public static HookPolicy HoldNothing { get; } = new([], []);

    /// <summary>
    /// Returns the policy that holds the given Trigger Keys.
    /// </summary>
    /// <remarks>
    /// The character scan codes are kept even when no key is held, so the hook already knows
    /// that a Word is being typed when the next policy starts holding.
    /// </remarks>
    /// <param name="keys">
    /// A bitwise combination of the enumeration values that specifies the Trigger Keys to hold.
    /// Both Enter keys send the same virtual-key code, so Enter covers the numeric keypad.
    /// </param>
    /// <param name="characterScanCodes">
    /// The scan codes of the keys that type a character in an active Layout.
    /// </param>
    public static HookPolicy Holding(TriggerKeys keys, IEnumerable<ushort> characterScanCodes) {
      (TriggerKeys Key, VIRTUAL_KEY Code)[] codes = [
          (TriggerKeys.Space, VIRTUAL_KEY.VK_SPACE),
          (TriggerKeys.Enter, VIRTUAL_KEY.VK_RETURN),
          (TriggerKeys.Tab, VIRTUAL_KEY.VK_TAB)];
      return new HookPolicy(
          codes.Where(code => (keys & code.Key) != 0).Select(code => (ushort)code.Code),
          characterScanCodes);
    }

    public bool IsTrigger(ushort virtualKey) {
      return _triggerKeys.Contains(virtualKey);
    }

    public bool IsCharacter(ushort scanCode, bool extended) {
      return !extended && _characterScanCodes.Contains(scanCode);
    }
  }
}
