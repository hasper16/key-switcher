using Rekey.Core.Languages;
using Rekey.Core.Words;
using Xunit;

namespace Rekey.Core.Tests.Words {
  // Spec word-learning: lists persist across restarts; design D1: atomic write, and a file
  // Rekey cannot read is kept aside.
  public sealed class WordListsStoreTests : IDisposable {
    private static readonly DateTimeOffset _monday = new(2026, 11, 16, 9, 0, 0, TimeSpan.Zero);
    private static readonly WordListsDocument _lists = new(
        [new ProtectedWord("ntcn", WordSource.Auto, _monday)],
        [new LearnedWord("кринж", Language.Russian, WordSource.Auto, _monday)]);

    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("rekey-words-");
    private readonly string _folder;

    public WordListsStoreTests() {
      _folder = Path.Combine(_root.FullName, "Rekey");
    }

    public void Dispose() {
      _root.Delete(recursive: true);
    }

    [Fact]
    public void ListsSurviveARestart() {
      new WordListsStore(_folder).Save(_lists);

      WordListsDocument reloaded = new WordListsStore(_folder).Load();

      Assert.Equal(_lists.Protected, reloaded.Protected);
      Assert.Equal(_lists.Learned, reloaded.Learned);
    }

    [Fact]
    public void FileIsNamedWordsJson() {
      var store = new WordListsStore(_folder);

      Assert.Equal(Path.Combine(_folder, "words.json"), store.FilePath);
    }

    [Fact]
    public void MissingFileGivesEmptyLists() {
      WordListsDocument lists = new WordListsStore(_folder).Load();

      Assert.Empty(lists.Protected);
      Assert.Empty(lists.Learned);
    }

    [Fact]
    public void SaveReplacesTheFileAndLeavesNoTemporaryFile() {
      var store = new WordListsStore(_folder);
      store.Save(WordListsDocument.Empty);

      store.Save(_lists);

      Assert.Equal(["words.json"], Directory.GetFiles(_folder).Select(Path.GetFileName));
      Assert.Equal(_lists.Protected, store.Load().Protected);
    }

    [Theory]
    [InlineData("{ \"version\": 1, \"protected\": [")]
    [InlineData("null")]
    [InlineData("{ \"version\": 2, \"protected\": [], \"learned\": [] }")]
    [InlineData("{ \"version\": 1, \"protected\": [{ \"text\": \"a\", \"source\": \"x\" }] }")]
    public void CorruptFileGivesEmptyListsAndKeepsItAsBackup(string corrupt) {
      var store = new WordListsStore(_folder);
      Directory.CreateDirectory(_folder);
      File.WriteAllText(store.FilePath, corrupt);

      WordListsDocument lists = store.Load();

      Assert.Empty(lists.Protected);
      Assert.Empty(lists.Learned);
      Assert.Equal(corrupt, File.ReadAllText(store.FilePath + ".bak"));
      Assert.False(File.Exists(store.FilePath));
    }
  }
}
