using Rekey.DesktopChecks;
using Xunit;

namespace Rekey.DesktopChecks.Tests {
  // Output carries counts, kinds and codes only: an exception's message is shown only when the
  // tool wrote it itself, as recovery steps.
  public sealed class ReportTests {
    [Fact]
    public void RecoveryStepsAreShown() {
      var failure = new RecoveryException("Compare the saved list in Settings.");

      Assert.Equal("Compare the saved list in Settings.", Report.Describe(failure));
    }

    [Fact]
    public void OtherExceptionsShowOnlyTheirType() {
      var failure = new InvalidOperationException("text from a system API");

      Assert.Equal("InvalidOperationException", Report.Describe(failure));
    }
  }
}
