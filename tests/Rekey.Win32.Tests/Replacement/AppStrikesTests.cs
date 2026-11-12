using Rekey.Win32.Replacement;
using Xunit;

namespace Rekey.Win32.Tests.Replacement {
  // Design D3: an app that failed twice is not checked again this session.
  public sealed class AppStrikesTests {
    private const string _editor = @"C:\Apps\Editor.exe";
    private readonly AppStrikes _apps = new();

    [Fact]
    public void OneStrikeLeavesTheAppChecked() {
      _apps.Record(_editor);

      Assert.False(_apps.HasStruckOut(_editor));
    }

    [Fact]
    public void TwoStrikesMarkTheAppOut() {
      _apps.Record(_editor);
      _apps.Record(_editor);

      Assert.True(_apps.HasStruckOut(_editor));
    }

    [Fact]
    public void AppPathsDifferingInCaseAreTheSameApp() {
      _apps.Record(_editor);
      _apps.Record(_editor.ToUpperInvariant());

      Assert.True(_apps.HasStruckOut(_editor));
    }

    [Fact]
    public void OtherAppsStayChecked() {
      _apps.Record(_editor);
      _apps.Record(_editor);

      Assert.False(_apps.HasStruckOut(@"C:\Apps\Browser.exe"));
    }
  }
}
