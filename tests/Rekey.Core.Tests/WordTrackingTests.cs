using Xunit;

namespace Rekey.Core.Tests {
  // Spec keyboard-capture: "Keys that build a Word" and "Keys and events that end a Word".
  public sealed class WordTrackingTests {
    [Fact]
    public void LettersBuildTheWord() {
      Assert.Equal("hello", new Typist().Type("hello").Word);
    }

    [Fact]
    public void BackspaceRemovesTheLastKeystroke() {
      Typist typist = new Typist().Type("ghbdtnn").Press(KeyKind.Backspace);

      Assert.Equal("ghbdtn", typist.Word);
    }

    [Fact]
    public void BackspaceOnAnEmptyWordKeepsItEmpty() {
      Typist typist = new Typist().Press(KeyKind.Backspace).Type("ab");

      Assert.Equal("ab", typist.Word);
    }

    [Fact]
    public void PunctuationStaysInsideTheWord() {
      Assert.Equal("ghbdtn!", new Typist().Type("ghbdtn!").Word);
    }

    [Theory]
    [InlineData(KeyKind.Space)]
    [InlineData(KeyKind.Enter)]
    [InlineData(KeyKind.Tab)]
    [InlineData(KeyKind.Navigation)]
    public void BoundaryKeyStartsANewWord(KeyKind boundary) {
      Typist typist = new Typist().Type("ghb").Press(boundary).Type("dtn");

      Assert.Equal("dtn", typist.Word);
    }

    [Theory]
    [InlineData(KeyModifiers.Control)]
    [InlineData(KeyModifiers.Alt)]
    [InlineData(KeyModifiers.Windows)]
    [InlineData(KeyModifiers.Control | KeyModifiers.Shift)]
    public void ShortcutEndsTheWord(KeyModifiers modifiers) {
      Typist typist = new Typist().Type("ghb").Chord(modifiers, 'a');

      Assert.Empty(typist.Engine.Word);
    }

    [Fact]
    public void CtrlBackspaceEndsTheWordRatherThanShorteningIt() {
      Typist typist = new Typist().Type("ghb").Press(KeyKind.Backspace, KeyModifiers.Control);

      Assert.Empty(typist.Engine.Word);
    }

    [Fact]
    public void ModifierPressedAloneKeepsTheWord() {
      Typist typist = new Typist()
          .Type("gh")
          .Press(KeyKind.Modifier, KeyModifiers.Shift)
          .Press(KeyKind.Modifier, KeyModifiers.Control)
          .Type("B");

      Assert.Equal("ghB", typist.Word);
    }

    [Fact]
    public void OtherKeyAloneKeepsTheWord() {
      Typist typist = new Typist().Type("gh").Press(KeyKind.Other).Type("b");

      Assert.Equal("ghb", typist.Word);
    }

    [Fact]
    public void OtherKeyWithCtrlEndsTheWord() {
      Typist typist = new Typist().Type("gh").Press(KeyKind.Other, KeyModifiers.Control);

      Assert.Empty(typist.Engine.Word);
    }

    [Fact]
    public void MouseClickEndsTheWord() {
      Assert.Empty(new Typist().Type("ghb").Click().Engine.Word);
    }

    [Fact]
    public void FocusChangeEndsTheWord() {
      Assert.Equal("dtn", new Typist().Type("ghb").Focus().Type("dtn").Word);
    }
  }
}
