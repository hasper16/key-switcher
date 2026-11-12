namespace Rekey.Win32.Keyboard {
  /// <summary>
  /// Counts the keys that could not be confirmed outside a Password Field, by cause; for
  /// diagnosing typing that builds no Word.
  /// </summary>
  /// <param name="Queued">Keys read while a later key or click was already waiting.</param>
  /// <param name="DuringCheck">Keys for which a later key arrived during the check.</param>
  /// <param name="Password">Keys the check placed in a Password Field, or could not place.</param>
  public readonly record struct KeyRejects(long Queued, long DuringCheck, long Password);
}
