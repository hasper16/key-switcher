using Rekey.DesktopChecks;
using Xunit;

namespace Rekey.DesktopChecks.Tests {
  // The snapshot remembers which language the run added, so a restore removes only that one,
  // never a language the user added later.
  public sealed class LanguageSnapshotTests {
    private const string _list = "en-US|0409:00000409|True|False\nuk|0422:00020422|True|False";

    [Fact]
    public void SnapshotFileKeepsTheListAndTheAddedLanguage() {
      var snapshot = new LanguageSnapshot(LanguageList.Parse(_list), "de-DE");

      LanguageSnapshot read = LanguageSnapshot.Parse(snapshot.ToText());

      Assert.Equal("de-DE", read.Added);
      Assert.True(read.List.SameAs(snapshot.List));
    }

    [Fact]
    public void FileWithoutTheAddedLanguageIsRefused() {
      Assert.Throws<FormatException>(() => LanguageSnapshot.Parse(_list));
    }

    [Fact]
    public void OnlyTheAddedLanguageIsRemoved() {
      var snapshot = new LanguageSnapshot(LanguageList.Parse(_list), "de-DE");
      LanguageList now = LanguageList.Parse(
          _list + "\nde-DE|0407:00000407|True|False\nfr-FR|040C:0000040C|True|False");

      Assert.Equal(["de-DE"], snapshot.ToRemoveFrom(now));
    }

    [Fact]
    public void NothingIsRemovedWhenTheAddedLanguageIsGone() {
      var snapshot = new LanguageSnapshot(LanguageList.Parse(_list), "de-DE");
      LanguageList now = LanguageList.Parse(_list + "\nfr-FR|040C:0000040C|True|False");

      Assert.Empty(snapshot.ToRemoveFrom(now));
    }
  }
}
