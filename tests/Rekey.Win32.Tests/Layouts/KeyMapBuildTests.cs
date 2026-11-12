using Rekey.Core.Detection;
using Rekey.Core.Languages;
using Xunit;

namespace Rekey.Win32.Tests.Layouts {
  // Task 4.2: Key Maps built from this Windows match the fixtures captured in add-language-scorer.
  public sealed class KeyMapBuildTests {
    public static TheoryData<string> Klids { get; } = [.. KeyMapFixtures.Klids];

    [Theory]
    [MemberData(nameof(Klids))]
    public void BuiltMapMatchesTheFixture(string klid) {
      Assert.True(LayoutId.TryParse(klid, out LayoutId layout));

      KeyMap built = KeyMapReader.Read(layout);

      Assert.Equal(KeyMapJson.Write(KeyMapFixtures.Load(klid)), KeyMapJson.Write(built));
    }

    [Fact]
    public void RussianKeyThatTypesFInEnglishTypesA() {
      // Spec layout-management: "Russian map".
      KeyMap russian = KeyMapReader.Read(new LayoutId(0x00000419));

      KeyMapEntry key = russian.Entries.Single(entry => entry.ScanCode == 0x21);
      Assert.Equal(("а", "А"), (key.Normal, key.Shifted));
    }

    [Fact]
    public void IsoKeyOfUnitedKingdomTypesBackslashAndBar() {
      // Spec layout-management: "ISO key".
      KeyMap unitedKingdom = KeyMapReader.Read(new LayoutId(0x00000809));

      KeyMapEntry key = unitedKingdom.Entries.Single(entry => entry.ScanCode == 0x56);
      Assert.Equal(("\\", "|"), (key.Normal, key.Shifted));
    }
  }
}
