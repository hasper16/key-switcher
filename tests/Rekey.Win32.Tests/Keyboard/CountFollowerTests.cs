using Rekey.Win32.Keyboard;
using Xunit;

namespace Rekey.Win32.Tests.Keyboard {
  // The worker resyncs modifiers once per batch of hook reinstalls, however many it missed.
  public sealed class CountFollowerTests {
    [Fact]
    public void NothingChangedBeforeTheFirstReinstall() {
      Assert.False(new CountFollower().Changed(0));
    }

    [Fact]
    public void ReinstallIsReportedOnce() {
      var follower = new CountFollower();

      Assert.Equal([true, false], [follower.Changed(1), follower.Changed(1)]);
    }

    [Fact]
    public void SeveralReinstallsBetweenLooksAreReportedTogether() {
      var follower = new CountFollower();
      follower.Changed(1);

      Assert.True(follower.Changed(4));
    }
  }
}
