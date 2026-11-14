namespace Rekey.Core.Words {
  /// <summary>
  /// Loads and saves the Protected Words and Learned Words in <c>words.json</c>.
  /// </summary>
  /// <remarks>
  /// The lists live apart from the settings so they can grow without rewriting the settings.
  /// </remarks>
  public sealed class WordListsStore {
    /// <summary>
    /// Initializes a new instance of the <see cref="WordListsStore" /> class.
    /// </summary>
    /// <param name="folder">
    /// The folder that holds the file; it is created on the first save.
    /// </param>
    public WordListsStore(string folder) {
      FilePath = Path.Join(folder, "words.json");
    }

    /// <summary>
    /// Gets the full path of the file.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Returns the stored lists.
    /// </summary>
    /// <remarks>
    /// When the file is missing, returns empty lists. When the file cannot be read as version 1,
    /// renames it with a <c>.bak</c> suffix (a dated one when that backup is locked) and returns
    /// empty lists.
    /// </remarks>
    /// <returns>The stored lists, or empty lists when there are none to read.</returns>
    public WordListsDocument Load() {
      if (!File.Exists(FilePath)) {
        return WordListsDocument.Empty;
      }

      WordListsDocument? lists = WordListsJson.Parse(File.ReadAllBytes(FilePath));
      if (lists is not null) {
        return lists;
      }

      JsonFile.KeepUnreadable(FilePath);
      return WordListsDocument.Empty;
    }

    /// <summary>
    /// Writes the lists to the file, replacing it in one step.
    /// </summary>
    /// <param name="lists">The lists to write.</param>
    public void Save(WordListsDocument lists) {
      byte[] json = WordListsJson.Write(lists);
      JsonFile.WriteAtomically(FilePath, stream => stream.Write(json));
    }
  }
}
