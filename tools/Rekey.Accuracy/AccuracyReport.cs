using Rekey.Core.Detection;

namespace Rekey.Accuracy {
  /// <summary>
  /// Holds the results of one harness run at one Sensitivity.
  /// </summary>
  /// <param name="Sensitivity">The Sensitivity of the run.</param>
  /// <param name="Streams">The counts of each stream.</param>
  /// <param name="LatencyP50">The median time of one Decision.</param>
  /// <param name="LatencyP99">The 99th percentile time of one Decision.</param>
  /// <param name="Worst">
  /// The most frequent false corrections and misses, most frequent first.
  /// </param>
  internal sealed record AccuracyReport(
      Sensitivity Sensitivity,
      IReadOnlyList<StreamResult> Streams,
      TimeSpan LatencyP50,
      TimeSpan LatencyP99,
      IReadOnlyList<string> Worst) {
    /// <summary>
    /// Gets the number of correctly typed Words.
    /// </summary>
    public int Negatives => Streams.Where(s => !s.IsPositive).Sum(s => s.Words);

    /// <summary>
    /// Gets the number of correctly typed Words that were corrected.
    /// </summary>
    public int FalseCorrections => Streams.Where(s => !s.IsPositive).Sum(s => s.Hits);

    /// <summary>
    /// Gets the number of wrong-layout Words.
    /// </summary>
    public int Positives => Streams.Where(s => s.IsPositive).Sum(s => s.Words);

    /// <summary>
    /// Gets the number of wrong-layout Words corrected to the right Layout.
    /// </summary>
    public int Corrected => Streams.Where(s => s.IsPositive).Sum(s => s.Hits);

    /// <summary>
    /// Gets the share of correctly typed Words that were corrected.
    /// </summary>
    public double FalseCorrectionRate => Rate(FalseCorrections, Negatives);

    /// <summary>
    /// Gets the share of wrong-layout Words corrected to the right Layout.
    /// </summary>
    public double CorrectionRate => Rate(Corrected, Positives);

    private static double Rate(int hits, int words) {
      return words == 0 ? 0 : (double)hits / words;
    }
  }
}
