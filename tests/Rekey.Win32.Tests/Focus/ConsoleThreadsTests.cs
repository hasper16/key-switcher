using Rekey.Win32.Focus;
using Windows.Win32.Foundation;
using Xunit;

namespace Rekey.Win32.Tests.Focus {
  // Windows reports a classic console window as owned by its client, so its Layout must be read
  // from the console host's thread that actually runs the window.
  public sealed class ConsoleThreadsTests {
    private static readonly HWND _console = new(0x1234);
    private static readonly HWND _other = new(0x5678);

    [Fact]
    public void ThreadWhoseActiveWindowIsTheConsoleIsPicked() {
      uint? picked = ConsoleThreads.Pick(_console, [10, 20, 30], ActiveOf);

      Assert.Equal(20u, picked);
    }

    [Fact]
    public void NoThreadRunningTheConsoleGivesNothing() {
      Assert.Null(ConsoleThreads.Pick(new HWND(0x9999), [10, 20, 30], ActiveOf));
    }

    [Fact]
    public void ThreadsWithoutGuiInfoAreSkipped() {
      Assert.Equal(20u, ConsoleThreads.Pick(_console, [40, 20], ActiveOf));
    }

    private static HWND? ActiveOf(uint thread) {
      return thread switch {
        10 => _other,
        20 => _console,
        30 => HWND.Null,
        _ => null,
      };
    }
  }
}
