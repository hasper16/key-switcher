using System.Text;
using Rekey.Core.Detection;
using Rekey.Core.Languages;
using Xunit;

namespace Rekey.Core.Tests.Detection {
  public sealed class KeyMapJsonTests {
    private const string _fixture = """
        {
          "layoutId": "00000419",
          "keys": [
            {
              "scanCode": 16,
              "normal": "й",
              "shifted": "Й",
              "capsLock": true,
              "deadNormal": false,
              "deadShifted": false
            },
            {
              "scanCode": 41,
              "normal": "^",
              "shifted": "~",
              "capsLock": false,
              "deadNormal": true,
              "deadShifted": false
            }
          ]
        }

        """;

    [Fact]
    public void FixtureRoundTripsUnchanged() {
      KeyMap map = Read(_fixture.ReplaceLineEndings("\n"));

      Assert.Equal(_fixture.ReplaceLineEndings("\n"), KeyMapJson.Write(map));
    }

    [Fact]
    public void ReadsLayoutAndKeys() {
      KeyMap map = Read(_fixture);

      Assert.Equal(new LayoutId(0x00000419), map.Layout);
      Assert.Equal(
          new KeyMapEntry(16, "й", "Й", CapsLock: true, DeadNormal: false, DeadShifted: false),
          map.Entries[0]);
    }

    [Fact]
    public void RejectsAnInvalidLayoutId() {
      Assert.Throws<InvalidDataException>(() => Read(_fixture.Replace("00000419", "419")));
    }

    [Fact]
    public void RejectsARepeatedScanCode() {
      string repeated = _fixture.Replace("\"scanCode\": 41", "\"scanCode\": 16");

      Assert.Throws<InvalidDataException>(() => Read(repeated));
    }

    [Fact]
    public void RejectsMalformedJson() {
      Assert.Throws<InvalidDataException>(() => Read("{"));
    }

    private static KeyMap Read(string json) {
      using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
      return KeyMapJson.Read(stream);
    }
  }
}
