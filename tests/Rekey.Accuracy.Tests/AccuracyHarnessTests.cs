using Rekey.Core.Detection;
using Rekey.Core.Languages;
using Xunit;

namespace Rekey.Accuracy.Tests {
  // Spec examples of wrong-layout-detection; real words come from the spec, not from users.
  public sealed class AccuracyHarnessTests {
    private static readonly AccuracyHarness _harness = AccuracyHarness.Create(
        Path.Join(AppContext.BaseDirectory, "fixtures", "keymaps"));

    [Fact]
    public void CountsCorrectedWrongLayoutWords() {
      AccuracyReport report = _harness.Run(
          [new Passage(Language.Russian, "привет")], [], Sensitivity.Balanced);

      // Typed in Ukrainian (Enhanced) the keys read the same, so only English (US) counts.
      Assert.Equal(1, report.Positives);
      Assert.Equal(1, report.Corrected);
    }

    [Fact]
    public void CountsCorrectlyTypedWordsThatGetCorrected() {
      AccuracyReport report = _harness.Run(
          [new Passage(Language.English, "hello ghbdtn")], [], Sensitivity.Balanced);

      Assert.Equal(2, report.Negatives);
      Assert.Equal(1, report.FalseCorrections);
      Assert.Contains(report.Worst, line => line.Contains("ghbdtn", StringComparison.Ordinal));
    }

    [Fact]
    public void TypesLatinWordsOfTechnicalTextInEnglish() {
      AccuracyReport report = _harness.Run(
          [], [new Passage(Language.Russian, "привет hello")], Sensitivity.Balanced);

      Assert.Equal(2, report.Negatives);
      Assert.Equal(0, report.FalseCorrections);
    }

    [Fact]
    public void LayoutSwitchDropsTheDeferredWord() {
      // "yt" is typed in English (US) and deferred; switching to Russian for "привыт" (spec
      // example) drops it, so only "привыт" is corrected.
      AccuracyReport report = _harness.Run(
          [], [new Passage(Language.Russian, "yt привыт")], Sensitivity.Balanced);

      Assert.Equal(2, report.Negatives);
      Assert.Equal(1, report.FalseCorrections);
    }

    [Fact]
    public void ConfirmedDeferredWordCountsAsCorrected() {
      AccuracyReport report = _harness.Run(
          [new Passage(Language.Russian, "не знаю")], [], Sensitivity.Balanced);

      Assert.Equal(2, report.Positives);
      Assert.Equal(2, report.Corrected);
    }

    [Fact]
    public void MeasuresDecisionLatency() {
      AccuracyReport report = _harness.Run(
          [new Passage(Language.English, "hello")], [], Sensitivity.Balanced);

      Assert.True(report.LatencyP99 >= report.LatencyP50);
      Assert.True(report.LatencyP50 > TimeSpan.Zero);
    }
  }
}
