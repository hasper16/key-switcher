using Rekey.Core;
using Rekey.Win32.Keyboard;
using Xunit;

namespace Rekey.Win32.Tests.Keyboard {
  // The worker reads a key, then asks ModifierSync to follow hook reinstalls before translating.
  public sealed class ModifierSyncTests {
    private const ushort _vkWindowsLeft = 0x5B;

    private readonly RawInputTranslator _translator = new(capsLockOn: false);
    private long _reattachments;
    private long _written;
    private bool _windowsDown;

    [Fact]
    public void ReinstallFinishedJustBeforeAReadIsFollowedBeforeTheKey() {
      ModifierSync sync = NewSync();
      Translate(Key(_vkWindowsLeft, 0x5B, extended: true));
      sync.FollowReattachments();

      // The hook reinstalls after that check; the Win key-up was lost while it was gone.
      _windowsDown = false;
      _reattachments++;
      RawInput letter = Key(0x47, 0x22);
      sync.FollowReattachments();

      Assert.Equal(KeyModifiers.None, Translate(letter).Modifiers);
    }

    [Fact]
    public void NoReinstallKeepsTheTrackedModifiers() {
      ModifierSync sync = NewSync();
      Translate(Key(_vkWindowsLeft, 0x5B, extended: true));
      _windowsDown = false;

      RawInput letter = Key(0x47, 0x22);
      sync.FollowReattachments();

      Assert.Equal(KeyModifiers.Windows, Translate(letter).Modifiers);
    }

    private ModifierSync NewSync() {
      return new ModifierSync(
          _translator,
          () => _reattachments,
          () => _written,
          key => _windowsDown && (ushort)key == _vkWindowsLeft,
          () => false);
    }

    // A key the hook wrote to the ring; Win counts as down once its key-down is written.
    private RawInput Key(ushort virtualKey, ushort scanCode, bool extended = false) {
      long sequence = _written++;
      _windowsDown |= virtualKey == _vkWindowsLeft;
      return new RawInput(
          RawInputKind.KeyDown, virtualKey, scanCode, extended, 1000 + sequence, sequence);
    }

    private KeyDown Translate(RawInput input) {
      return Assert.IsType<KeyDown>(_translator.Translate(input));
    }
  }
}
