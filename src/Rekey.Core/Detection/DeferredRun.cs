using Rekey.Core.Languages;

namespace Rekey.Core.Detection {
  /// <summary>
  /// Holds the Deferred Words that wait for the next Word to confirm their Layout.
  /// </summary>
  /// <remarks>
  /// Not thread-safe: the engine's worker thread owns it.
  /// </remarks>
  public sealed class DeferredRun {
    // Design decision (spec: Deferred Words are confirmed by the next Word).
    private const int _maxWords = 3;

    private readonly Queue<string> _texts = new();
    private LayoutId _target;

    /// <summary>
    /// Takes the Decision for the next Word and returns the Correction it leads to.
    /// </summary>
    /// <param name="decision">The Decision for the Word.</param>
    /// <returns>
    /// The Correction of the Word, together with the run when the Word confirms its Layout, or
    /// <see langword="null" /> when nothing is corrected now.
    /// </returns>
    public Correction? Add(Decision decision) {
      if (decision.Kind == DecisionKind.Keep) {
        Reset();
        return null;
      }

      LayoutId target = decision.Target!.Value;
      if (target != _target) {
        Reset();
        _target = target;
      }

      _texts.Enqueue(decision.Text!);
      if (decision.Kind == DecisionKind.Defer) {
        if (_texts.Count > _maxWords) {
          _texts.Dequeue();
        }

        return null;
      }

      var correction = new Correction(target, [.. _texts]);
      Reset();
      return correction;
    }

    /// <summary>
    /// Drops the waiting Words, which stay as typed.
    /// </summary>
    /// <remarks>
    /// Call it on a focus change, a Layout Switch by the user or a long typing gap.
    /// </remarks>
    public void Reset() {
      _texts.Clear();
    }
  }
}
