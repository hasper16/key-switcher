using Rekey.DesktopChecks;
using Xunit;

namespace Rekey.DesktopChecks.Tests {
  // The desktop checks restore the user's language list from this snapshot, so it must read and
  // put it back exactly.
  public sealed class LanguageListTests {
    // A list like the one that broke the first desktop run: one language with two keyboards.
    private const string _output =
        "en-US|0409:00000409|True|False\r\nuk|0422:00020422,0422:00000419|True|False\r\n";

    [Fact]
    public void ReadsEachLanguageWithItsSettings() {
      LanguageList list = LanguageList.Parse(_output);

      Assert.Equal(["en-US", "uk"], list.Languages.Select(language => language.Tag));
      Assert.Equal(["0422:00020422", "0422:00000419"], list.Languages[1].Tips);
      Assert.True(list.Languages[0].Spellchecking);
      Assert.False(list.Languages[0].Handwriting);
    }

    [Theory]
    [InlineData("en-US\r\nuk\r\n|\r\n0409:00000409,0422:00020422\r\n")]
    [InlineData("")]
    [InlineData("|0409:00000409|True|False")]
    [InlineData("en-US|0409:00000409")]
    [InlineData("en-US|0409:00000409|maybe|False")]
    public void RefusesOutputThatIsNotOneLanguagePerLine(string output) {
      Assert.Throws<FormatException>(() => LanguageList.Parse(output));
    }

    [Fact]
    public void LanguagesAddedSinceTheSnapshotAreFound() {
      LanguageList snapshot = LanguageList.Parse(_output);
      LanguageList now = LanguageList.Parse(_output + "de-DE|0407:00000407|True|False");

      Assert.Equal(["de-DE"], snapshot.ExtraIn(now));
    }

    [Fact]
    public void RemoveScriptKeepsTheOtherLanguageObjects() {
      string script = LanguageList.RemoveScript(["de-DE"]);

      Assert.Equal(
          "$l = Get-WinUserLanguageList; $remove = @('de-DE'); "
              + "for ($i = $l.Count - 1; $i -ge 0; $i--) { "
              + "if ($remove -contains $l[$i].LanguageTag) { $l.RemoveAt($i) } }; "
              + "Set-WinUserLanguageList $l -Force",
          script);
    }

    [Fact]
    public void SnapshotTextParsesBackToTheSameList() {
      LanguageList list = LanguageList.Parse(_output);

      Assert.True(LanguageList.Parse(list.ToText()).SameAs(list));
    }

    [Fact]
    public void ChangedSettingIsNotTheSame() {
      LanguageList list = LanguageList.Parse(_output);
      LanguageList changed = LanguageList.Parse(_output.Replace(
          "en-US|0409:00000409|True", "en-US|0409:00000409|False", StringComparison.Ordinal));

      Assert.False(changed.SameAs(list));
    }

    [Fact]
    public void QuotesInValuesAreEscaped() {
      Assert.Contains(
          "'x''y'", LanguageList.RemoveScript(["x'y"]), StringComparison.Ordinal);
    }
  }
}
