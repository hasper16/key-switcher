namespace Rekey.Core.Detection {
  /// <summary>
  /// Specifies the outcome of evaluating a Word.
  /// </summary>
  public enum DecisionKind {
    /// <summary>Leave the Word as typed.</summary>
    Keep,

    /// <summary>Wait for the next Word to show which Layout the user is typing in.</summary>
    Defer,

    /// <summary>Replace the Word with its Rendering in another Layout.</summary>
    Correct,
  }
}
