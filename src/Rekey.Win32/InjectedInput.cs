namespace Rekey.Win32 {
  /// <summary>
  /// Marks the input Rekey injects itself, so its hook never reads it as user typing.
  /// </summary>
  public static class InjectedInput {
    /// <summary>
    /// The <c>dwExtraInfo</c> value of every key Rekey injects with <c>SendInput</c> ("REKY").
    /// </summary>
    public const nuint Tag = 0x52454B59;
  }
}
