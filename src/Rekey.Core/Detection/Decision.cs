using Rekey.Core.Languages;

namespace Rekey.Core.Detection {
  /// <summary>
  /// Holds the outcome of evaluating a Word: keep it, or correct or defer it towards a Layout.
  /// </summary>
  public sealed record Decision {
    private Decision(DecisionKind kind, LayoutId? target, string? text) {
      Kind = kind;
      Target = target;
      Text = text;
    }

    /// <summary>
    /// Gets the Decision to leave the Word as typed.
    /// </summary>
    public static Decision Keep { get; } = new(DecisionKind.Keep, null, null);

    /// <summary>
    /// Gets what to do with the Word.
    /// </summary>
    public DecisionKind Kind { get; }

    /// <summary>
    /// Gets the Layout the Word should be in, or <see langword="null" /> for
    /// <see cref="DecisionKind.Keep" />.
    /// </summary>
    public LayoutId? Target { get; }

    /// <summary>
    /// Gets the Rendering of the Word in <see cref="Target" />, or <see langword="null" /> for
    /// <see cref="DecisionKind.Keep" />.
    /// </summary>
    public string? Text { get; }

    /// <summary>
    /// Returns the Decision to replace the Word with its Rendering in another Layout.
    /// </summary>
    /// <param name="target">The Layout to correct to.</param>
    /// <param name="text">The Rendering in that Layout.</param>
    /// <returns>The Decision.</returns>
    public static Decision Correct(LayoutId target, string text) {
      return new Decision(DecisionKind.Correct, target, text);
    }

    /// <summary>
    /// Returns the Decision to wait for the next Word before correcting towards a Layout.
    /// </summary>
    /// <param name="target">The Layout the Word would be corrected to.</param>
    /// <param name="text">The Rendering in that Layout.</param>
    /// <returns>The Decision.</returns>
    public static Decision Defer(LayoutId target, string text) {
      return new Decision(DecisionKind.Defer, target, text);
    }
  }
}
