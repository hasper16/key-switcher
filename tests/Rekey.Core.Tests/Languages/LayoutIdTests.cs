using Rekey.Core.Languages;
using Xunit;

namespace Rekey.Core.Tests.Languages {
  public sealed class LayoutIdTests {
    [Theory]
    [InlineData(0x00000409u, "EN")]
    [InlineData(0x00000419u, "RU")]
    [InlineData(0x00000422u, "UK")]
    [InlineData(0x00010409u, "EN")]
    [InlineData(0x00000407u, "DE")]
    public void LanguageCodeNamesTheLanguageOfTheLayout(uint klid, string code) {
      Assert.Equal(code, new LayoutId(klid).LanguageCode);
    }

    [Fact]
    public void UnknownLanguageIdGivesAPlaceholder() {
      Assert.Equal("??", new LayoutId(0x00000000u).LanguageCode);
    }
  }
}
