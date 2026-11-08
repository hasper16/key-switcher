using Rekey.Core.Languages;
using Xunit;
using static Rekey.Core.Tests.Languages.TestProfiles;

namespace Rekey.Core.Tests.Languages {
  // Spec examples of language-profiles; these real words come from the spec, not from users.
  public sealed class BundledProfilesTests {
    private static readonly LanguageSetLoad _load =
        LanguageSet.LoadBundled(DateTimeOffset.UnixEpoch);

    [Fact]
    public void AllThreeLoadWithoutIncidents() {
      Assert.Empty(_load.Incidents);
      Assert.Equal(
          [Language.English, Language.Russian, Language.Ukrainian],
          _load.Languages.Profiles.Select(p => p.Language));
    }

    [Fact]
    public void RussianHasItsShortWordsAndFoldsYo() {
      LanguageProfile russian = Profile(Language.Russian);

      Assert.True(russian.ContainsShortWord("я"));
      Assert.True(russian.ContainsShortWord("не"));
      Assert.True(russian.ContainsCommonWord("ЕЩЁ"));
    }

    [Fact]
    public void UkrainianMatchesTypographicApostropheAndListsBothLayouts() {
      LanguageProfile ukrainian = Profile(Language.Ukrainian);

      Assert.True(ukrainian.ContainsCommonWord("п’ять"));
      Assert.True(ukrainian.LayoutIds.IsSupersetOf([Ukrainian, UkrainianEnhanced]));
    }

    [Fact]
    public void UkrainianIsInactiveWithoutAUkrainianLayout() {
      IReadOnlyList<LanguageProfile> active = _load.Languages.Activate([EnglishUs, Russian]);

      Assert.Equal([Language.English, Language.Russian], active.Select(p => p.Language));
    }

    private static LanguageProfile Profile(Language language) {
      return _load.Languages.Profiles.Single(p => p.Language == language);
    }
  }
}
