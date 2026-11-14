namespace Rekey.Core.Words {
  /// <summary>
  /// Saves the latest snapshot of the word lists 2 seconds after the last change (design D1 of
  /// add-word-learning), so a burst of changes writes the file once.
  /// </summary>
  /// <remarks>
  /// <see cref="Changed" /> is called by one thread, the engine's worker; <see cref="Flush" />
  /// may be called from any thread.
  /// </remarks>
  public sealed class WordListsSaver : IDisposable {
    private static readonly TimeSpan _delay = TimeSpan.FromSeconds(2);

    private readonly WordListsStore _store;
    private readonly Action<Exception> _failed;
    private readonly ITimer _timer;

    // The timer's thread and a flush may save at the same moment; both write the same file.
    private readonly Lock _fileLock = new();

    // Written only by the thread that calls Changed.
    private WordListsDocument? _latest;

    /// <summary>
    /// Initializes a new instance of the <see cref="WordListsSaver" /> class.
    /// </summary>
    /// <param name="store">The store that writes the file.</param>
    /// <param name="time">The clock whose timer delays the save.</param>
    /// <param name="failed">
    /// Receives an <see cref="IOException" /> or <see cref="UnauthorizedAccessException" /> when
    /// a save fails; the next change saves again.
    /// </param>
    public WordListsSaver(WordListsStore store, TimeProvider time, Action<Exception> failed) {
      _store = store;
      _failed = failed;
      _timer = time.CreateTimer(
          _ => Save(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    /// <summary>
    /// Takes the lists as they are after a change and saves them once no change followed for
    /// 2 seconds.
    /// </summary>
    /// <param name="lists">The snapshot of the lists after the change.</param>
    public void Changed(WordListsDocument lists) {
      Volatile.Write(ref _latest, lists);
      _timer.Change(_delay, Timeout.InfiniteTimeSpan);
    }

    /// <summary>
    /// Saves the latest snapshot now, if any change was taken.
    /// </summary>
    public void Flush() {
      _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
      Save();
    }

    /// <inheritdoc />
    public void Dispose() {
      _timer.Dispose();
    }

    private void Save() {
      lock (_fileLock) {
        if (Volatile.Read(ref _latest) is not { } lists) {
          return;
        }

        try {
          _store.Save(lists);
        } catch (Exception e) when (e is IOException or UnauthorizedAccessException) {
          _failed(e);
        }
      }
    }
  }
}
