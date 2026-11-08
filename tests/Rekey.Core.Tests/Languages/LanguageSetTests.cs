using Rekey.Core.Languages;
using Xunit;
using static Rekey.Core.Tests.Languages.TestProfiles;

namespace Rekey.Core.Tests.Languages {
  public sealed class LanguageSetTests {
    [Fact]
    public void OnlyLanguagesWithAnInstalledLayoutAreActive() {
      IReadOnlyList<LanguageProfile> active =
          ThreeLanguages().Activate([EnglishUs, Russian]);

      Assert.Equal([Language.English, Language.Russian], active.Select(p => p.Language));
    }

    [Fact]
    public void AddedLayoutActivatesItsLanguageOnTheNextActivation() {
      LanguageSet languages = ThreeLanguages();
      languages.Activate([EnglishUs, Russian]);

      IReadOnlyList<LanguageProfile> active =
          languages.Activate([EnglishUs, Russian, UkrainianEnhanced]);

      Assert.Contains(active, p => p.Language == Language.Ukrainian);
    }

    [Fact]
    public void UnlistedVariantMapsByLanguageId() {
      LanguageProfile? profile = ThreeLanguages().ProfileFor(EnglishInternational);

      Assert.Equal(Language.English, profile?.Language);
    }

    [Fact]
    public void UnsupportedLayoutMapsToNone() {
      LanguageSet languages = ThreeLanguages();

      Assert.Null(languages.ProfileFor(German));
      Assert.Empty(languages.Activate([German]));
    }

    [Fact]
    public void ListedLayoutWinsOverLanguageIdFallback() {
      // A profile listing a variant claims it even when another profile shares its language id.
      var languages = new LanguageSet([
          Load(Json("en", "00000409")),
          Load(Json("ru", "00020409"))]);

      Assert.Equal(Language.Russian, languages.ProfileFor(EnglishInternational)?.Language);
    }
  }
}
