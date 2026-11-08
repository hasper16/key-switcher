using Xunit;

namespace Rekey.Core.Tests {
  public sealed class SettingsStoreTests : IDisposable {
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("rekey-settings-");
    private readonly string _folder;

    public SettingsStoreTests() {
      // A fresh user has no Rekey folder yet, so the store must create it.
      _folder = Path.Combine(_root.FullName, "Rekey");
    }

    public void Dispose() {
      _root.Delete(recursive: true);
    }

    [Fact]
    public void MissingFileGivesDefaultsAndCreatesTheFile() {
      var store = new SettingsStore(_folder);

      Settings settings = store.Load();

      Assert.Equal(new Settings(), settings);
      Assert.True(File.Exists(store.FilePath));
    }

    [Fact]
    public void ChangedValueSurvivesReload() {
      new SettingsStore(_folder).Save(new Settings { AutoCorrectionEnabled = false });

      Settings reloaded = new SettingsStore(_folder).Load();

      Assert.False(reloaded.AutoCorrectionEnabled);
    }

    [Theory]
    [InlineData("{ \"autoCorrectionEnabled\": fals")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{ \"autoCorrectionEnabled\": \"maybe\" }")]
    public void CorruptFileGivesDefaultsAndKeepsItAsBackup(string corruptJson) {
      var store = new SettingsStore(_folder);
      Directory.CreateDirectory(_folder);
      File.WriteAllText(store.FilePath, corruptJson);

      Settings settings = store.Load();

      Assert.Equal(new Settings(), settings);
      Assert.Equal(corruptJson, File.ReadAllText(store.FilePath + ".bak"));
    }

    [Fact]
    public void CorruptFileIsKeptWhenTheUsualBackupIsLocked() {
      var store = new SettingsStore(_folder);
      Directory.CreateDirectory(_folder);
      File.WriteAllText(store.FilePath, "{ broken");
      string lockedBackup = store.FilePath + ".bak";
      File.WriteAllText(lockedBackup, "older backup");

      Settings settings;
      using (File.Open(lockedBackup, FileMode.Open, FileAccess.Read, FileShare.None)) {
        settings = store.Load();
      }

      Assert.Equal(new Settings(), settings);
      Assert.Equal("older backup", File.ReadAllText(lockedBackup));
      string kept = Assert.Single(Directory.GetFiles(_folder, "settings.json.*.bak"));
      Assert.Equal("{ broken", File.ReadAllText(kept));
    }

    [Fact]
    public void UnknownFieldsAreIgnored() {
      Settings settings =
          LoadFrom("{ \"autoCorrectionEnabled\": false, \"futureField\": [1, 2] }");

      Assert.False(settings.AutoCorrectionEnabled);
      Assert.False(File.Exists(Path.Combine(_folder, "settings.json.bak")));
    }

    [Fact]
    public void MissingFieldsGetDefaults() {
      Settings settings = LoadFrom("{ \"version\": 1 }");

      Assert.True(settings.AutoCorrectionEnabled);
      Assert.False(File.Exists(Path.Combine(_folder, "settings.json.bak")));
    }

    private Settings LoadFrom(string json) {
      var store = new SettingsStore(_folder);
      Directory.CreateDirectory(_folder);
      File.WriteAllText(store.FilePath, json);
      return store.Load();
    }
  }
}
