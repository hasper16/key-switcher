using Rekey.Core.Detection;
using Rekey.Core.Languages;
using Xunit;

namespace Rekey.Win32.Tests {
  public sealed class KeyMapReaderTests {
    private static readonly LayoutId _englishUs = new(0x00000409);

    [Fact]
    public void ReadsAllPrintableKeysOfEnglishUs() {
      KeyMap map = KeyMapReader.Read(_englishUs);

      Assert.Equal(_englishUs, map.Layout);
      Assert.Equal(47, map.Entries.Count);
    }

    [Fact]
    public void LettersFollowCapsLockAndDigitsDoNot() {
      KeyMap map = KeyMapReader.Read(_englishUs);

      Assert.Contains(new KeyMapEntry(0x10, "q", "Q", true, false, false), map.Entries);
      Assert.Contains(new KeyMapEntry(0x02, "1", "!", false, false, false), map.Entries);
    }
  }
}
