using Rekey.Win32.Focus;
using Xunit;

namespace Rekey.Win32.Tests.Focus {
  public sealed class ForegroundAppsTests {
    [Theory]
    [InlineData("Shell_TrayWnd")]
    [InlineData("Shell_SecondaryTrayWnd")]
    [InlineData("NotifyIconOverflowWindow")]
    [InlineData("TopLevelWindowForOverflowXamlIsland")]
    [InlineData("Xaml_WindowedPopupClass")]
    public void TaskbarSurfacesAreNotTheAppTheUserTypesIn(string className) {
      Assert.False(ForegroundApps.CountsAsApp(className));
    }

    [Theory]
    [InlineData("Notepad")]
    [InlineData("Chrome_WidgetWin_1")]
    [InlineData("CabinetWClass")]
    public void OrdinaryWindowsAreApps(string className) {
      Assert.True(ForegroundApps.CountsAsApp(className));
    }
  }
}
