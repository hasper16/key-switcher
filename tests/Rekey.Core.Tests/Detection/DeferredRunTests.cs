using Rekey.Core.Detection;
using Rekey.Core.Languages;
using Xunit;
using static Rekey.Core.Tests.Detection.DetectionWorld;

namespace Rekey.Core.Tests.Detection {
  // "yt pyf." comes from the spec; the other texts are invented.
  public sealed class DeferredRunTests {
    private static readonly LayoutId _russian = KeyMapFixtures.Russian.Layout;
    private static readonly LayoutId _ukrainian = KeyMapFixtures.Ukrainian.Layout;

    [Fact]
    public void ConfirmedRunIsCorrectedTogether() {
      var run = new DeferredRun();

      Correction? first = run.Add(Decide(KeyMapFixtures.EnglishUs, "yt"));
      Correction? second = run.Add(Decide(KeyMapFixtures.EnglishUs, "pyf."));

      Assert.Null(first);
      Assert.NotNull(second);
      Assert.Equal(_russian, second.Target);
      Assert.Equal("не знаю", string.Join(" ", second.Texts));
    }

    [Fact]
    public void KeptNextWordDropsTheRun() {
      var run = new DeferredRun();
      run.Add(Decision.Defer(_russian, "ыф"));

      Assert.Null(run.Add(Decision.Keep));
      Assert.Equal(["вапро"], run.Add(Decision.Correct(_russian, "вапро"))?.Texts);
    }

    [Fact]
    public void CorrectionToAnotherLayoutDropsTheRun() {
      var run = new DeferredRun();
      run.Add(Decision.Defer(_russian, "ыф"));

      Correction? correction = run.Add(Decision.Correct(_ukrainian, "вапро"));

      Assert.Equal(new Correction(_ukrainian, ["вапро"]), correction);
    }

    [Fact]
    public void DeferralTowardsAnotherLayoutStartsANewRun() {
      var run = new DeferredRun();
      run.Add(Decision.Defer(_russian, "ыф"));
      run.Add(Decision.Defer(_ukrainian, "ів"));

      Assert.Equal(["ів", "вапро"], run.Add(Decision.Correct(_ukrainian, "вапро"))?.Texts);
    }

    [Fact]
    public void FourthDeferralDropsTheOldest() {
      var run = new DeferredRun();
      foreach (string text in new[] { "ыа", "ыб", "ыв", "ыг" }) {
        Assert.Null(run.Add(Decision.Defer(_russian, text)));
      }

      Correction? correction = run.Add(Decision.Correct(_russian, "вапро"));

      Assert.Equal(["ыб", "ыв", "ыг", "вапро"], correction?.Texts);
    }

    [Fact]
    public void ResetRunIsNotCorrected() {
      var run = new DeferredRun();
      run.Add(Decision.Defer(_russian, "ыф"));

      run.Reset();

      Assert.Equal(
          new Correction(_russian, ["вапро"]), run.Add(Decision.Correct(_russian, "вапро")));
    }

    [Fact]
    public void WordCorrectedWithoutARunIsCorrectedAlone() {
      Assert.Equal(
          new Correction(_russian, ["вапро"]),
          new DeferredRun().Add(Decision.Correct(_russian, "вапро")));
    }
  }
}
