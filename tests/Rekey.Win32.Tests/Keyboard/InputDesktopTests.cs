using Rekey.Win32.Keyboard;
using Xunit;

namespace Rekey.Win32.Tests.Keyboard {
  // Spec keyboard-capture, scenario "Secure desktop": input there never reaches the hooks.
  public sealed class InputDesktopTests {
    [Theory]
    [InlineData("Default")]
    [InlineData("default")]
    public void UsersDesktopIsVisibleToTheHooks(string name) {
      Assert.False(InputDesktop.HidesInputFromHooks(name));
    }

    [Theory]
    [InlineData("Winlogon")]
    [InlineData("Screen-saver")]
    [InlineData(null)]
    public void SecureOrUnreadableDesktopHidesInput(string? name) {
      Assert.True(InputDesktop.HidesInputFromHooks(name));
    }
  }
}
