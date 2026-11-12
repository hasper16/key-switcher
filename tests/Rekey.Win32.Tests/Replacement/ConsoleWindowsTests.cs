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

    [Fact]
    public void WindowsTerminalTakesTypedTextButNoSelection() {
      // The desktop probe, with the buffer read proven, showed Windows Terminal taking them too.
      Assert.Equal(
          TextTarget.Console, ConsoleWindows.TargetOf("CASCADIA_HOSTING_WINDOW_CLASS"));
    }

    [Fact]
    public void PseudoConsoleNotYetProvenIsLeftAlone() {
      Assert.Equal(TextTarget.Unsupported, ConsoleWindows.TargetOf("PseudoConsoleWindow"));
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
