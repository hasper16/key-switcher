using System.Buffers;
using Rekey.Core.Detection;
using Rekey.Core.Languages;
using Rekey.Core.Words;

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

    // A Word on screen as typed in a Layout, with the Space or Tab typed after it, if any
    // (design D2 of add-manual-correction).
    private sealed record RecentWord(
        IReadOnlyList<Keystroke> Keystrokes, LayoutId Layout, string Trailer);

    // DeferredRun keeps at most three Words, so the run on screen never needs more.
    private const int _maxRunWords = 3;

    // Design D4 of add-manual-correction: longer selections, or ones with line breaks, are not
    // typed over; Rekey only switches Layout.
    private const int _maxSelection = 500;

    // Spec keyboard-capture: a pause of more than 2 seconds ends the Word, as in the original.
    private static readonly TimeSpan _typingGap = TimeSpan.FromSeconds(2);

    private static readonly SearchValues<char> _lineBreaks = SearchValues.Create("\r\n");

    // Spec text-replacement: the Undo Window, as in the original.
    private static readonly TimeSpan _undoWindow = TimeSpan.FromSeconds(5);

    private readonly WordTracker _word = new();
    private readonly LanguageSet _languages;
    private readonly WordEvaluator? _evaluate;
    private readonly TimeProvider _time;

    // The on-screen text of the Deferred Words, oldest first; the run on screen is these Words
    // joined by single spaces, so a Correction can find it.
    private readonly List<string> _run = [];
    private Dictionary<LayoutId, KeyMap> _keyMaps = [];
    private IReadOnlyList<LayoutId> _layoutOrder = [];
    private IReadOnlyList<KeyMap> _keyMapsInOrder = [];
    private WrongLayoutDetector _detector = new([]);
    private Settings _settings;
    private bool _selfPaused;
    private PendingReplacement? _pending;
    private CorrectionRecord? _lastCorrection;
    private RecentWord? _recent;
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
    /// <remarks>
    /// It uses the default settings and the system clock.
    /// </remarks>
    /// <param name="languages">The loaded Language Profiles.</param>
    public Engine(LanguageSet languages)
        : this(languages, new Settings(), TimeProvider.System) {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Engine" /> class with the user's settings.
    /// </summary>
    /// <param name="languages">The loaded Language Profiles.</param>
    /// <param name="settings">
    /// The user's settings; <see cref="SettingsChanged" /> replaces them.
    /// </param>
    /// <param name="time">The wall clock a Pause is compared with.</param>
    /// <param name="evaluate">
    /// The evaluator of Words ended by a held Trigger Key, or <see langword="null" /> to
    /// evaluate them against the Language Profiles of the installed Layouts. The default is
    /// <see langword="null" />.
    /// </param>
    /// <param name="words">
    /// The user's Protected Words and Learned Words, which the engine owns from now on, or
    /// <see langword="null" /> to start with empty lists. The default is
    /// <see langword="null" />.
    /// </param>
    public Engine(
        LanguageSet languages,
        Settings settings,
        TimeProvider time,
        WordEvaluator? evaluate = null,
        WordLists? words = null) {
      _languages = languages;
      Words = words ?? new WordLists();
      _settings = settings;
      _time = time;
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
    /// Gets a value indicating whether Auto Correction applies to the Word being typed now, or
    /// the reason it does not.
    /// </summary>
    /// <value>One of the enumeration values that specifies the answer.</value>
    public GateResult Gate => AutoCorrectionGate.Decide(
        _settings, Focus, _selfPaused, _word.LayoutSwitchedByUser, _time.GetUtcNow());

    /// <summary>
    /// Gets the Trigger Keys worth holding back now: the configured ones while Auto Correction
    /// applies, none otherwise.
    /// </summary>
    /// <value>A bitwise combination of the enumeration values that specifies the keys.</value>
    public TriggerKeys HeldTriggerKeys =>
        Gate == GateResult.Applies ? _settings.TriggerKeys : TriggerKeys.None;

    /// <summary>
    /// Gets the user's Protected Words and Learned Words; only the engine's thread may use them.
    /// </summary>
    public WordLists Words { get; }

    /// <summary>
    /// Gets the counts of the Words ended by a Trigger Key so far.
    /// </summary>
    public WordCounts WordCounts { get; private set; }

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
        case ManualCorrectionRequested manual:
          return OnManualCorrection(manual);
        case ReplacementFinished finished:
          return OnReplacementFinished(finished);
        case WordListEdited edit:
          return OnWordListEdited(edit);
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
          OnLayoutChanged(layout);
          break;
        case LayoutsInstalledChanged installed:
          OnLayoutsInstalledChanged(installed);
          break;
        case SettingsChanged changed:
          _settings = changed.Settings;
          break;
        case SelfPauseChanged pause:
          _selfPaused = pause.Paused;
          break;
        default:
          break;
      }

      return [];
    }

    private static TriggerKeys TriggerOf(KeyKind kind) {
      return kind switch {
        KeyKind.Space => TriggerKeys.Space,
        KeyKind.Enter => TriggerKeys.Enter,
        KeyKind.Tab => TriggerKeys.Tab,
        _ => TriggerKeys.None,
      };
    }

    private static string TypedBy(KeyKind trigger) {
      return trigger switch {
        KeyKind.Space => " ",
        KeyKind.Tab => "\t",
        _ => string.Empty,
      };
    }

    private IReadOnlyList<EngineAction> OnKeyDown(KeyDown keyDown) {
      // Modifiers are pressed on the way to Ctrl+Z or as the Hotkey, so they keep the
      // Correction undoable and neither end nor extend the typing gap.
      if (keyDown.Kind != KeyKind.Modifier) {
        if (keyDown.Timestamp - _lastKeyDown > _typingGap) {
          _word.Clear();
          EndRun();
        }

        _lastKeyDown = keyDown.Timestamp;
        _lastCorrection = null;
        _recent = RecentAfter(keyDown);
      }

      // A key stamped at or before the latest focus change may belong to the previous control,
      // perhaps a Password Field: the worker can read queued keys after focus moved on, and the
      // timestamps share one clock tick of about 15 ms. A key the host could not confirm outside
      // a Password Field is dropped the same way.
      if (keyDown.Timestamp <= _focusSince || !keyDown.FocusConfirmed) {
        _word.Drop(keyDown);
        _recent = null;
        EndRun();
        return [];
      }

      // An unknown focus may be a Password Field, so nothing is kept until it is known.
      if (!CanReadFocus()) {
        _recent = null;
        EndRun();
        return [];
      }

      if (EndsWordForEvaluation(keyDown)) {
        CountEndedWord();
        if (keyDown.Held) {
          return Evaluate(keyDown);
        }
      }

      if (WordTracker.EndsWord(keyDown)
          || (keyDown.Kind == KeyKind.Backspace && _word.Keystrokes.Count == 0)) {
        EndRun();
      }

      _word.Add(keyDown);
      return [];
    }

    // A configured Trigger Key while Auto Correction applies; a shortcut such as Ctrl+Space is
    // not a Trigger Key. The hook's snapshot of what to hold can lag, so the gate is asked here.
    private bool EndsWordForEvaluation(KeyDown keyDown) {
      const KeyModifiers shortcut = KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Windows;
      return (_settings.TriggerKeys & TriggerOf(keyDown.Kind)) != 0
          && (keyDown.Modifiers & shortcut) == 0
          && Gate == GateResult.Applies;
    }

    private void CountEndedWord() {
      if (_word.Unconfirmed) {
        WordCounts = WordCounts with {
          Ended = WordCounts.Ended + 1,
          Unconfirmed = WordCounts.Unconfirmed + 1,
        };
      } else if (_word.Keystrokes.Count > 0) {
        WordCounts = WordCounts with { Ended = WordCounts.Ended + 1 };
      } else {
        // A Trigger Key after nothing Rekey knows ends no Word.
      }
    }

    // Only a held key has not reached the application yet, so only then can the Word before it
    // be replaced.
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

      WordCounts = WordCounts with { Evaluated = WordCounts.Evaluated + 1 };

      // Spec word-learning: a Protected Word is kept, and keeping it ends the run before it.
      Decision decision = Words.IsProtected(typed)
          ? Decision.Keep
          : _evaluate?.Invoke(word, layout)
              ?? _detector.Decide(word, layout, _settings.Sensitivity);
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
      return CanUndo(undo.Timestamp) ? Undo() : ClearForUndo();
    }

    // Spec: after Enter the line may already have been submitted.
    private bool CanUndo(TimeSpan now) {
      return _lastCorrection is { Undoable: true } last && now - last.Time < _undoWindow;
    }

    private IReadOnlyList<EngineAction> Undo() {
      CorrectionRecord last = _lastCorrection!;
      ClearForUndo();
      _pending = new PendingReplacement(last, IsUndo: true);
      return [
          new ReplaceText(last.Rendering + last.TriggerText, last.Typed + last.TriggerText)];
    }

    private IReadOnlyList<EngineAction> ClearForUndo() {
      _lastCorrection = null;
      _recent = null;
      _word.Clear();
      EndRun();
      return [];
    }

    // Spec manual-correction: revert the last Correction, else correct the selection, the
    // current Word or the RecentWord, else switch to the next Layout.
    private IReadOnlyList<EngineAction> OnManualCorrection(ManualCorrectionRequested request) {
      if (!CanReadFocus() || CurrentLayout is not { } layout) {
        return [];
      }

      if (CanUndo(request.Timestamp)) {
        return Undo();
      }

      RecentWord? typed = WordOnScreen();
      ClearForUndo();

      // Spec: a selection the control does not expose is left as it is.
      if (request.SelectionUnreadable) {
        return NextLayout(layout);
      }

      if (request.Selection is { Length: > 0 } selection) {
        return ConvertSelection(selection, layout, request.Timestamp);
      }

      if (typed is null
          || !_keyMaps.TryGetValue(typed.Layout, out KeyMap? map)
          || map.Render(typed.Keystrokes) is not { } text
          || ManualTarget.Best(typed.Keystrokes, typed.Layout, _keyMapsInOrder,
              _languages.ProfileFor) is not { } best) {
        return NextLayout(layout);
      }

      var record = new CorrectionRecord(
          text, best.Text, typed.Trailer, typed.Layout, best.Layout, request.Timestamp,
          Undoable: true);
      _pending = new PendingReplacement(record, IsUndo: false);
      return [new ReplaceText(text + typed.Trailer, best.Text + typed.Trailer)];
    }

    // An unknown focus may be a Password Field.
    private bool CanReadFocus() {
      return Focus is { IsPasswordField: false, IsElevated: false };
    }

    private IReadOnlyList<EngineAction> ConvertSelection(
        string selection, LayoutId layout, TimeSpan now) {
      if (selection.Length > _maxSelection
          || selection.AsSpan().ContainsAny(_lineBreaks)
          || !_keyMaps.TryGetValue(layout, out KeyMap? source)
          || ManualTarget.BestConversion(selection, source, _keyMapsInOrder, _languages.ProfileFor)
              is not { } best) {
        return NextLayout(layout);
      }

      var record = new CorrectionRecord(
          selection, best.Text, string.Empty, layout, best.Layout, now, Undoable: true);
      _pending = new PendingReplacement(record, IsUndo: false);
      return [new TypeOverSelection(selection, best.Text)];
    }

    // The Word being typed, or else the RecentWord, with the Layout it was typed in.
    private RecentWord? WordOnScreen() {
      if (_word.Keystrokes.Count > 0 && !_word.LayoutSwitchedInside
          && CurrentLayout is { } layout) {
        return new RecentWord([.. _word.Keystrokes], layout, string.Empty);
      }

      return _recent;
    }

    private IReadOnlyList<EngineAction> NextLayout(LayoutId current) {
      LayoutId[] next = [.. ManualTarget.AfterCurrent(_layoutOrder, id => id, current).Take(1)];
      return next.Length == 0 ? [] : [new SwitchLayout(next[0])];
    }

    // Design D2 of add-manual-correction: only a Space or Tab right after a Word keeps it.
    private RecentWord? RecentAfter(KeyDown keyDown) {
      const KeyModifiers shortcut = KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Windows;
      bool endsWithBlank = keyDown.Kind is KeyKind.Space or KeyKind.Tab
          && (keyDown.Modifiers & shortcut) == 0;
      if (!endsWithBlank || _word.Keystrokes.Count == 0 || _word.LayoutSwitchedInside
          || CurrentLayout is not { } layout) {
        return null;
      }

      return new RecentWord([.. _word.Keystrokes], layout, TypedBy(keyDown.Kind));
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
      _recent = null;
      _word.Clear();
      return [new SwitchLayout(record.Target)];
    }

    private IReadOnlyList<EngineAction> OnWordListEdited(WordListEdited edit) {
      if (Apply(edit)) {
        return [new WordListsChanged()];
      }

      return edit.Remove ? [] : [new WordListFull()];
    }

    // True when the list changed; false when the word to remove was absent or the list refused
    // the word to add.
    private bool Apply(WordListEdited edit) {
      DateTimeOffset now = _time.GetUtcNow();
      if (edit.Language is not { } language) {
        return edit.Remove
            ? Words.Unprotect(edit.Text)
            : Words.Protect(edit.Text, WordSource.User, now);
      }

      return edit.Remove
          ? Words.Unlearn(edit.Text, language)
          : Words.Learn(edit.Text, language, WordSource.User, now);
    }

    private void OnLayoutChanged(LayoutChanged layout) {
      CurrentLayout = layout.Layout;
      if (!layout.ByUser) {
        return;
      }

      // A pause before the switch has already ended the Word typed before it.
      if (layout.Timestamp - _lastKeyDown > _typingGap) {
        _word.Clear();
      }

      // The user chose the Layout, so the Deferred Words stay as typed in the old one.
      EndRun();
      _recent = null;
      _word.MarkLayoutSwitchedByUser();

      // A switch before the Word's first key covers the Word typed right after it, so the
      // pause that ends that cover counts from the switch. Mid-Word, the typing gap still
      // counts from the last key.
      if (!_word.IsUnderway) {
        _lastKeyDown = layout.Timestamp;
      }
    }

    private void OnLayoutsInstalledChanged(LayoutsInstalledChanged installed) {
      ActiveProfiles = _languages.Activate(installed.Layouts);
      KeyMap[] keyMaps = [.. installed.KeyMaps.DistinctBy(map => map.Layout)];
      _keyMaps = keyMaps.ToDictionary(map => map.Layout);
      _layoutOrder = [.. installed.Layouts];
      _keyMapsInOrder = [.. installed.Layouts
          .Where(_keyMaps.ContainsKey)
          .Select(layout => _keyMaps[layout])];
      _detector = new WrongLayoutDetector(keyMaps.SelectMany(map =>
          _languages.ProfileFor(map.Layout) is { } profile
              ? [new DetectionLayout(map, profile, Words.LearnedIn(profile.Language))]
              : Array.Empty<DetectionLayout>()));
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
      _recent = null;
    }
  }
}
