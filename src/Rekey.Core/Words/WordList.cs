namespace Rekey.Core.Words {
  // One bounded word list (design D4 of add-word-learning): when it is full, a new entry takes
  // the place of the automatic entry used least recently; user entries are never dropped.
  internal sealed class WordList<TKey>
      where TKey : notnull {
    private readonly Dictionary<TKey, Entry> _entries = [];
    private readonly Action<TKey> _dropped;

    public WordList(Action<TKey> dropped) {
      _dropped = dropped;
    }

    public IEnumerable<(TKey Key, WordSource Source, DateTimeOffset LastUsed)> Entries =>
        _entries.Select(pair => (pair.Key, pair.Value.Source, pair.Value.LastUsed));

    public bool Contains(TKey key) {
      return _entries.ContainsKey(key);
    }

    // False when the list is full of user entries and the key is new.
    public bool Add(TKey key, WordSource source, DateTimeOffset now) {
      if (_entries.TryGetValue(key, out Entry existing)) {
        WordSource kept = existing.Source == WordSource.User ? WordSource.User : source;
        _entries[key] = new Entry(kept, now);
        return true;
      }

      if (_entries.Count >= WordLists.Capacity && !DropLeastRecentlyUsedAuto()) {
        return false;
      }

      _entries[key] = new Entry(source, now);
      return true;
    }

    public bool Remove(TKey key) {
      return _entries.Remove(key);
    }

    private bool DropLeastRecentlyUsedAuto() {
      KeyValuePair<TKey, Entry>[] auto =
          [.. _entries.Where(pair => pair.Value.Source == WordSource.Auto)];
      if (auto.Length == 0) {
        return false;
      }

      TKey oldest = auto.MinBy(pair => pair.Value.LastUsed).Key;
      _entries.Remove(oldest);
      _dropped(oldest);
      return true;
    }

    private readonly record struct Entry(WordSource Source, DateTimeOffset LastUsed);
  }
}
