using Rekey.Core;
using Rekey.Win32.Focus;
using Xunit;

namespace Rekey.Win32.Tests.Focus {
  // Design D4 of add-auto-correction: which process names the application of a window. The
  // apps are invented.
  public sealed class AppIdentityTests {
    private const uint _frameProcess = 100;
    private const uint _hostedProcess = 200;
    private const string _frameHost = @"C:\Windows\System32\ApplicationFrameHost.exe";

    private static readonly AppId _hosted = new(@"C:\Apps\Zorbit\Calc.exe", "Zorbit.Calc_8abc!App");

    [Fact]
    public void OrdinaryAppIsItsOwnProcess() {
      var editor = new AppId(@"C:\Apps\Zorbit\Editor.exe");

      AppId app = AppIdentity.Resolve(_frameProcess, _ => editor, () => _hostedProcess);

      Assert.Equal(editor, app);
    }

    [Fact]
    public void PackagedAppWithItsOwnWindowKeepsItsAumid() {
      AppId app = AppIdentity.Resolve(_hostedProcess, _ => _hosted, () => null);

      Assert.Equal(_hosted, app);
    }

    [Theory]
    [InlineData(_frameHost)]
    [InlineData(@"c:\windows\system32\APPLICATIONFRAMEHOST.EXE")]
    public void FrameHostWindowIsTheAppItHosts(string frameHostPath) {
      AppId app = AppIdentity.Resolve(_frameProcess, Read(frameHostPath), () => _hostedProcess);

      Assert.Equal(_hosted, app);
    }

    [Fact]
    public void FrameHostWithoutAHostedAppIsItself() {
      AppId app = AppIdentity.Resolve(_frameProcess, Read(_frameHost), () => null);

      Assert.Equal(new AppId(_frameHost), app);
    }

    [Fact]
    public void FrameHostWhoseCoreWindowIsItsOwnIsItself() {
      AppId app = AppIdentity.Resolve(_frameProcess, Read(_frameHost), () => _frameProcess);

      Assert.Equal(new AppId(_frameHost), app);
    }

    [Fact]
    public void FrameHostWhoseHostedAppCannotBeReadIsItself() {
      AppId app = AppIdentity.Resolve(
          _frameProcess,
          process => process == _hostedProcess ? new AppId(string.Empty) : new AppId(_frameHost),
          () => _hostedProcess);

      Assert.Equal(new AppId(_frameHost), app);
    }

    [Fact]
    public void OrdinaryAppNeverAsksForAHostedProcess() {
      var editor = new AppId(@"C:\Apps\Zorbit\Editor.exe");
      int asked = 0;

      AppIdentity.Resolve(_frameProcess, _ => editor, () => {
        asked++;
        return _hostedProcess;
      });

      Assert.Equal(0, asked);
    }

    private static Func<uint, AppId> Read(string frameHostPath) {
      return process => process == _hostedProcess ? _hosted : new AppId(frameHostPath);
    }
  }
}
