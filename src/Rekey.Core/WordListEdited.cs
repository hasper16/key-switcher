using System.Text;
using Rekey.Core.Languages;

namespace Rekey.Core {
  /// <summary>
  /// Reports that the user added or removed a Protected Word or a Learned Word in settings.
  /// </summary>
  /// <remarks>
  /// Create it with <see cref="Protect" />, <see cref="Unprotect" />, <see cref="Learn" /> or
  /// <see cref="Unlearn" />.
  /// </remarks>
  public sealed record WordListEdited : InputEvent {
    private WordListEdited(TimeSpan timestamp, string text, Language? language, bool remove)
        : base(timestamp) {
      Text = text;
      Language = language;
      Remove = remove;
    }

    /// <summary>
    /// Gets the word.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets the Language of a Learned Word, or <see langword="null" /> for a Protected Word.
    /// </summary>
    public Language? Language { get; }

    /// <summary>
    /// Gets a value indicating whether the word is removed rather than added.
    /// </summary>
    public bool Remove { get; }

    /// <summary>
    /// Returns the event for the user adding a Protected Word.
    /// </summary>
    /// <param name="timestamp">The time of the edit, measured from system start.</param>
    /// <param name="text">The word.</param>
    /// <returns>The event.</returns>
    public static WordListEdited Protect(TimeSpan timestamp, string text) {
      return new WordListEdited(timestamp, text, null, remove: false);
    }

    /// <summary>
    /// Returns the event for the user removing a Protected Word.
    /// </summary>
    /// <param name="timestamp">The time of the edit, measured from system start.</param>
    /// <param name="text">The word.</param>
    /// <returns>The event.</returns>
    public static WordListEdited Unprotect(TimeSpan timestamp, string text) {
      return new WordListEdited(timestamp, text, null, remove: true);
    }

    /// <summary>
    /// Returns the event for the user adding a Learned Word.
    /// </summary>
    /// <param name="timestamp">The time of the edit, measured from system start.</param>
    /// <param name="text">The word.</param>
    /// <param name="language">The Language the word belongs to.</param>
    /// <returns>The event.</returns>
    public static WordListEdited Learn(TimeSpan timestamp, string text, Language language) {
      return new WordListEdited(timestamp, text, language, remove: false);
    }

    /// <summary>
    /// Returns the event for the user removing a Learned Word.
    /// </summary>
    /// <param name="timestamp">The time of the edit, measured from system start.</param>
    /// <param name="text">The word.</param>
    /// <param name="language">The Language the word belongs to.</param>
    /// <returns>The event.</returns>
    public static WordListEdited Unlearn(TimeSpan timestamp, string text, Language language) {
      return new WordListEdited(timestamp, text, language, remove: true);
    }

    /// <inheritdoc />
    protected override bool PrintMembers(StringBuilder builder) {
      // Words stay out of logs.
      base.PrintMembers(builder);
      builder.Append(", Text.Length = ").Append(Text.Length)
          .Append(", Language = ").Append(Language)
          .Append(", Remove = ").Append(Remove);
      return true;
    }
  }
}
