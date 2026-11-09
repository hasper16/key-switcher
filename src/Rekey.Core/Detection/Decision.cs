using Rekey.Core.Languages;

namespace Rekey.Core.Detection {
  /// <summary>
  /// Holds the outcome of evaluating a Word: keep it, correct it, or defer it.
  /// </summary>
  public sealed record Decision {
    private Decision(DecisionKind kind, IReadOnlyList<TargetRendering> targets) {
      Kind = kind;
      Targets = targets;
    }

    /// <summary>
    /// Gets the Decision to leave the Word as typed.
    /// </summary>
    public static Decision Keep { get; } = new(DecisionKind.Keep, []);

    /// <summary>
    /// Gets what to do with the Word.
    /// </summary>
    public DecisionKind Kind { get; }

    /// <summary>
    /// Gets the Layouts the Word may be corrected to with its Renderings there, best first: one
    /// for a Correction, one or more for a Deferred Word, none for
    /// <see cref="DecisionKind.Keep" />.
    /// </summary>
    public IReadOnlyList<TargetRendering> Targets { get; }

    /// <summary>
    /// Gets the best Layout for the Word, or <see langword="null" /> for
    /// <see cref="DecisionKind.Keep" />.
    /// </summary>
    public LayoutId? Target => Targets.Count == 0 ? null : Targets[0].Layout;

    /// <summary>
    /// Gets the Rendering of the Word in <see cref="Target" />, or <see langword="null" /> for
    /// <see cref="DecisionKind.Keep" />.
    /// </summary>
    public string? Text => Targets.Count == 0 ? null : Targets[0].Text;

    /// <summary>
    /// Returns the Decision to replace the Word with its Rendering in another Layout.
    /// </summary>
    /// <param name="target">The Layout to correct to.</param>
    /// <param name="text">The Rendering in that Layout.</param>
    /// <returns>The Decision.</returns>
    public static Decision Correct(LayoutId target, string text) {
      return new Decision(DecisionKind.Correct, [new TargetRendering(target, text)]);
    }

    /// <summary>
    /// Returns the Decision to wait for the next Word before correcting towards one Layout.
    /// </summary>
    /// <param name="target">The Layout the Word would be corrected to.</param>
    /// <param name="text">The Rendering in that Layout.</param>
    /// <returns>The Decision.</returns>
    public static Decision Defer(LayoutId target, string text) {
      return Defer([new TargetRendering(target, text)]);
    }

    /// <summary>
    /// Returns the Decision to wait for the next Word to show which of several Layouts the user
    /// is typing in.
    /// </summary>
    /// <param name="targets">The Layouts with the Word's Renderings there, best first.</param>
    /// <returns>The Decision.</returns>
    /// <exception cref="ArgumentException">The list is empty.</exception>
    public static Decision Defer(IReadOnlyList<TargetRendering> targets) {
      if (targets.Count == 0) {
        throw new ArgumentException("A Deferred Word needs a target Layout.", nameof(targets));
      }

      return new Decision(DecisionKind.Defer, [.. targets]);
    }

    /// <inheritdoc />
    public bool Equals(Decision? other) {
      return other is not null && Kind == other.Kind && Targets.SequenceEqual(other.Targets);
    }

    /// <inheritdoc />
    public override int GetHashCode() {
      return HashCode.Combine(Kind, Target, Text);
    }
  }
}
