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
  /// <param name="Sequence">
  /// The input's position in the order the hook wrote it; <see cref="InputRing" /> sets it.
  /// </param>
  /// <param name="Actions">
  /// The number of key-downs and mouse-button presses written up to and including this input;
  /// <see cref="InputRing" /> sets it. Only those inputs can move focus.
  /// </param>
  internal readonly record struct RawInput(
      RawInputKind Kind,
      ushort VirtualKey,
      ushort ScanCode,
      bool Extended,
      long Tick,
      long Sequence = 0,
      long Actions = 0);
}
