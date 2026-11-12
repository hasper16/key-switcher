using System.Collections.Frozen;

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

    public bool IsTrigger(ushort virtualKey) {
      return _triggerKeys.Contains(virtualKey);
    }

    public bool IsCharacter(ushort scanCode, bool extended) {
      return !extended && _characterScanCodes.Contains(scanCode);
    }
  }
}
