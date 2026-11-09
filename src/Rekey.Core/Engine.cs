using Rekey.Core.Detection;

namespace Rekey.Core {
  /// <summary>
  /// Decides how Rekey responds to input. It returns actions and never calls Windows itself.
  /// </summary>
  /// <remarks>
  /// Not thread-safe: the worker thread owns the engine and feeds it every input in order.
  /// </remarks>
  public sealed class Engine {
    // Spec keyboard-capture: a pause of more than 2 seconds ends the Word, as in the original.
    private static readonly TimeSpan _typingGap = TimeSpan.FromSeconds(2);

    private readonly WordTracker _word = new();
    private TimeSpan? _lastKeyDown;

    /// <summary>
    /// Gets the Keystrokes of the current Word.
    /// </summary>
    public IReadOnlyList<Keystroke> Word => _word.Keystrokes;

    /// <summary>
    /// Gets the Deferred Words waiting for the next Word.
    /// </summary>
    public DeferredRun DeferredWords { get; } = new();

    /// <summary>
    /// Gets the focused control, or <see langword="null" /> before the first focus report.
    /// </summary>
    public Context? Focus { get; private set; }

    /// <summary>
    /// Returns the actions that respond to one input.
    /// </summary>
    /// <param name="inputEvent">The input to respond to.</param>
    /// <returns>The actions to execute in order; empty when nothing should happen.</returns>
    public IReadOnlyList<EngineAction> Handle(InputEvent inputEvent) {
      switch (inputEvent) {
        case KeyDown keyDown:
          OnKeyDown(keyDown);
          break;
        case MouseClick:
          _word.Clear();
          break;
        case FocusChanged focus:
          OnFocusChanged(focus);
          break;
        default:
          break;
      }

      return [];
    }

    private void OnKeyDown(KeyDown keyDown) {
      if (keyDown.Timestamp - _lastKeyDown > _typingGap) {
        _word.Clear();
        DeferredWords.Reset();
      }

      _lastKeyDown = keyDown.Timestamp;
      if (Focus is { IsPasswordField: false, IsElevated: false } or null) {
        _word.Add(keyDown);
      }
    }

    private void OnFocusChanged(FocusChanged focus) {
      Focus = new Context(focus.AppId, focus.IsPassword, focus.IsElevated);
      _word.Clear();
      DeferredWords.Reset();
    }
  }
}
