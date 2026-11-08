using Rekey.Core.Languages;
using Xunit;
using static Rekey.Core.Tests.Languages.TestProfiles;

namespace Rekey.Core.Tests.Languages {
  public sealed class LanguageSetLoadTests {
    private static readonly DateTimeOffset _now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);

    public static TheoryData<string, Func<Stream>> BadUkrainianSources => new() {
      { "corrupt JSON", () => Stream("{ \"format\": 1, ") },
      { "wrong code", () => Stream(Json("ru", "00000422")) },
      { "unreadable", () => throw new IOException("The resource is missing.") },
      { "access denied", () => throw new UnauthorizedAccessException("No access.") },
    };

    [Theory]
    [MemberData(nameof(BadUkrainianSources))]
    public void BadProfileIsSkippedWithAnIncident(string reason, Func<Stream> ukrainian) {
      Assert.NotEmpty(reason);

      LanguageSetLoad load = LanguageSet.Load(
          [
              new(Language.English, () => Stream(Json("en", "00000409"))),
              new(Language.Russian, () => Stream(Json("ru", "00000419"))),
              new(Language.Ukrainian, ukrainian),
          ],
          _now);

      IReadOnlyList<LanguageProfile> active =
          load.Languages.Activate([EnglishUs, Russian, Ukrainian]);
      Assert.Equal([Language.English, Language.Russian], active.Select(p => p.Language));
      Incident incident = Assert.Single(load.Incidents);
      Assert.Equal(IncidentType.LanguageProfileSkipped, incident.Type);
      Assert.Equal(_now, incident.Time);
      Assert.Equal([Language.Ukrainian], incident.Details);
    }

    [Fact]
    public void GoodProfilesLoadWithoutIncidents() {
      LanguageSetLoad load = LanguageSet.Load(
          [new(Language.English, () => Stream(Json("en", "00000409")))], _now);

      Assert.Single(load.Languages.Profiles);
      Assert.Empty(load.Incidents);
    }
  }
}
