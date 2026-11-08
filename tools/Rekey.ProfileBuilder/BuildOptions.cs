namespace Rekey.ProfileBuilder {
  /// <summary>
  /// Holds the generator settings.
  /// </summary>
  /// <param name="TopWords">The number of most frequent words to keep (design D4).</param>
  /// <param name="MinCount">The lowest corpus count a word needs to be kept.</param>
  /// <param name="TopNGrams">The number of most frequent letter pairs and triples to keep.</param>
  internal sealed record BuildOptions(
      int TopWords = 80_000, int MinCount = 3, int TopNGrams = 300);
}
