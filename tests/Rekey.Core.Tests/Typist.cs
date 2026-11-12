using Rekey.Core.Detection;
using Rekey.Core.Languages;
using Rekey.Core.Tests.Detection;

namespace Rekey.Core.Tests {
  // Feeds an Engine the events of a user typing in English (US), 100 ms apart unless told
  // otherwise.
  internal sealed class Typist {
    // The invented application the user types in.
    public static readonly AppId App = new(@"C:\Apps\Editor.exe");

    private static readonly TimeSpan _step = TimeSpan.FromMilliseconds(100);

    private TimeSpan _now = TimeSpan.FromSeconds(10);

    // The worker reports the focused control as it starts; a plain editor here.
    public Typist()
        : this(new Engine()) {
    }

    // An engine that asks the evaluator about Words, with English (US) and Russian installed
    // and English (US) current, as the worker reports them at start.
    public Typist(WordEvaluator evaluate)
        : this(new Engine(new LanguageSet([]), evaluate)) {
      Send(new LayoutsInstalledChanged(
          Next(),
          [KeyMapFixtures.EnglishUs.Layout, KeyMapFixtures.Russian.Layout],
          [KeyMapFixtures.EnglishUs, KeyMapFixtures.Russian]));
      Send(new LayoutChanged(Next(), KeyMapFixtures.EnglishUs.Layout));
    }

    private Typist(Engine engine) {
      Engine = engine;
      Focus();
    }

    public Engine Engine { get; }

    // The current Word as it reads in English (US).
    public string Word => KeyMapFixtures.EnglishUs.Render(Engine.Word)
        ?? throw new InvalidOperationException("The Word does not render in English (US).");

    public List<EngineAction> Actions { get; } = [];

    public Typist Type(string text) {
      IReadOnlyList<Keystroke> keystrokes = KeyMapFixtures.EnglishUs.Type(text, capsLock: false)
          ?? throw new ArgumentException("English (US) cannot type the text.", nameof(text));
      foreach (Keystroke keystroke in keystrokes) {
        KeyModifiers modifiers = keystroke.Shift ? KeyModifiers.Shift : KeyModifiers.None;
        Send(new KeyDown(Next(), keystroke, KeyKind.Character, modifiers));
      }

      return this;
    }

    public Typist Press(KeyKind kind, KeyModifiers modifiers = KeyModifiers.None) {
      Send(new KeyDown(Next(), new Keystroke(0x01, false, false), kind, modifiers));
      return this;
    }

    // A Trigger Key the hook held back, as it does after a character key.
    public Typist Hold(KeyKind kind, KeyModifiers modifiers = KeyModifiers.None) {
      Send(new KeyDown(Next(), new Keystroke(0x39, false, false), kind, modifiers, Held: true));
      return this;
    }

    // The host's report on the latest ReplaceText.
    public Typist Finish(bool applied = true) {
      Send(new ReplacementFinished(Next(), applied));
      return this;
    }

    // A Ctrl+Z the hook held back as a possible Undo.
    public Typist RequestUndo() {
      Send(new UndoRequested(Next()));
      return this;
    }

    // Ctrl+C and the like: a character key with a modifier held.
    public Typist Chord(KeyModifiers modifiers, char letter) {
      Keystroke keystroke = KeyMapFixtures.EnglishUs.Type(letter.ToString(), false)![0];
      Send(new KeyDown(Next(), keystroke, KeyKind.Character, modifiers));
      return this;
    }

    public Typist Click() {
      Send(new MouseClick(Next()));
      return this;
    }

    public Typist Focus(bool isPassword = false, bool isElevated = false) {
      Send(new FocusChanged(Next(), App, isPassword, isElevated));
      return this;
    }

    public Typist Wait(TimeSpan pause) {
      _now += pause - _step;
      return this;
    }

    public Typist Send(InputEvent inputEvent) {
      Actions.AddRange(Engine.Handle(inputEvent));
      return this;
    }

    private TimeSpan Next() {
      _now += _step;
      return _now;
    }
  }
}
