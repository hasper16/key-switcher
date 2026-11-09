using Rekey.Core.Detection;
using Rekey.Core.Tests.Detection;

namespace Rekey.Core.Tests {
  // Feeds an Engine the events of a user typing in English (US), 100 ms apart unless told
  // otherwise.
  internal sealed class Typist {
    private static readonly TimeSpan _step = TimeSpan.FromMilliseconds(100);

    private TimeSpan _now = TimeSpan.FromSeconds(10);

    public Engine Engine { get; } = new();

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
      Send(new FocusChanged(Next(), @"C:\Apps\Editor.exe", isPassword, isElevated));
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
