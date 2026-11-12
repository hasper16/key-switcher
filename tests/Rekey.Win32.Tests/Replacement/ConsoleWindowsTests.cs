using Rekey.Win32.Replacement;
using Xunit;

namespace Rekey.Win32.Tests.Replacement {
  // What kind of target the foreground window is for a replacement (design D3).
  public sealed class ConsoleWindowsTests {
    [Fact]
    public void ClassicConsoleTakesTypedTextButNoSelection() {
      // The desktop probe showed conhost taking Backspaces and Unicode characters.
      Assert.Equal(TextTarget.Console, ConsoleWindows.TargetOf("ConsoleWindowClass"));
    }

    [Theory]
    [InlineData("CASCADIA_HOSTING_WINDOW_CLASS")]
    [InlineData("PseudoConsoleWindow")]
    public void ConsolesNotYetProvenAreLeftAlone(string className) {
      Assert.Equal(TextTarget.Unsupported, ConsoleWindows.TargetOf(className));
    }

    [Theory]
    [InlineData("Notepad")]
    [InlineData("Chrome_WidgetWin_1")]
    [InlineData("")]
    public void OtherWindowsAreEditable(string className) {
      Assert.Equal(TextTarget.Editable, ConsoleWindows.TargetOf(className));
    }
  }
}
