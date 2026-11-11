using Rekey.DesktopChecks;
using Xunit;

namespace Rekey.DesktopChecks.Tests {
  // The desktop checks restore the user's language list from this snapshot, so it must read and
  // write it back exactly.
  public sealed class LanguageListTests {
    // A list like the one that broke the first desktop run: one language with two keyboards.
    private const string _output = "en-US|0409:00000409\r\nuk|0422:00020422,0422:00000419\r\n";

    [Fact]
    public void ReadsEachLanguageWithItsInputMethods() {
      LanguageList list = LanguageList.Parse(_output);

      Assert.Equal(["en-US", "uk"], list.Languages.Select(language => language.Tag));
      Assert.Equal(["0422:00020422", "0422:00000419"], list.Languages[1].Tips);
    }

    [Theory]
    [InlineData("en-US\r\nuk\r\n|\r\n0409:00000409,0422:00020422\r\n")]
    [InlineData("")]
    [InlineData("|0409:00000409")]
    public void RefusesOutputThatIsNotOneLanguagePerLine(string output) {
      Assert.Throws<FormatException>(() => LanguageList.Parse(output));
    }

    [Fact]
    public void WriteScriptRebuildsEveryLanguageAndInputMethodInOrder() {
      string script = LanguageList.Parse(_output).WriteScript();

      Assert.Equal(
          "$l = New-WinUserLanguageList 'en-US'; $l[0].InputMethodTips.Clear(); "
              + "$l[0].InputMethodTips.Add('0409:00000409'); "
              + "$l.Add('uk'); $l[1].InputMethodTips.Clear(); "
              + "$l[1].InputMethodTips.Add('0422:00020422'); "
              + "$l[1].InputMethodTips.Add('0422:00000419'); "
              + "Set-WinUserLanguageList $l -Force",
          script);
    }

    [Fact]
    public void SnapshotTextParsesBackToTheSameList() {
      LanguageList list = LanguageList.Parse(_output);

      Assert.True(LanguageList.Parse(list.ToText()).SameAs(list));
    }

    [Fact]
    public void ListWithAnAddedLanguageIsNotTheSame() {
      LanguageList list = LanguageList.Parse(_output);

      Assert.False(LanguageList.Parse(_output + "de-DE|0407:00000407").SameAs(list));
    }

    [Fact]
    public void QuotesInValuesAreEscaped() {
      string script = LanguageList.Parse("x'y|0409:00000409").WriteScript();

      Assert.Contains("'x''y'", script, StringComparison.Ordinal);
    }
  }
}
