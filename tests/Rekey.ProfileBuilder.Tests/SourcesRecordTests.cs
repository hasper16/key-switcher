using Xunit;

namespace Rekey.ProfileBuilder.Tests {
  public sealed class SourcesRecordTests : IDisposable {
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("rekey-sources-");

    public void Dispose() {
      _root.Delete(recursive: true);
    }

    [Fact]
    public void ReportsFilesTheRecordDoesNotList() {
      WriteFile("xx/list.txt");
      WriteFile("xx/short-words.txt");
      WriteFile("yy/list.txt");
      WriteFile(SourcesRecord.FileName, "| `xx/list.txt` | CC BY-SA 4.0 |\n| `yy/list.txt` |");

      Assert.Equal(["xx/short-words.txt"], SourcesRecord.Unlisted(_root.FullName));
    }

    [Fact]
    public void MissingRecordReportsEveryFile() {
      WriteFile("xx/list.txt");

      Assert.Equal(["xx/list.txt"], SourcesRecord.Unlisted(_root.FullName));
    }

    private void WriteFile(string relativePath, string text = "") {
      string path = Path.Combine(_root.FullName, relativePath);
      Directory.CreateDirectory(Path.GetDirectoryName(path)!);
      File.WriteAllText(path, text);
    }
  }
}
