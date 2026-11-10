using Xunit;

namespace Rekey.Core.Tests {
  // Design D7: the time of the last restart after a freeze survives the restart.
  public sealed class RestartStateStoreTests : IDisposable {
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("rekey-restart-");

    public void Dispose() {
      _root.Delete(recursive: true);
    }

    [Fact]
    public void NoFileMeansNoRestart() {
      Assert.Null(new RestartStateStore(_root.FullName).LoadLastRestart());
    }

    [Fact]
    public void SavedRestartTimeSurvivesReload() {
      var time = new DateTimeOffset(2026, 11, 9, 12, 30, 15, TimeSpan.Zero);
      new RestartStateStore(_root.FullName).SaveLastRestart(time);

      Assert.Equal(time, new RestartStateStore(_root.FullName).LoadLastRestart());
    }

    [Fact]
    public void UnreadableFileMeansNoRestart() {
      var store = new RestartStateStore(_root.FullName);
      File.WriteAllText(store.FilePath, "not a time");

      Assert.Null(store.LoadLastRestart());
    }

    [Fact]
    public void SaveCreatesTheFolder() {
      string folder = Path.Combine(_root.FullName, "Rekey");

      new RestartStateStore(folder).SaveLastRestart(DateTimeOffset.UnixEpoch);

      Assert.True(File.Exists(Path.Combine(folder, "restart-state.txt")));
    }
  }
}
