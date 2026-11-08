using Rekey.Core.Languages;
using Xunit;

namespace Rekey.ProfileBuilder.Tests {
  public sealed class BuildCommandTests : IDisposable {
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("rekey-build-");
    private readonly string _sources;
    private readonly string _output;

    public BuildCommandTests() {
      _sources = Path.Join(_root.FullName, "sources");
      _output = Path.Join(_root.FullName, "profiles");
      WriteSource("en/language.json", """
          {
            "code": "en", "displayName": "English", "alphabet": "abcdefghijklmnopqrstuvwxyz",
            "layoutIds": ["00000409"], "frequencyList": "list.txt"
          }
          """);
      WriteSource("en/list.txt", "zorbit 9\nblim 5\n");
      WriteSource("en/short-words.txt", "q\nzo\n");
    }

    public void Dispose() {
      _root.Delete(recursive: true);
    }

    [Fact]
    public void WritesALoadableProfilePerLanguage() {
      WriteRecord("`en/language.json` `en/list.txt` `en/short-words.txt`");

      int exitCode = BuildCommand.Run(_sources, _output, TextWriter.Null, TextWriter.Null);

      Assert.Equal(0, exitCode);
      using FileStream stream = File.OpenRead(Path.Join(_output, "en.json"));
      LanguageProfile profile = LanguageProfileLoader.Load(stream);
      Assert.True(profile.ContainsCommonWord("Blim"));
      Assert.True(profile.ContainsShortWord("ZO"));
    }

    [Fact]
    public void UnlistedSourceStopsWithoutWritingProfiles() {
      WriteRecord("`en/language.json` `en/list.txt`");
      using var error = new StringWriter();

      int exitCode = BuildCommand.Run(_sources, _output, TextWriter.Null, error);

      Assert.NotEqual(0, exitCode);
      Assert.Contains("en/short-words.txt", error.ToString(), StringComparison.Ordinal);
      Assert.False(Directory.Exists(_output));
    }

    private void WriteRecord(string text) {
      WriteSource(SourcesRecord.FileName, text);
    }

    private void WriteSource(string relativePath, string text) {
      string path = Path.Join(_sources, relativePath);
      Directory.CreateDirectory(Path.GetDirectoryName(path)!);
      File.WriteAllText(path, text);
    }
  }
}
