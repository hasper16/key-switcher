using System.Diagnostics;
using Rekey.Core.Detection;
using Rekey.Core.Languages;

namespace Rekey.Accuracy {
  /// <summary>
  /// Types the corpus word by word through the detector and counts false and missed corrections.
  /// </summary>
  /// <remarks>
  /// Active Layouts are English (US), Russian and Ukrainian (Enhanced); the last types the
  /// Ukrainian apostrophe. Each stream runs as continuous typing, so a deferred last Word of a
  /// sentence can be confirmed by the next sentence.
  /// </remarks>
  internal sealed class AccuracyHarness {
    private const int _worstCount = 50;

    private static readonly (Language Language, string Code, string Klid)[] _typingLayouts = [
        (Language.English, "en", "00000409"),
        (Language.Russian, "ru", "00000419"),
        (Language.Ukrainian, "uk", "00020422")];

    private readonly WrongLayoutDetector _detector;
    private readonly IReadOnlyList<Typist> _typists;

    private AccuracyHarness(IReadOnlyList<Typist> typists, LanguageSet profiles) {
      _typists = typists;
      _detector = new WrongLayoutDetector(typists.Select(typist => new DetectionLayout(
          typist.KeyMap,
          profiles.Profiles.Single(profile => profile.Language == typist.Language))));
    }

    /// <summary>
    /// Creates a harness from the bundled profiles and captured Key Maps.
    /// </summary>
    /// <param name="keyMapFolder">The folder that holds the Key Map files.</param>
    /// <returns>The harness.</returns>
    public static AccuracyHarness Create(string keyMapFolder) {
      Typist[] typists = [.. _typingLayouts.Select(layout => new Typist(
          layout.Language, layout.Code, ReadKeyMap(keyMapFolder, layout.Klid)))];
      return new AccuracyHarness(
          typists, LanguageSet.LoadBundled(DateTimeOffset.UtcNow).Languages);
    }

    /// <summary>
    /// Runs the corpus at one Sensitivity.
    /// </summary>
    /// <param name="natural">The held-out sentences.</param>
    /// <param name="technical">The technical and mixed-language lines.</param>
    /// <param name="sensitivity">The Sensitivity to run at.</param>
    /// <returns>The report.</returns>
    public AccuracyReport Run(
        IReadOnlyList<Passage> natural,
        IReadOnlyList<Passage> technical,
        Sensitivity sensitivity) {
      var tally = new Tally(_detector, sensitivity);
      foreach (Typist typist in _typists) {
        string[] words =
            [.. natural.Where(p => p.Language == typist.Language).SelectMany(Words)];
        tally.Stream(
            $"{typist.Code} typed correctly",
            isPositive: false,
            words.Select(word => Typed(word, typist, typist)));
        foreach (Typist wrong in _typists.Where(other => other != typist)) {
          tally.Stream(
              $"{typist.Code} typed in {wrong.Code}",
              isPositive: true,
              words.Select(word => Typed(word, typist, wrong)));
        }
      }

      IEnumerable<TypedWord> technicalWords = technical.SelectMany(
          passage => Words(passage).Select(word => {
            Typist typist = _typists.Single(t => t.Language == (word.Any(char.IsAsciiLetter)
                ? Language.English
                : passage.Language));
            return Typed(word, typist, typist);
          }));
      tally.Stream("technical", isPositive: false, technicalWords);
      return tally.Report(_worstCount);
    }

    private static KeyMap ReadKeyMap(string folder, string klid) {
      using FileStream stream = File.OpenRead(Path.Join(folder, $"{klid}.json"));
      return KeyMapJson.Read(stream);
    }

    private static IEnumerable<string> Words(Passage passage) {
      return passage.Text.Replace('’', '\'').Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    private static TypedWord Typed(string word, Typist typist, Typist current) {
      IReadOnlyList<Keystroke>? keys = typist.KeyMap.Type(word, capsLock: false);
      string? shown = keys is null ? null : current.KeyMap.Render(keys);
      return new TypedWord(word, keys, shown, current.KeyMap.Layout, typist.KeyMap.Layout);
    }

    private sealed record Typist(Language Language, string Code, KeyMap KeyMap);

    private sealed record TypedWord(
        string Source,
        IReadOnlyList<Keystroke>? Keys,
        string? Shown,
        LayoutId Current,
        LayoutId Intended) {
      public bool IsPositive => Current != Intended;

      // A wrong-layout Word whose keys read the same in both Layouts is not counted.
      public bool Counts => Keys is not null && (!IsPositive || Shown != Source);
    }

    private sealed class Tally(WrongLayoutDetector detector, Sensitivity sensitivity) {
      private readonly List<StreamResult> _streams = [];
      private readonly List<TimeSpan> _latencies = [];
      private readonly Dictionary<string, int> _problems = [];

      public void Stream(string name, bool isPositive, IEnumerable<TypedWord> words) {
        var run = new DeferredRun();
        var history = new List<TypedWord>();
        var corrections = new List<string?>();
        foreach (TypedWord word in words) {
          history.Add(word);
          corrections.Add(null);
          IReadOnlyList<string> texts = run.Add(Decide(word))?.Texts ?? [];
          for (int i = 0; i < texts.Count; i++) {
            corrections[history.Count - texts.Count + i] = texts[i];
          }
        }

        _streams.Add(Count(name, isPositive, history, corrections));
      }

      public AccuracyReport Report(int worstCount) {
        TimeSpan[] sorted = [.. _latencies.Order()];
        IEnumerable<string> worst = _problems.OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key, StringComparer.Ordinal)
            .Take(worstCount)
            .Select(pair => $"{pair.Value,5}  {pair.Key}");
        return new AccuracyReport(
            sensitivity, _streams, Percentile(sorted, 0.50), Percentile(sorted, 0.99), [.. worst]);
      }

      private static TimeSpan Percentile(TimeSpan[] sorted, double share) {
        return sorted.Length == 0
            ? TimeSpan.Zero
            : sorted[Math.Min(sorted.Length - 1, (int)(sorted.Length * share))];
      }

      private Decision Decide(TypedWord word) {
        if (word.Keys is null) {
          return Decision.Keep;
        }

        long start = Stopwatch.GetTimestamp();
        Decision decision = detector.Decide(word.Keys, word.Current, sensitivity);
        _latencies.Add(Stopwatch.GetElapsedTime(start));
        return decision;
      }

      private StreamResult Count(
          string name, bool positive, List<TypedWord> history, List<string?> corrections) {
        int words = 0;
        int hits = 0;
        for (int i = 0; i < history.Count; i++) {
          TypedWord word = history[i];
          if (!word.Counts) {
            continue;
          }

          words++;
          string? correction = corrections[i];
          bool hit = positive ? correction == word.Source : correction is not null;
          hits += hit ? 1 : 0;
          if (hit != positive) {
            string outcome = correction is null ? "kept" : $"became \"{correction}\"";
            Note(positive
                ? $"miss   {name}: \"{word.Shown}\" {outcome}, meant \"{word.Source}\""
                : $"false  {name}: \"{word.Source}\" {outcome}");
          }
        }

        return new StreamResult(name, positive, words, hits);
      }

      private void Note(string problem) {
        _problems[problem] = _problems.GetValueOrDefault(problem) + 1;
      }
    }
  }
}
