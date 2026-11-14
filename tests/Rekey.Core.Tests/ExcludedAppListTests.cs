using Xunit;

namespace Rekey.Core.Tests {
  // Spec auto-correction: "Excluded Apps are identified by application" (design D4). The apps
  // are invented.
  public sealed class ExcludedAppListTests {
    private const string _frameHost = @"C:\Windows\System32\ApplicationFrameHost.exe";
    private const string _aumid = "Zorbit.Calc_8abc!App";

    private static readonly AppId _editor = new(@"C:\Apps\Zorbit\Editor.exe");

    [Theory]
    [InlineData(@"C:\Apps\Zorbit\Editor.exe")]
    [InlineData(@"c:\apps\zorbit\EDITOR.EXE")]
    [InlineData("C:/Apps/Zorbit/Editor.exe")]
    [InlineData("\"C:\\Apps\\Zorbit\\Editor.exe\"")]
    [InlineData("  C:\\Apps\\Zorbit\\Editor.exe ")]
    [InlineData(@"\\?\C:\Apps\Zorbit\Editor.exe")]
    [InlineData(@"C:\Apps\Other\..\Zorbit\.\Editor.exe")]
    [InlineData(@"C:\Apps\\Zorbit\Editor.exe")]
    public void PathMatchesWhateverItsCaseAndForm(string entry) {
      Assert.True(new ExcludedAppList([entry]).Contains(_editor));
    }

    [Theory]
    [InlineData(@"C:\Apps\Zorbit\Editor2.exe")]
    [InlineData(@"D:\Apps\Zorbit\Editor.exe")]
    [InlineData("Editor.exe")]
    [InlineData("")]
    public void OtherPathDoesNotMatch(string entry) {
      Assert.False(new ExcludedAppList([entry]).Contains(_editor));
    }

    [Fact]
    public void FocusedPathIsNormalisedToo() {
      var list = new ExcludedAppList([@"C:\Apps\Zorbit\Editor.exe"]);

      Assert.True(list.Contains(new AppId(@"\\?\c:\APPS\Zorbit\Editor.exe")));
    }

    [Theory]
    [InlineData(@"\\?\UNC\zorbit-server\apps\Editor.exe", @"\\zorbit-server\apps\Editor.exe")]
    [InlineData(@"\\zorbit-server\apps\Editor.exe", @"\\?\UNC\zorbit-server\apps\Editor.exe")]
    [InlineData(@"\\?\unc\zorbit-server\apps\Editor.exe", @"\\ZORBIT-SERVER\apps\editor.exe")]
    public void NetworkPathMatchesInItsExtendedAndOrdinaryForm(string entry, string focused) {
      Assert.True(new ExcludedAppList([entry]).Contains(new AppId(focused)));
    }

    [Fact]
    public void PackagedAppMatchesByItsAumidWithoutCase() {
      var list = new ExcludedAppList(["zorbit.calc_8ABC!app"]);

      Assert.True(list.Contains(new AppId(@"C:\Program Files\WindowsApps\Calc\Calc.exe", _aumid)));
    }

    [Fact]
    public void AumidWinsOverTheFrameHostPath() {
      // Excluding the frame host's path must not exclude every app it hosts.
      var list = new ExcludedAppList([_frameHost]);

      Assert.False(list.Contains(new AppId(_frameHost, _aumid)));
      Assert.True(list.Contains(new AppId(_frameHost)));
    }

    [Fact]
    public void OtherAppOfTheSameFrameHostIsNotExcluded() {
      var list = new ExcludedAppList([_aumid]);

      Assert.True(list.Contains(new AppId(_frameHost, _aumid)));
      Assert.False(list.Contains(new AppId(_frameHost, "Zorbit.Notes_8abc!App")));
    }

    [Fact]
    public void DefaultListExcludesNothing() {
      Assert.False(default(ExcludedAppList).Contains(_editor));
      Assert.Empty(default(ExcludedAppList).Entries);
    }

    [Fact]
    public void ListsWithTheSameAppsAreEqual() {
      var one = new ExcludedAppList([@"C:\Apps\Zorbit\Editor.exe", _aumid]);
      var other = new ExcludedAppList([_aumid.ToUpperInvariant(), "c:/apps/zorbit/editor.exe"]);

      Assert.Equal(one, other);
      Assert.Equal(one.GetHashCode(), other.GetHashCode());
      Assert.NotEqual(one, new ExcludedAppList([_aumid]));
      Assert.Equal(default, new ExcludedAppList([]));
    }
  }
}
