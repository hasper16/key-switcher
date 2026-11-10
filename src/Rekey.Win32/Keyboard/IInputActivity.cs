namespace Rekey.Win32.Keyboard {
  /// <summary>
  /// Tells when the system and Rekey's hook last saw input, in milliseconds since system start.
  /// </summary>
  internal interface IInputActivity {
    /// <summary>
    /// Gets the tick of the last input anywhere in the session.
    /// </summary>
    long SystemLastInputTick { get; }

    /// <summary>
    /// Gets the tick of the last input Rekey's hooks saw.
    /// </summary>
    long HookLastInputTick { get; }
  }
}
