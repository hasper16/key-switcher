using Rekey.Core.Languages;

namespace Rekey.Core.Detection {
  /// <summary>
  /// Chooses the Rendering of a Manual Correction (design D3 of add-manual-correction).
  /// </summary>
  internal static class ManualTarget {
    // Below any Score, for a Layout no Language Profile covers.
    private const double _unscored = -1;

    // Ranks the Renderings in the other Layouts by Score and takes the best even when it is no
    // common word, since the user asked. Ties go to the Layout that comes first after the
    // current one in Windows' order.
    public static TargetRendering? Best(
        IReadOnlyList<Keystroke> word,
        LayoutId current,
        IReadOnlyList<KeyMap> keyMapsInOrder,
        Func<LayoutId, LanguageProfile?> profileFor) {
      TargetRendering? best = null;
      double bestScore = double.NegativeInfinity;
      foreach (KeyMap map in AfterCurrent(keyMapsInOrder, current)) {
        if (map.Render(word) is not { } text) {
          continue;
        }

        double score = profileFor(map.Layout) is { } profile
            ? RenderingScorer.Score(profile, WrongLayoutDetector.TrimToLetters(text, profile)).Value
            : _unscored;
        if (score > bestScore) {
          (best, bestScore) = (new TargetRendering(map.Layout, text), score);
        }
      }

      return best;
    }

    // Converts a selection to every other Layout and takes the conversion whose words score
    // best on average; null when no Layout changes the text.
    public static TargetRendering? BestConversion(
        string selection,
        KeyMap source,
        IReadOnlyList<KeyMap> keyMapsInOrder,
        Func<LayoutId, LanguageProfile?> profileFor) {
      TargetRendering? best = null;
      double bestScore = double.NegativeInfinity;
      foreach (KeyMap map in AfterCurrent(keyMapsInOrder, source.Layout)) {
        string text = SelectionConversion.Convert(selection, source, map);
        if (text == selection) {
          continue;
        }

        double score = profileFor(map.Layout) is { } profile ? MeanScore(text, profile) : _unscored;
        if (score > bestScore) {
          (best, bestScore) = (new TargetRendering(map.Layout, text), score);
        }
      }

      return best;
    }

    // The Layouts other than the current one, starting with the one after it.
    internal static IEnumerable<T> AfterCurrent<T>(
        IReadOnlyList<T> inOrder, Func<T, LayoutId> layoutOf, LayoutId current) {
      int at = inOrder.Select(layoutOf).ToList().IndexOf(current);
      return Enumerable.Range(1, inOrder.Count)
          .Select(step => inOrder[(at + step + inOrder.Count) % inOrder.Count])
          .Where(item => layoutOf(item) != current);
    }

    private static double MeanScore(string text, LanguageProfile profile) {
      double[] scores = [.. text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
          .Select(word => WrongLayoutDetector.TrimToLetters(word, profile))
          .Where(letters => letters.Length > 0)
          .Select(letters => RenderingScorer.Score(profile, letters).Value)];
      return scores.Length == 0 ? _unscored : scores.Average();
    }

    private static IEnumerable<KeyMap> AfterCurrent(
        IReadOnlyList<KeyMap> keyMapsInOrder, LayoutId current) {
      return AfterCurrent(keyMapsInOrder, map => map.Layout, current);
    }
  }
}
