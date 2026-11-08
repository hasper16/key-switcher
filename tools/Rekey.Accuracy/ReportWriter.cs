namespace Rekey.Accuracy {
  /// <summary>
  /// Prints an accuracy report as plain text.
  /// </summary>
  internal static class ReportWriter {
    /// <summary>
    /// Writes a report.
    /// </summary>
    /// <param name="report">The report to write.</param>
    /// <remarks>
    /// Numbers follow the current culture; the tool sets the invariant culture.
    /// </remarks>
    /// <param name="output">The writer to write to.</param>
    public static void Write(AccuracyReport report, TextWriter output) {
      output.WriteLine($"## {report.Sensitivity}");
      output.WriteLine();
      output.WriteLine(
          $"False corrections: {report.FalseCorrections} of {report.Negatives} correctly typed "
          + $"Words ({report.FalseCorrectionRate:P2})");
      output.WriteLine(
          $"Corrections: {report.Corrected} of {report.Positives} wrong-layout Words "
          + $"({report.CorrectionRate:P2})");
      output.WriteLine(
          $"Decision latency: p50 {report.LatencyP50.TotalMilliseconds:F3} ms, "
          + $"p99 {report.LatencyP99.TotalMilliseconds:F3} ms");
      output.WriteLine();
      output.WriteLine("| Stream | Words | Corrected | Share |");
      output.WriteLine("|---|---:|---:|---:|");
      foreach (StreamResult stream in report.Streams) {
        double share = stream.Words == 0 ? 0 : (double)stream.Hits / stream.Words;
        output.WriteLine(
            $"| {stream.Name} | {stream.Words} | {stream.Hits} | {share:P2} |");
      }

      output.WriteLine();
      output.WriteLine("Most frequent problems:");
      output.WriteLine();
      output.WriteLine("```text");
      foreach (string line in report.Worst) {
        output.WriteLine(line);
      }

      output.WriteLine("```");
      output.WriteLine();
    }
  }
}
