using Rekey.Core.Languages;

namespace Rekey.Core.Words {
  /// <summary>
  /// Holds the Protected Words and Learned Words while Rekey runs.
  /// </summary>
  /// <remarks>
  /// Not thread-safe: the engine's worker thread owns the lists and alone changes them; others
  /// get a <see cref="Snapshot" />. Words are matched normalised and without punctuation at their
  /// edges.
  /// </remarks>
  public sealed class WordLists {
    /// <summary>
    /// The most entries each list holds (design D4 of add-word-learning).
    /// </summary>
    public const int Capacity = 10_000;

    private readonly WordList<string> _protected;
    private readonly WordList<(Language Language, string Text)> _learned;
    private readonly Dictionary<Language, HashSet<string>> _learnedByLanguage = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="WordLists" /> class with empty lists.
    /// </summary>
    public WordLists()
        : this(WordListsDocument.Empty) {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WordLists" /> class from a snapshot.
    /// </summary>
    /// <remarks>
    /// Duplicates merge, and entries beyond <see cref="Capacity" /> are handled as new ones.
    /// </remarks>
    /// <param name="lists">The lists to start with, as loaded from the file.</param>
    public WordLists(WordListsDocument lists) {
      _protected = new WordList<string>(_ => { });
      _learned = new WordList<(Language Language, string Text)>(
          dropped => SetOf(dropped.Language).Remove(dropped.Text));
      foreach (ProtectedWord word in lists.Protected) {
        Protect(word.Text, word.Source, word.LastUsed);
      }

      foreach (LearnedWord word in lists.Learned) {
        Learn(word.Text, word.Language, word.Source, word.LastUsed);
      }
    }

    /// <summary>
    /// Returns the form in which the lists store and match a word.
    /// </summary>
    /// <param name="text">The word, perhaps with punctuation at its edges.</param>
    /// <returns>
    /// The word normalised, without leading or trailing characters that are no letter or digit.
    /// </returns>
    public static string Key(string text) {
      string normalised = WordNormaliser.Normalise(text);
      return normalised.Trim([.. normalised.Where(c => !char.IsLetterOrDigit(c))]);
    }

    /// <summary>
    /// Returns a value indicating whether a Rendering matches a Protected Word.
    /// </summary>
    /// <param name="rendering">The Rendering in the current Layout, in any case.</param>
    /// <returns>
    /// <see langword="true" /> if it matches; otherwise, <see langword="false" />.
    /// </returns>
    public bool IsProtected(string rendering) {
      return _protected.Contains(Key(rendering));
    }

    /// <summary>
    /// Adds a Protected Word, or refreshes it when present.
    /// </summary>
    /// <param name="text">The word.</param>
    /// <param name="source">Who adds it.</param>
    /// <param name="now">The current time.</param>
    /// <returns>
    /// <see langword="false" /> if the list is full of user entries and the word is new;
    /// otherwise, <see langword="true" />.
    /// </returns>
    public bool Protect(string text, WordSource source, DateTimeOffset now) {
      string key = Key(text);
      return key.Length > 0 && _protected.Add(key, source, now);
    }

    /// <summary>
    /// Removes a Protected Word.
    /// </summary>
    /// <param name="text">The word.</param>
    /// <returns><see langword="true" /> if it was on the list.</returns>
    public bool Unprotect(string text) {
      return _protected.Remove(Key(text));
    }

    /// <summary>
    /// Returns the Learned Words of a Language; the set follows later changes.
    /// </summary>
    /// <param name="language">The Language.</param>
    /// <returns>The words, as <see cref="Key" /> forms them.</returns>
    public IReadOnlySet<string> LearnedIn(Language language) {
      return SetOf(language);
    }

    /// <summary>
    /// Adds a Learned Word, or refreshes it when present.
    /// </summary>
    /// <param name="text">The word.</param>
    /// <param name="language">The Language it belongs to.</param>
    /// <param name="source">Who adds it.</param>
    /// <param name="now">The current time.</param>
    /// <returns>
    /// <see langword="false" /> if the list is full of user entries and the word is new;
    /// otherwise, <see langword="true" />.
    /// </returns>
    public bool Learn(string text, Language language, WordSource source, DateTimeOffset now) {
      string key = Key(text);
      if (key.Length == 0 || !_learned.Add((language, key), source, now)) {
        return false;
      }

      SetOf(language).Add(key);
      return true;
    }

    /// <summary>
    /// Removes a Learned Word.
    /// </summary>
    /// <param name="text">The word.</param>
    /// <param name="language">The Language it belongs to.</param>
    /// <returns><see langword="true" /> if it was on the list.</returns>
    public bool Unlearn(string text, Language language) {
      string key = Key(text);
      return _learned.Remove((language, key)) && SetOf(language).Remove(key);
    }

    /// <summary>
    /// Returns a snapshot of both lists, safe to hand to another thread.
    /// </summary>
    /// <returns>The snapshot.</returns>
    public WordListsDocument Snapshot() {
      ProtectedWord[] words = [.. _protected.Entries
          .Select(entry => new ProtectedWord(entry.Key, entry.Source, entry.LastUsed))];
      LearnedWord[] learned = [.. _learned.Entries
          .Select(entry => new LearnedWord(
              entry.Key.Text, entry.Key.Language, entry.Source, entry.LastUsed))];
      return new WordListsDocument(words, learned);
    }

    private HashSet<string> SetOf(Language language) {
      if (!_learnedByLanguage.TryGetValue(language, out HashSet<string>? words)) {
        words = [];
        _learnedByLanguage[language] = words;
      }

      return words;
    }
  }
}
