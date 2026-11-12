namespace Rekey.Core {
  /// <summary>
  /// Counts the Words a configured Trigger Key ended while Auto Correction applied, to measure
  /// how many get no evaluation because a key could not be confirmed outside a Password Field.
  /// </summary>
  /// <param name="Ended">The Words ended, known to Rekey or dropped for an unconfirmed key.</param>
  /// <param name="Evaluated">The Words evaluated.</param>
  /// <param name="Unconfirmed">
  /// The Words not evaluated because one of their keys could not be confirmed.
  /// </param>
  public readonly record struct WordCounts(long Ended, long Evaluated, long Unconfirmed);
}
