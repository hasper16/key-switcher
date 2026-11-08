namespace Rekey.Core.Detection {
  /// <summary>
  /// Holds the numbers the Decision policy uses at one Sensitivity.
  /// </summary>
  /// <remarks>
  /// All values are design decisions tuned with the accuracy harness (design D3); tuning
  /// changes only this record.
  /// </remarks>
  /// <param name="GibberishCeiling">
  /// The lowest Score at which a Rendering counts as plausible text.
  /// </param>
  /// <param name="MinWordMargin">
  /// How much the target's Score must exceed the current Rendering's Score to correct to a word.
  /// </param>
  /// <param name="StrongLetterScore">
  /// The lowest Score of a non-word target that counts as strong letter-frequency evidence.
  /// </param>
  /// <param name="OnStrongLetters">
  /// What to do when the target is not a word but its Score reaches
  /// <paramref name="StrongLetterScore" />.
  /// </param>
  /// <param name="OnWeakLetters">
  /// What to do when the target is a plausible non-word below
  /// <paramref name="StrongLetterScore" />.
  /// </param>
  /// <param name="CorrectsShortWords">
  /// <see langword="true" /> to correct a one- or two-letter Word on its own; otherwise,
  /// <see langword="false" /> to defer it.
  /// </param>
  public sealed record DetectionThresholds(
      double GibberishCeiling,
      double MinWordMargin,
      double StrongLetterScore,
      DecisionKind OnStrongLetters,
      DecisionKind OnWeakLetters,
      bool CorrectsShortWords) {
    private static readonly DetectionThresholds _cautious = new(
        0.35, 0.30, 0.80, DecisionKind.Defer, DecisionKind.Defer, false);

    private static readonly DetectionThresholds _balanced = new(
        0.35, 0, 0.80, DecisionKind.Defer, DecisionKind.Keep, false);

    private static readonly DetectionThresholds _aggressive = new(
        0.35, 0, 0.80, DecisionKind.Correct, DecisionKind.Keep, true);

    /// <summary>
    /// Returns the thresholds of a Sensitivity.
    /// </summary>
    /// <param name="sensitivity">
    /// One of the enumeration values that specifies the Sensitivity.
    /// </param>
    /// <returns>The thresholds.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The value is not a defined Sensitivity.
    /// </exception>
    public static DetectionThresholds For(Sensitivity sensitivity) {
      return sensitivity switch {
        Sensitivity.Cautious => _cautious,
        Sensitivity.Balanced => _balanced,
        Sensitivity.Aggressive => _aggressive,
        _ => throw new ArgumentOutOfRangeException(nameof(sensitivity)),
      };
    }
  }
}
