using Rekey.Win32.Keyboard;
using Xunit;

namespace Rekey.Win32.Tests.Keyboard {
  // Spec keyboard-capture: "Each Keystroke is confirmed outside a Password Field".
  public sealed class KeyConfirmerTests {
    private readonly InputRing _ring = new(capacity: 16);
    private bool _passwordFocused;
    private int _checks;
    private Action _duringCheck = () => { };

    [Fact]
    public void KeyInAnOrdinaryControlWithNothingAfterItIsConfirmed() {
      RawInput key = Read(Write(RawInputKind.KeyDown));

      Assert.True(NewConfirmer().Confirm(key));
    }

    [Fact]
    public void KeysOwnKeyUpDoesNotStopTheConfirmation() {
      // Injected keys arrive down and up together; a key-up cannot move focus.
      RawInput key = Read(Write(RawInputKind.KeyDown));
      Write(RawInputKind.KeyUp);

      Assert.True(NewConfirmer().Confirm(key));
    }

    [Fact]
    public void KeyInAPasswordFieldIsNotConfirmed() {
      RawInput key = Read(Write(RawInputKind.KeyDown));
      _passwordFocused = true;

      Assert.False(NewConfirmer().Confirm(key));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void KeyWithALaterKeyOrClickQueuedIsNotConfirmed(bool click) {
      // A Tab or click after the key may already have moved focus.
      RawInput key = Read(Write(RawInputKind.KeyDown));
      Write(click ? RawInputKind.MouseDown : RawInputKind.KeyDown);

      Assert.False(NewConfirmer().Confirm(key));
      Assert.Equal(0, _checks);
    }

    [Fact]
    public void KeyWithAKeyArrivingDuringTheCheckIsNotConfirmed() {
      // Focus may have moved before the check read it.
      RawInput key = Read(Write(RawInputKind.KeyDown));
      _duringCheck = () => Write(RawInputKind.KeyDown);

      Assert.False(NewConfirmer().Confirm(key));
    }

    [Fact]
    public void RejectionsAreCountedByCause() {
      KeyConfirmer confirmer = NewConfirmer();
      RawInput queued = Read(Write(RawInputKind.KeyDown));
      Write(RawInputKind.KeyDown);
      confirmer.Confirm(queued);
      RawInput during = Read(RawInputKind.KeyDown);
      _duringCheck = () => Write(RawInputKind.KeyDown);
      confirmer.Confirm(during);
      _duringCheck = () => { };
      RawInput password = Read(RawInputKind.KeyDown);
      _passwordFocused = true;
      confirmer.Confirm(password);

      Assert.Equal(new KeyRejects(Queued: 1, DuringCheck: 1, Password: 1), confirmer.Rejects);
    }

    private KeyConfirmer NewConfirmer() {
      return new KeyConfirmer(
          () => {
            _checks++;
            _duringCheck();
            return _passwordFocused;
          },
          () => _ring.ActionsWritten);
    }

    private RawInputKind Write(RawInputKind kind) {
      _ring.TryWrite(new RawInput(kind, 0x47, 0x22, false, 1000));
      return kind;
    }

    private RawInput Read(RawInputKind expected) {
      Assert.True(_ring.TryRead(out RawInput input));
      Assert.Equal(expected, input.Kind);
      return input;
    }
  }
}
