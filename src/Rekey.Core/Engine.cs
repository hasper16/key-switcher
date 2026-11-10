using Rekey.Core.Detection;
using Rekey.Core.Languages;

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
    private readonly LanguageSet _languages;
    private TimeSpan? _lastKeyDown;
    private TimeSpan _focusSince = TimeSpan.MinValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="Engine" /> class without Language Profiles.
    /// </summary>
    public Engine()
        : this(new LanguageSet([])) {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Engine" /> class.
    /// </summary>
    /// <param name="languages">The loaded Language Profiles.</param>
    public Engine(LanguageSet languages) {
      _languages = languages;
    }

    /// <summary>
    /// Gets the Keystrokes of the current Word.
    /// </summary>
    public IReadOnlyList<Keystroke> Word => _word.Keystrokes;

    /// <summary>
    /// Gets the Deferred Words waiting for the next Word.
    /// </summary>
    public DeferredRun DeferredWords { get; } = new();

    /// <summary>
    /// Gets the focused control, or <see langword="null" /> while it is unknown: before the first
    /// focus report and after <see cref="FocusLost" />.
    /// </summary>
    public Context? Focus { get; private set; }

    /// <summary>
    /// Gets the Layout of the foreground window, or <see langword="null" /> before the first
    /// report.
    /// </summary>
    public LayoutId? CurrentLayout { get; private set; }

    /// <summary>
    /// Gets the profiles of the Languages the installed Layouts make active, in load order.
    /// </summary>
    public IReadOnlyList<LanguageProfile> ActiveProfiles { get; private set; } = [];

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
        case FocusLost lost:
          _focusSince = TimeSpan.FromTicks(Math.Max(_focusSince.Ticks, lost.Timestamp.Ticks));
          Focus = null;
          _word.Clear();
          DeferredWords.Reset();
          break;
        case LayoutChanged layout:
          CurrentLayout = layout.Layout;
          break;
        case LayoutsInstalledChanged installed:
          ActiveProfiles = _languages.Activate(installed.Layouts);
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

      // A key stamped at or before the latest focus change may belong to the previous control,
      // perhaps a Password Field: the worker can read queued keys after focus moved on, and the
      // timestamps share one clock tick of about 15 ms.
      if (keyDown.Timestamp <= _focusSince) {
        _word.Drop(keyDown);
        return;
      }

      // An unknown focus may be a Password Field, so nothing is kept until it is known.
      if (Focus is { IsPasswordField: false, IsElevated: false }) {
        _word.Add(keyDown);
      }
    }

    private void OnFocusChanged(FocusChanged focus) {
      _focusSince = TimeSpan.FromTicks(Math.Max(_focusSince.Ticks, focus.Timestamp.Ticks));
      Focus = new Context(focus.AppId, focus.IsPassword, focus.IsElevated);
      _word.Clear();
      DeferredWords.Reset();
    }
  }
}
