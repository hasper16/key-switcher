using Rekey.Win32.Replacement;
using Xunit;

namespace Rekey.Win32.Tests.Replacement {
  // Design D3: an app that ran out of time twice is not asked again this session.
  public sealed class SlowAppsTests {
    private const string _editor = @"C:\Apps\Editor.exe";
    private readonly SlowApps _apps = new();

    [Fact]
    public void OneTimeoutLeavesTheAppVerified() {
      _apps.RecordTimeout(_editor);

      Assert.False(_apps.IsSlow(_editor));
    }

    [Fact]
    public void TwoTimeoutsMarkTheAppSlow() {
      _apps.RecordTimeout(_editor);
      _apps.RecordTimeout(_editor);

      Assert.True(_apps.IsSlow(_editor));
    }

    [Fact]
    public void AppPathsDifferingInCaseAreTheSameApp() {
      _apps.RecordTimeout(_editor);
      _apps.RecordTimeout(_editor.ToUpperInvariant());

      Assert.True(_apps.IsSlow(_editor));
    }

    [Fact]
    public void OtherAppsStayVerified() {
      _apps.RecordTimeout(_editor);
      _apps.RecordTimeout(_editor);

      Assert.False(_apps.IsSlow(@"C:\Apps\Browser.exe"));
    }
  }
}
