using Rekey.Core.Languages;

namespace Rekey.Accuracy {
  /// <summary>
  /// Holds a sentence or line of the corpus and the Language its Cyrillic words are in.
  /// </summary>
  /// <param name="Language">The Language of the text.</param>
  /// <param name="Text">The text, Words separated by spaces.</param>
  internal sealed record Passage(Language Language, string Text);
}
