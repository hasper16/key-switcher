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
          .Where(candidate => candidate.Score.Value >= thresholds.GibberishCeiling
              && IsEvidence(candidate, typedIn, typed))
          .OrderByDescending(candidate => candidate.Score.Value)];
      List<Candidate> words = [.. candidates.Where(candidate => candidate.Score.IsWord)];
      if (candidates.Count == 0 || (typedScore.IsWord && words.Count == 0)) {
        return Decision.Keep;
      }

      // Ambiguous Word: the next Word shows which Layout the user is typing in.
      if (typedScore.IsWord || IsAmbiguous(words)) {
        return Decision.Defer([.. words.Select(candidate => candidate.Target)]);
      }

      Candidate best = candidates[0];
      bool isShort = best.Letters.Length <= 2;
      return DecisionPolicy.Decide(typedScore, best.Score, isShort, thresholds) switch {
        DecisionKind.Correct => Decision.Correct(best.Target.Layout, best.Text),
        DecisionKind.Defer =>
            Decision.Defer([.. candidates.Select(candidate => candidate.Target)]),
        _ => Decision.Keep,
      };
    }

    // Punctuation at the edges is typed with the Correction but says nothing about the Language.
    internal static string TrimToLetters(string rendering, LanguageProfile profile) {
      string normalised = WordNormaliser.Normalise(rendering);
      return normalised.Trim([.. normalised.Where(c => !profile.Alphabet.Contains(c))]);
    }

    // Two Languages with the same word give the same text, so only distinct words conflict.
    private static bool IsAmbiguous(IEnumerable<Candidate> words) {
      return words.Select(candidate => candidate.Letters).Distinct().Skip(1).Any();
    }

    // Russian and Ukrainian share most letters, so a Rendering there is only evidence when a key
    // differs and the result is a word (spec: stricter rules between same-script Layouts).
    private static bool IsEvidence(Candidate candidate, DetectionLayout typedIn, string typed) {
      bool sharesScript =
          typedIn.Profile.Alphabet.AsSpan().ContainsAny(candidate.Layout.Profile.Alphabet);
      return !sharesScript || (candidate.Score.IsWord && candidate.Text != typed);
    }

    private static bool LooksLikeCode(string text) {
      string core = text.Trim([.. text.Where(c => !char.IsLetterOrDigit(c))]);
      return text.Any(char.IsDigit)
          || core.Any(_codeCharacters.Contains)
          || Enumerable.Range(1, Math.Max(0, core.Length - 2))
              .Any(i => char.IsLower(core[i - 1]) && char.IsUpper(core[i]));
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
        DetectionLayout Layout, string Text, string Letters, RenderingScore Score) {
      public TargetRendering Target => new(Layout.KeyMap.Layout, Text);
    }
  }
}
