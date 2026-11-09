namespace Rekey.Core.Detection {
  /// <summary>
  /// Turns the Scores of a Word's current and best Renderings into a Decision kind.
  /// </summary>
  internal static class DecisionPolicy {
    /// <summary>
    /// Returns what to do with a Word whose best other Rendering is plausible.
    /// </summary>
    /// <param name="current">The Score of the Rendering in the current Layout.</param>
    /// <param name="target">The Score of the best Rendering in another Layout.</param>
    /// <param name="isShort">
    /// <see langword="true" /> if the best Rendering has one or two letters; otherwise,
    /// <see langword="false" />.
    /// </param>
    /// <param name="thresholds">The thresholds of the user's Sensitivity.</param>
    /// <returns>One of the enumeration values that specifies the Decision.</returns>
    public static DecisionKind Decide(
        RenderingScore current,
        RenderingScore target,
        bool isShort,
        DetectionThresholds thresholds) {
      if (isShort) {
        return thresholds.CorrectsShortWords && target.IsWord
            ? DecisionKind.Correct
            : DecisionKind.Defer;
      }

      if (target.IsWord) {
        return target.Value - current.Value >= thresholds.MinWordMargin
            ? DecisionKind.Correct
            : DecisionKind.Defer;
      }

      return target.Value >= thresholds.StrongLetterScore
          ? thresholds.OnStrongLetters
          : thresholds.OnWeakLetters;
    }
  }
}
