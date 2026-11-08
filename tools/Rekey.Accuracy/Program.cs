using System.Globalization;
using Rekey.Core.Detection;
using Rekey.Core.Languages;

namespace Rekey.Accuracy {
  /// <summary>
  /// Measures false and missed corrections on the held-out corpus (design D7 of
  /// add-language-scorer).
  /// </summary>
  internal static class Program {
    private const int _usageError = 2;
    private const int _limitsBroken = 1;

    // Spec: at most 0.5% false corrections and at least 95% corrections under Balanced.
    private const double _maxFalseCorrectionRate = 0.005;
    private const double _minCorrectionRate = 0.95;

    private static int Main(string[] args) {
      CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
      if (args.Length < 2) {
        Console.Error.WriteLine(
            "Usage: Rekey.Accuracy <corpus folder> <key map folder> [--sample <n>] [--check]");
        return _usageError;
      }

      int sample = int.MaxValue;
      int sampleAt = Array.IndexOf(args, "--sample");
      if (sampleAt >= 0
          && (sampleAt + 1 >= args.Length
              || !int.TryParse(args[sampleAt + 1], CultureInfo.InvariantCulture, out sample))) {
        Console.Error.WriteLine("--sample needs a number of sentences per Language.");
        return _usageError;
      }

      IReadOnlyList<Passage> natural = Corpus.ReadNatural(args[0], sample);
      IReadOnlyList<Passage> technical = Corpus.ReadTechnical(args[0]);
      AccuracyHarness harness = AccuracyHarness.Create(args[1]);
      AccuracyReport? balanced = null;
      foreach (Sensitivity sensitivity in Enum.GetValues<Sensitivity>()) {
        AccuracyReport report = harness.Run(natural, technical, sensitivity);
        ReportWriter.Write(report, Console.Out);
        balanced = sensitivity == Sensitivity.Balanced ? report : balanced;
      }

      bool withinLimits = balanced!.FalseCorrectionRate <= _maxFalseCorrectionRate
          && balanced.CorrectionRate >= _minCorrectionRate;
      Console.WriteLine(withinLimits
          ? "Balanced is within the spec limits."
          : "Balanced breaks the spec limits.");
      return args.Contains("--check") && !withinLimits ? _limitsBroken : 0;
    }
  }
}
