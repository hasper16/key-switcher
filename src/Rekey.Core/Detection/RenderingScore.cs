namespace Rekey.Core.Detection {
  /// <summary>
  /// Holds the Score of a Rendering in one Language.
  /// </summary>
  /// <param name="IsWord">
  /// <see langword="true" /> if the Rendering is a word of the Language; otherwise,
  /// <see langword="false" />.
  /// </param>
  /// <param name="Value">
  /// The Score: from 1 to 2 for a word, higher for more frequent words; from 0 to 1 for other
  /// text, higher for more frequent letter pairs and triples.
  /// </param>
  public readonly record struct RenderingScore(bool IsWord, double Value);
}
