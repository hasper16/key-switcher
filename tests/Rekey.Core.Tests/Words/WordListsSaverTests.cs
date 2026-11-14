using Rekey.Core.Words;
using Xunit;

namespace Rekey.Core.Tests.Words {
  // Design D1 of add-word-learning: the lists are saved 2 s after the last change, and at once
  // when the app exits.
  public sealed class WordListsSaverTests : IDisposable {
    private static readonly DateTimeOffset _monday = new(2026, 11, 16, 9, 0, 0, TimeSpan.Zero);

    private readonly DirectoryInfo _folder = Directory.CreateTempSubdirectory("rekey-saver-");
    private readonly ManualClock _clock = new();
    private readonly WordListsStore _store;
    private readonly WordListsSaver _saver;

    public WordListsSaverTests() {
      _store = new WordListsStore(_folder.FullName);
      _saver = new WordListsSaver(_store, _clock, _ => { });
    }

    public void Dispose() {
      _saver.Dispose();
      _folder.Delete(recursive: true);
    }

    [Fact]
    public void ChangeIsSavedTwoSecondsLater() {
      _saver.Changed(Protecting("ntcn"));

      _clock.Advance(TimeSpan.FromSeconds(1.9));
      Assert.False(File.Exists(_store.FilePath));

      _clock.Advance(TimeSpan.FromSeconds(0.1));
      Assert.Equal(["ntcn"], SavedWords());
    }

    [Fact]
    public void LaterChangeRestartsTheWait() {
      _saver.Changed(Protecting("ntcn"));
      _clock.Advance(TimeSpan.FromSeconds(1.5));
      _saver.Changed(Protecting("ntcn", "zorbit"));

      _clock.Advance(TimeSpan.FromSeconds(1.9));
      Assert.False(File.Exists(_store.FilePath));

      _clock.Advance(TimeSpan.FromSeconds(0.1));
      Assert.Equal(["ntcn", "zorbit"], SavedWords());
    }

    [Fact]
    public void FlushSavesAtOnce() {
      _saver.Changed(Protecting("ntcn"));

      _saver.Flush();

      Assert.Equal(["ntcn"], SavedWords());
    }

    [Fact]
    public void FlushWithoutChangesWritesNothing() {
      _saver.Flush();

      Assert.False(File.Exists(_store.FilePath));
    }

    [Fact]
    public void FailedSaveIsReportedAndRetriedOnTheNextChange() {
      List<Exception> failures = [];
      using var saver = new WordListsSaver(_store, _clock, failures.Add);
      Directory.CreateDirectory(_folder.FullName);
      using (File.Open(_store.FilePath, FileMode.Create, FileAccess.Write, FileShare.None)) {
        saver.Changed(Protecting("ntcn"));
        _clock.Advance(TimeSpan.FromSeconds(2));
      }

      saver.Changed(Protecting("zorbit"));
      _clock.Advance(TimeSpan.FromSeconds(2));

      Assert.IsAssignableFrom<IOException>(Assert.Single(failures));
      Assert.Equal(["zorbit"], SavedWords());
    }

    private static WordListsDocument Protecting(params string[] words) {
      return new WordListsDocument(
          [.. words.Select(word => new ProtectedWord(word, WordSource.Auto, _monday))], []);
    }

    private string[] SavedWords() {
      return [.. _store.Load()!.Protected.Select(word => word.Text)];
    }
  }
}
