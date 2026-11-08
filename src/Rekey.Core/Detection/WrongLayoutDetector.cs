using Rekey.Core.Languages;

namespace Rekey.Core.Detection {
  /// <summary>
  /// Decides whether a Word was typed in the wrong Layout and what it should become.
  /// </summary>
  /// <remarks>
  /// The Decision depends only on its inputs, so the same Word always gets the same Decision.
  /// </remarks>
  public sealed class WrongLayoutDetector {
    private const string _codeCharacters = @"@/\_:#";

    private readonly IReadOnlyList<DetectionLayout> _layouts;

    /// <summary>
    /// Initializes a new instance of the <see cref="WrongLayoutDetector" /> class.
    /// </summary>
    /// <param name="layouts">The active Layouts with the profiles of their Languages.</param>
    public WrongLayoutDetector(IEnumerable<DetectionLayout> layouts) {
      _layouts = [.. layouts];
    }

    /// <summary>
    /// Evaluates a finished Word.
    /// </summary>
    /// <param name="word">The Keystrokes of the Word.</param>
    /// <param name="current">The Layout the Word was typed in.</param>
    /// <param name="sensitivity">
    /// One of the enumeration values that specifies how much evidence a Correction needs.
    /// </param>
    /// <returns>The Decision.</returns>
    public Decision Decide(
        IReadOnlyList<Keystroke> word, LayoutId current, Sensitivity sensitivity) {
      DetectionThresholds thresholds = DetectionThresholds.For(sensitivity);
      DetectionLayout? typedIn =
          _layouts.FirstOrDefault(layout => layout.KeyMap.Layout == current);
      string? typed = typedIn?.KeyMap.Render(word);
      if (typedIn is null || typed is null || LooksLikeCode(typed)) {
        return Decision.Keep;
      }

      RenderingScore typedScore =
          RenderingScorer.Score(typedIn.Profile, TrimToLetters(typed, typedIn.Profile));
      List<Candidate> candidates = [.. Candidates(word, typedIn)
          .Where(candidate => candidate.Score.Value >= thresholds.GibberishCeiling)];
      if (typedScore.IsWord || candidates.Count == 0 || IsAmbiguous(candidates)) {
        return Decision.Keep;
      }

      Candidate best = candidates.MaxBy(candidate => candidate.Score.Value)!;
      if (SharesScript(typedIn, best.Layout) && (!best.Score.IsWord || best.Text == typed)) {
        return Decision.Keep;
      }

      LayoutId target = best.Layout.KeyMap.Layout;
      bool isShort = best.Letters.Length <= 2;
      return DecisionPolicy.Decide(typedScore, best.Score, isShort, thresholds) switch {
        DecisionKind.Correct => Decision.Correct(target, best.Text),
        DecisionKind.Defer => Decision.Defer(target, best.Text),
        _ => Decision.Keep,
      };
    }

    private static bool IsAmbiguous(IEnumerable<Candidate> candidates) {
      // Two Languages with the same word give the same text, so only distinct words conflict.
      return candidates.Where(candidate => candidate.Score.IsWord)
          .Select(candidate => candidate.Letters)
          .Distinct()
          .Skip(1)
          .Any();
    }

    // Russian and Ukrainian share most letters, so a Rendering there is only evidence when a key
    // differs and the result is a word (spec: stricter rules between same-script Layouts).
    private static bool SharesScript(DetectionLayout a, DetectionLayout b) {
      return a.Profile.Alphabet.AsSpan().ContainsAny(b.Profile.Alphabet);
    }

    // Punctuation at the edges is typed with the Correction but says nothing about the Language.
    private static string TrimToLetters(string rendering, LanguageProfile profile) {
      string normalised = WordNormaliser.Normalise(rendering);
      return normalised.Trim([.. normalised.Where(c => !profile.Alphabet.Contains(c))]);
    }

    private static bool LooksLikeCode(string text) {
      return text.Any(c => char.IsDigit(c) || _codeCharacters.Contains(c))
          || text.Zip(text.Skip(1))
              .Any(pair => char.IsLower(pair.First) && char.IsUpper(pair.Second));
    }

    private IEnumerable<Candidate> Candidates(
        IReadOnlyList<Keystroke> word, DetectionLayout typedIn) {
      foreach (DetectionLayout layout in _layouts.Where(layout => layout != typedIn)) {
        string? text = layout.KeyMap.Render(word);
        if (text is not null) {
          string letters = TrimToLetters(text, layout.Profile);
          yield return new Candidate(
              layout, text, letters, RenderingScorer.Score(layout.Profile, letters));
        }
      }
    }

    private sealed record Candidate(
        DetectionLayout Layout, string Text, string Letters, RenderingScore Score);
  }
}
