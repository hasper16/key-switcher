using Rekey.Win32.Keyboard;
using Xunit;

namespace Rekey.Win32.Tests.Keyboard {
  // Spec keyboard-capture: "Each Keystroke is confirmed outside a Password Field".
  public sealed class KeyConfirmerTests {
    private long _written;
    private bool _passwordFocused;
    private int _checks;
    private Action _duringCheck = () => { };

    [Fact]
    public void KeyInAnOrdinaryControlWithNothingAfterItIsConfirmed() {
      RawInput key = Write();

      Assert.True(NewConfirmer().Confirm(key));
    }

    [Fact]
    public void KeyInAPasswordFieldIsNotConfirmed() {
      RawInput key = Write();
      _passwordFocused = true;

      Assert.False(NewConfirmer().Confirm(key));
    }

    [Fact]
    public void KeyWithLaterInputAlreadyQueuedIsNotConfirmed() {
      // A Tab or click after the key may already have moved focus.
      RawInput key = Write();
      Write();

      Assert.False(NewConfirmer().Confirm(key));
      Assert.Equal(0, _checks);
    }

    [Fact]
    public void KeyWithInputArrivingDuringTheCheckIsNotConfirmed() {
      // Focus may have moved before the check read it.
      RawInput key = Write();
      _duringCheck = () => Write();

      Assert.False(NewConfirmer().Confirm(key));
    }

    private KeyConfirmer NewConfirmer() {
      return new KeyConfirmer(
          () => {
            _checks++;
            _duringCheck();
            return _passwordFocused;
          },
          () => _written);
    }

    private RawInput Write() {
      long sequence = _written++;
      return new RawInput(RawInputKind.KeyDown, 0x47, 0x22, false, 1000 + sequence, sequence);
    }
  }
}
