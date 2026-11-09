using Rekey.Core.Languages;
using Rekey.Win32.Layouts;
using Xunit;

namespace Rekey.Win32.Tests.Layouts {
  public sealed class LayoutIdResolverTests {
    private static readonly FakeLayoutRegistry _registry = new(
        Preload: ["00000409", "00000422", "d0010422"],
        Substitutes: new Dictionary<string, string> {
          ["00000422"] = "00000419",
          ["d0010422"] = "00020422",
        },
        VariantIds: new Dictionary<ushort, LayoutId> { [0x00A8] = new(0x00020422) });

    [Theory]
    [InlineData(0x04090409u, 0x00000409u)]
    [InlineData(0x04190419u, 0x00000419u)]
    [InlineData(0x04220422u, 0x00000422u)]
    public void DefaultLayoutOfALanguageIsItsLanguageId(uint hkl, uint klid) {
      Assert.Equal(new LayoutId(klid), new LayoutIdResolver(_registry).Resolve(hkl));
    }

    [Fact]
    public void LanguageTypedWithAnotherLanguagesLayoutResolvesToThatLayout() {
      // Ukrainian language with the Russian keyboard.
      Assert.Equal(new LayoutId(0x00000419), new LayoutIdResolver(_registry).Resolve(0x04190422));
    }

    [Fact]
    public void VariantLayoutResolvesThroughItsLayoutId() {
      Assert.Equal(new LayoutId(0x00020422), new LayoutIdResolver(_registry).Resolve(0xF0A80422));
    }

    [Fact]
    public void VariantMissingFromTheSystemListFallsBackToThePreloadedSubstitute() {
      var registry = _registry with { VariantIds = new Dictionary<ushort, LayoutId>() };

      Assert.Equal(new LayoutId(0x00020422), new LayoutIdResolver(registry).Resolve(0xF0A80422));
    }

    [Fact]
    public void UnknownVariantResolvesToNothing() {
      Assert.Null(new LayoutIdResolver(_registry).Resolve(0xF0B10409));
    }

    [Fact]
    public void InputMethodEditorKeepsItsHandleAsTheId() {
      Assert.Equal(new LayoutId(0xE0010411), new LayoutIdResolver(_registry).Resolve(0xE0010411));
    }

    private sealed record FakeLayoutRegistry(
        IReadOnlyList<string> Preload,
        IReadOnlyDictionary<string, string> Substitutes,
        IReadOnlyDictionary<ushort, LayoutId> VariantIds) : ILayoutRegistry;
  }
}
