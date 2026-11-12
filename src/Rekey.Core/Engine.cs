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
    // A Correction as typed and as replaced, kept for its Undo.
    private sealed record CorrectionRecord(
        string Typed,
        string Rendering,
        string TriggerText,
        LayoutId Source,
        LayoutId Target,
        TimeSpan Time,
        bool Undoable);

    private sealed record PendingReplacement(CorrectionRecord Correction, bool IsUndo);

    // DeferredRun keeps at most three Words, so the run on screen never needs more.
    private const int _maxRunWords = 3;

    // Spec keyboard-capture: a pause of more than 2 seconds ends the Word, as in the original.
    private static readonly TimeSpan _typingGap = TimeSpan.FromSeconds(2);

    // Spec text-replacement: the Undo Window, as in the original.
    private static readonly TimeSpan _undoWindow = TimeSpan.FromSeconds(5);

    private readonly WordTracker _word = new();
    private readonly LanguageSet _languages;
    private readonly WordEvaluator? _evaluate;

    // The on-screen text of the Deferred Words, oldest first; the run on screen is these Words
    // joined by single spaces, so a Correction can find it.
    private readonly List<string> _run = [];
    private Dictionary<LayoutId, KeyMap> _keyMaps = [];
    private PendingReplacement? _pending;
    private CorrectionRecord? _lastCorrection;
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
    public Engine(LanguageSet languages)
        : this(languages, null) {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Engine" /> class that corrects Words.
    /// </summary>
    /// <param name="languages">The loaded Language Profiles.</param>
    /// <param name="evaluate">
    /// The evaluator of Words ended by a held Trigger Key, or <see langword="null" /> to correct
    /// nothing.
    /// </param>
    public Engine(LanguageSet languages, WordEvaluator? evaluate) {
      _languages = languages;
      _evaluate = evaluate;
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
          return OnKeyDown(keyDown);
        case UndoRequested undo:
          return OnUndoRequested(undo);
        case ReplacementFinished finished:
          return OnReplacementFinished(finished);
        case MouseClick:
          _word.Clear();
          Interrupt();
          break;
        case FocusChanged focus:
          OnFocusChanged(focus);
          break;
        case InputLost:
          _word.Spoil();
          Interrupt();
          break;
        case FocusLost lost:
          _focusSince = TimeSpan.FromTicks(Math.Max(_focusSince.Ticks, lost.Timestamp.Ticks));
          Focus = null;
          _word.Clear();
          Interrupt();
          break;
        case LayoutChanged layout:
          CurrentLayout = layout.Layout;
          break;
        case LayoutsInstalledChanged installed:
          ActiveProfiles = _languages.Activate(installed.Layouts);
          _keyMaps = installed.KeyMaps.DistinctBy(map => map.Layout)
              .ToDictionary(map => map.Layout);
          break;
        default:
          break;
      }

      return [];
    }

    private static string TypedBy(KeyKind trigger) {
      return trigger switch {
        KeyKind.Space => " ",
        KeyKind.Tab => "\t",
        _ => string.Empty,
      };
    }

    private IReadOnlyList<EngineAction> OnKeyDown(KeyDown keyDown) {
      if (keyDown.Timestamp - _lastKeyDown > _typingGap) {
        _word.Clear();
        EndRun();
      }

      _lastKeyDown = keyDown.Timestamp;

      // Modifiers are pressed on the way to Ctrl+Z, so they keep the Correction undoable.
      if (keyDown.Kind != KeyKind.Modifier) {
        _lastCorrection = null;
      }

      // A key stamped at or before the latest focus change may belong to the previous control,
      // perhaps a Password Field: the worker can read queued keys after focus moved on, and the
      // timestamps share one clock tick of about 15 ms. A key the host could not confirm outside
      // a Password Field is dropped the same way.
      if (keyDown.Timestamp <= _focusSince || !keyDown.FocusConfirmed) {
        _word.Drop(keyDown);
        EndRun();
        return [];
      }

      // An unknown focus may be a Password Field, so nothing is kept until it is known.
      if (Focus is not { IsPasswordField: false, IsElevated: false }) {
        EndRun();
        return [];
      }

      if (IsTrigger(keyDown)) {
        return Evaluate(keyDown);
      }

      if (WordTracker.EndsWord(keyDown)
          || (keyDown.Kind == KeyKind.Backspace && _word.Keystrokes.Count == 0)) {
        EndRun();
      }

      _word.Add(keyDown);
      return [];
    }

    // Only a held key has not reached the application yet, so only then can the Word before it
    // be replaced; a shortcut such as Ctrl+Space is not a Trigger Key.
    private bool IsTrigger(KeyDown keyDown) {
      const KeyModifiers shortcut = KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Windows;
      return _evaluate is not null
          && keyDown.Held
          && keyDown.Kind is KeyKind.Space or KeyKind.Enter or KeyKind.Tab
          && (keyDown.Modifiers & shortcut) == 0;
    }

    private IReadOnlyList<EngineAction> Evaluate(KeyDown trigger) {
      IReadOnlyList<Keystroke> word = [.. _word.Keystrokes];
      _word.Add(trigger);
      if (CurrentLayout is not { } layout
          || !_keyMaps.TryGetValue(layout, out KeyMap? map)
          || word.Count == 0
          || map.Render(word) is not { } typed) {
        EndRun();
        return [];
      }

      Decision decision = _evaluate!(word, layout);
      Correction? correction = DeferredWords.Add(decision);
      if (correction is null) {
        ContinueOrEndRun(decision, trigger, typed);
        return [];
      }

      string[] typedWords = [.. _run.TakeLast(correction.Texts.Count - 1), typed];
      EndRun();
      if (typedWords.Length != correction.Texts.Count) {
        return [];
      }

      var record = new CorrectionRecord(
          string.Join(' ', typedWords),
          string.Join(' ', correction.Texts),
          TypedBy(trigger.Kind),
          layout,
          correction.Target,
          trigger.Timestamp,
          Undoable: trigger.Kind != KeyKind.Enter);
      _pending = new PendingReplacement(record, IsUndo: false);
      return [new ReplaceText(record.Typed, record.Rendering)];
    }

    // Only Words separated by single spaces stay a run on screen.
    private void ContinueOrEndRun(Decision decision, KeyDown trigger, string typed) {
      if (decision.Kind != DecisionKind.Defer || trigger.Kind != KeyKind.Space) {
        EndRun();
        return;
      }

      _run.Add(typed);
      if (_run.Count > _maxRunWords) {
        _run.RemoveAt(0);
      }
    }

    private IReadOnlyList<EngineAction> OnUndoRequested(UndoRequested undo) {
      CorrectionRecord? last = _lastCorrection;
      _lastCorrection = null;
      _word.Clear();
      EndRun();

      // Spec: after Enter the line may already have been submitted.
      if (last is not { Undoable: true } || undo.Timestamp - last.Time >= _undoWindow) {
        return [];
      }

      _pending = new PendingReplacement(last, IsUndo: true);
      return [
          new ReplaceText(last.Rendering + last.TriggerText, last.Typed + last.TriggerText)];
    }

    private IReadOnlyList<EngineAction> OnReplacementFinished(ReplacementFinished finished) {
      PendingReplacement? pending = _pending;
      _pending = null;
      if (pending is null || !finished.Applied) {
        return [];
      }

      CorrectionRecord record = pending.Correction;
      if (pending.IsUndo) {
        return [
            new SwitchLayout(record.Source), new UndoPerformed(record.Typed, record.Rendering)];
      }

      _lastCorrection = record;
      return [new SwitchLayout(record.Target)];
    }

    private void OnFocusChanged(FocusChanged focus) {
      _focusSince = TimeSpan.FromTicks(Math.Max(_focusSince.Ticks, focus.Timestamp.Ticks));
      Focus = new Context(focus.App, focus.IsPassword, focus.IsElevated);
      _word.Clear();
      Interrupt();
    }

    // The text on screen no longer continues the run, so the Deferred Words stay as typed.
    private void EndRun() {
      _run.Clear();
      DeferredWords.Reset();
    }

    // The caret may have moved away from the corrected text.
    private void Interrupt() {
      EndRun();
      _lastCorrection = null;
    }
  }
}
