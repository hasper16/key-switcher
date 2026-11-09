namespace Rekey.Core.Detection {
  /// <summary>
  /// Records one key press of a Word by key position, independent of the active Layout.
  /// </summary>
  /// <param name="ScanCode">The scan code of the key.</param>
  /// <param name="Shift">
  /// <see langword="true" /> if Shift was held; otherwise, <see langword="false" />.
  /// </param>
  /// <param name="CapsLock">
  /// <see langword="true" /> if Caps Lock was on; otherwise, <see langword="false" />.
  /// </param>
  /// <param name="AltGr">
  /// <see langword="true" /> if AltGr was held; otherwise, <see langword="false" />. The default
  /// is <see langword="false" />.
  /// </param>
  public readonly record struct Keystroke(
      ushort ScanCode, bool Shift, bool CapsLock, bool AltGr = false);
}
