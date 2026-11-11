using Rekey.Win32.Replacement;
using Xunit;

namespace Rekey.Win32.Tests.Replacement {
  // A console's UI Automation selection is the terminal's own, which typing does not replace.
  public sealed class ConsoleWindowsTests {
    [Theory]
    [InlineData("ConsoleWindowClass")]
    [InlineData("CASCADIA_HOSTING_WINDOW_CLASS")]
    [InlineData("PseudoConsoleWindow")]
    public void ConsoleHostWindowsAreConsoles(string className) {
      Assert.True(ConsoleWindows.IsConsole(className));
    }

    [Theory]
    [InlineData("Notepad")]
    [InlineData("Chrome_WidgetWin_1")]
    [InlineData("")]
    public void OtherWindowsAreNot(string className) {
      Assert.False(ConsoleWindows.IsConsole(className));
    }
  }
}
