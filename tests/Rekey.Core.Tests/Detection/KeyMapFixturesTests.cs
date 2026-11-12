using Rekey.Core.Detection;
using Xunit;

namespace Rekey.Core.Tests.Detection {
  public sealed class KeyMapFixturesTests {
    public static TheoryData<string> Klids => [.. KeyMapFixtures.Klids];

    [Theory]
    [MemberData(nameof(Klids))]
    public void FixtureHasEveryPrintableKeyInBothShiftStates(string klid) {
      KeyMap map = KeyMapFixtures.Load(klid);

      Assert.Equal(KeyMapFixtures.Id(klid), map.Layout);
      Assert.Equal(48, map.Entries.Count);
      Assert.All(map.Entries, entry => {
        Assert.NotEmpty(entry.Normal);
        Assert.NotEmpty(entry.Shifted);
      });
    }

    [Theory]
    [MemberData(nameof(Klids))]
    public void FixtureRoundTripsUnchanged(string klid) {
      string text = File.ReadAllText(KeyMapFixtures.PathOf(klid));

      Assert.Equal(text, KeyMapJson.Write(KeyMapFixtures.Load(klid)));
    }
  }
}
