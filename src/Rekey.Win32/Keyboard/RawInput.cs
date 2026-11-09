namespace Rekey.Win32.Keyboard {
  /// <summary>
  /// Holds one input exactly as a low-level hook reported it, before the worker interprets it.
  /// </summary>
  /// <param name="Kind">One of the enumeration values that specifies what the hook saw.</param>
  /// <param name="VirtualKey">The virtual-key code; zero for a mouse button.</param>
  /// <param name="ScanCode">The hardware scan code; zero for a mouse button.</param>
  /// <param name="Extended">
  /// <see langword="true" /> if the key is an extended key, such as an arrow key outside the
  /// numeric keypad; otherwise, <see langword="false" />.
  /// </param>
  /// <param name="Tick">The milliseconds since system start when the hook saw the input.</param>
  internal readonly record struct RawInput(
      RawInputKind Kind, ushort VirtualKey, ushort ScanCode, bool Extended, long Tick);
}
