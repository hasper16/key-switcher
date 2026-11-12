using Rekey.Core;
using Rekey.Win32.Focus;
using Xunit;

namespace Rekey.Win32.Tests.Focus {
  // Design D7: a focus move keeps the Word only when the keys that could have caused it were
  // typing; a navigation key, a shortcut or a click before it, or input still waiting, decides
  // otherwise.
  public sealed class TypingStateTests {
    private readonly TypingState _state = new();

    [Fact]
    public void NothingReadIsNotTyping() {
      Assert.Equal(Typing.No, _state.Now(actionsWritten: 0));
    }

    [Fact]
    public void CharacterKeyIsTyping() {
      _state.KeyRead(KeyKind.Character, KeyModifiers.Shift, actions: 1);

      Assert.Equal(Typing.Yes, _state.Now(actionsWritten: 1));
    }

    [Fact]
    public void BackspaceIsTyping() {
      _state.KeyRead(KeyKind.Backspace, KeyModifiers.None, actions: 1);

      Assert.Equal(Typing.Yes, _state.Now(actionsWritten: 1));
    }

    [Theory]
    [InlineData(KeyKind.Tab, KeyModifiers.None)]
    [InlineData(KeyKind.Navigation, KeyModifiers.None)]
    [InlineData(KeyKind.Enter, KeyModifiers.None)]
    [InlineData(KeyKind.Other, KeyModifiers.None)]
    [InlineData(KeyKind.Character, KeyModifiers.Control)]
    [InlineData(KeyKind.Character, KeyModifiers.Alt)]
    [InlineData(KeyKind.Character, KeyModifiers.Windows)]
    public void OtherKeysAreNotTyping(KeyKind kind, KeyModifiers modifiers) {
      _state.KeyRead(KeyKind.Character, KeyModifiers.None, actions: 1);

      _state.KeyRead(kind, modifiers, actions: 2);

      Assert.Equal(Typing.No, _state.Now(actionsWritten: 2));
    }

    [Fact]
    public void ModifierAloneChangesNothing() {
      _state.KeyRead(KeyKind.Character, KeyModifiers.None, actions: 1);

      _state.KeyRead(KeyKind.Modifier, KeyModifiers.Shift, actions: 2);

      Assert.Equal(Typing.Yes, _state.Now(actionsWritten: 2));
    }

    [Fact]
    public void ClickIsNotTyping() {
      _state.KeyRead(KeyKind.Character, KeyModifiers.None, actions: 1);

      _state.ClickRead(actions: 2);

      Assert.Equal(Typing.No, _state.Now(actionsWritten: 2));
    }

    [Fact]
    public void InputStillWaitingLeavesTheMoveUndecided() {
      _state.KeyRead(KeyKind.Character, KeyModifiers.None, actions: 1);

      Assert.Equal(Typing.Waiting, _state.Now(actionsWritten: 2));
    }

    [Fact]
    public void DeliveredCharacterKeyAfterTypingIsTyping() {
      _state.KeyRead(KeyKind.Character, KeyModifiers.None, actions: 1);

      Typing typing = _state.ForKey(
          KeyKind.Character, KeyModifiers.None, delivered: true, actions: 2, actionsWritten: 2);

      Assert.Equal(Typing.Yes, typing);
    }

    [Fact]
    public void DeliveredTabCountsAsTheCause() {
      _state.KeyRead(KeyKind.Character, KeyModifiers.None, actions: 1);

      Typing typing = _state.ForKey(
          KeyKind.Tab, KeyModifiers.None, delivered: true, actions: 2, actionsWritten: 2);

      Assert.Equal(Typing.No, typing);
    }

    [Fact]
    public void HeldKeyCountsOnlyTheKeysBeforeIt() {
      _state.KeyRead(KeyKind.Character, KeyModifiers.None, actions: 1);

      Typing typing = _state.ForKey(
          KeyKind.Space, KeyModifiers.None, delivered: false, actions: 2, actionsWritten: 2);

      Assert.Equal(Typing.Yes, typing);
    }

    [Fact]
    public void HeldKeyAfterANavigationKeyIsNotTyping() {
      _state.KeyRead(KeyKind.Navigation, KeyModifiers.None, actions: 1);

      Typing typing = _state.ForKey(
          KeyKind.Space, KeyModifiers.None, delivered: false, actions: 2, actionsWritten: 2);

      Assert.Equal(Typing.No, typing);
    }

    [Fact]
    public void KeyWithLaterInputWaitingLeavesTheMoveUndecided() {
      _state.KeyRead(KeyKind.Character, KeyModifiers.None, actions: 1);

      Typing typing = _state.ForKey(
          KeyKind.Character, KeyModifiers.None, delivered: true, actions: 2, actionsWritten: 3);

      Assert.Equal(Typing.Waiting, typing);
    }
  }
}
