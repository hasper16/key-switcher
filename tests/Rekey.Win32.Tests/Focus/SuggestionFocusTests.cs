using Rekey.Win32.Focus;
using Xunit;

namespace Rekey.Win32.Tests.Focus {
  // Design D7: focus moving from a text control to an element that takes no text, such as a
  // row of an autocomplete list, keeps the Word; menus, buttons, Password Fields and other text
  // controls do not.
  public sealed class SuggestionFocusTests {
    private const int _edit = 50004;
    private const int _document = 50030;
    private const int _group = 50026;
    private const int _listItem = 50007;
    private const int _button = 50000;
    private const int _comboBox = 50003;
    private const int _menu = 50009;
    private const int _menuBar = 50010;
    private const int _menuItem = 50011;
    private const int _splitButton = 50031;

    [Theory]
    [InlineData(_edit, _group)]
    [InlineData(_document, _listItem)]
    public void ListRowAfterATextControlKeepsTheWord(int from, int to) {
      Assert.True(SuggestionFocus.KeepsWord(from, to, toIsPassword: false));
    }

    [Theory]
    [InlineData(_edit)]
    [InlineData(_document)]
    [InlineData(_comboBox)]
    [InlineData(_button)]
    [InlineData(_splitButton)]
    [InlineData(_menu)]
    [InlineData(_menuBar)]
    [InlineData(_menuItem)]
    public void TextControlsMenusAndButtonsAreMoves(int to) {
      Assert.False(SuggestionFocus.KeepsWord(_edit, to, toIsPassword: false));
    }

    [Fact]
    public void PasswordFieldIsAMove() {
      Assert.False(SuggestionFocus.KeepsWord(_edit, _group, toIsPassword: true));
    }

    [Fact]
    public void MoveFromAnElementThatTakesNoTextIsAMove() {
      Assert.False(SuggestionFocus.KeepsWord(_group, _listItem, toIsPassword: false));
    }
  }
}
