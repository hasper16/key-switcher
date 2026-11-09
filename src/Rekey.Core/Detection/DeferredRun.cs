using Rekey.Core.Languages;

namespace Rekey.Core.Detection {
  /// <summary>
  /// Holds the Deferred Words that wait for the next Word to show which Layout the user is typing
  /// in.
  /// </summary>
  /// <remarks>
  /// Not thread-safe: the engine's worker thread owns it.
  /// </remarks>
  public sealed class DeferredRun {
    // Design decision (spec: Deferred Words are confirmed by the next Word).
    private const int _maxWords = 3;

    private readonly Queue<IReadOnlyList<TargetRendering>> _words = new();

    /// <summary>
    /// Takes the Decision for the next Word and returns the Correction it leads to.
    /// </summary>
    /// <remarks>
    /// A Correction confirms the waiting Words when each of them reads as plausible text in its
    /// Layout; they are then typed as they read there. A deferral that shares no Layout with the
    /// waiting Words starts a new run.
    /// </remarks>
    /// <param name="decision">The Decision for the Word.</param>
    /// <returns>
    /// The Correction of the Word, together with the run when the Word confirms it, or
    /// <see langword="null" /> when nothing is corrected now.
    /// </returns>
    public Correction? Add(Decision decision) {
      switch (decision.Kind) {
        case DecisionKind.Defer:
          if (!decision.Targets.Any(t => AllReadIn(t.Layout))) {
            Reset();
          }

          _words.Enqueue(decision.Targets);
          if (_words.Count > _maxWords) {
            _words.Dequeue();
          }

          return null;
        case DecisionKind.Correct:
          LayoutId target = decision.Target!.Value;
          IEnumerable<string> run = AllReadIn(target)
              ? _words.Select(targets => targets.First(t => t.Layout == target).Text)
              : [];
          var correction = new Correction(target, [.. run, decision.Text!]);
          Reset();
          return correction;
        default:
          Reset();
          return null;
      }
    }

    /// <summary>
    /// Drops the waiting Words, which stay as typed.
    /// </summary>
    /// <remarks>
    /// Call it on a focus change, a Layout Switch by the user or a long typing gap.
    /// </remarks>
    public void Reset() {
      _words.Clear();
    }

    private bool AllReadIn(LayoutId layout) {
      return _words.All(targets => targets.Any(t => t.Layout == layout));
    }
  }
}
