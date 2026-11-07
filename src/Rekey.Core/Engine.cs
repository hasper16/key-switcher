namespace Rekey.Core {
  /// <summary>
  /// Decides how Rekey responds to input. It returns actions and never calls Windows itself.
  /// </summary>
  public static class Engine {
    /// <summary>
    /// Returns the actions that respond to one input.
    /// </summary>
    /// <param name="inputEvent">The input to respond to.</param>
    /// <param name="context">The state around the input.</param>
    /// <returns>The actions to execute in order; empty when nothing should happen.</returns>
    public static IReadOnlyList<EngineAction> Handle(InputEvent inputEvent, Context context) {
      return [];
    }
  }
}
