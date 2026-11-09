namespace Rekey.Core.Detection {
  /// <summary>
  /// Holds what one key types in a Layout without and with Shift.
  /// </summary>
  /// <param name="ScanCode">The scan code of the key.</param>
  /// <param name="Normal">The text the key types without Shift.</param>
  /// <param name="Shifted">The text the key types with Shift.</param>
  /// <param name="CapsLock">
  /// <see langword="true" /> if Caps Lock swaps the two texts, as it does for letters; otherwise,
  /// <see langword="false" />.
  /// </param>
  /// <param name="DeadNormal">
  /// <see langword="true" /> if the key is a dead key without Shift; otherwise,
  /// <see langword="false" />.
  /// </param>
  /// <param name="DeadShifted">
  /// <see langword="true" /> if the key is a dead key with Shift; otherwise,
  /// <see langword="false" />.
  /// </param>
  public sealed record KeyMapEntry(
      ushort ScanCode,
      string Normal,
      string Shifted,
      bool CapsLock,
      bool DeadNormal,
      bool DeadShifted);
}
