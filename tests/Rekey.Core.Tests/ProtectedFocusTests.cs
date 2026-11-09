using Rekey.Core.Detection;
using Rekey.Core.Languages;
using Rekey.Core.Tests.Detection;
using Xunit;

namespace Rekey.Core.Tests {
  // Spec keyboard-capture: "Password Fields are never recorded" and "Elevated windows". A
  // Correction is built from the current Word, so no Word kept means no Correction.
  public sealed class ProtectedFocusTests {
    private static readonly LayoutId _russian = KeyMapFixtures.Russian.Layout;

    [Fact]
    public void PasswordFieldKeepsNoKeystrokes() {
      Typist typist = new Typist().Focus(isPassword: true).Type("ghbdtn");

      Assert.Empty(typist.Engine.Word);
      Assert.Empty(typist.Actions);
    }

    [Fact]
    public void TypingResumesAfterLeavingThePasswordField() {
      Typist typist = new Typist().Focus(isPassword: true).Type("qwe").Focus().Type("dtn");

      Assert.Equal("dtn", typist.Word);
    }

    [Fact]
    public void ElevatedWindowKeepsNoKeystrokes() {
      Typist typist = new Typist().Focus(isElevated: true).Type("ghbdtn");

      Assert.Empty(typist.Engine.Word);
      Assert.Empty(typist.Actions);
    }

    [Fact]
    public void EngineReportsTheFocusedControl() {
      Typist typist = new Typist().Focus(isPassword: true);

      Assert.Equal(new Context(@"C:\Apps\Editor.exe", true, false), typist.Engine.Focus);
    }

    [Fact]
    public void FocusChangeDropsDeferredWords() {
      var typist = new Typist();
      typist.Engine.DeferredWords.Add(Decision.Defer(_russian, "ыф"));

      typist.Focus();

      Correction? next = typist.Engine.DeferredWords.Add(Decision.Correct(_russian, "вапро"));
      Assert.Equal(["вапро"], next?.Texts);
    }
  }
}
