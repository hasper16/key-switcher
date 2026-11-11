using Rekey.Core;
using Rekey.Core.Languages;
using Rekey.Win32.Layouts;
using Xunit;

namespace Rekey.Win32.Tests.Layouts {
  // Task 4.5 and spec language-profiles: the installed Layouts decide the active Languages.
  public sealed class ActiveLanguagesTests {
    private readonly List<uint> _handles = [0x04090409, 0x04190419];
    private readonly Engine _engine =
        new(LanguageSet.LoadBundled(DateTimeOffset.UnixEpoch).Languages);

    [Fact]
    public void EnglishAndRussianLayoutsActivateOnlyEnglishAndRussian() {
      var list = new LayoutList(() => [.. _handles], () => new NoVariants());

      Publish(list);

      Assert.Equal([Language.English, Language.Russian], ActiveLanguages());
    }

    [Fact]
    public void AddingAUkrainianLayoutActivatesUkrainian() {
      var list = new LayoutList(() => [.. _handles], () => new NoVariants());
      Publish(list);

      _handles.Add(0x04220422);
      Publish(list);

      Assert.Contains(Language.Ukrainian, ActiveLanguages());
    }

    [Fact]
    public void NoLanguageIsActiveBeforeTheFirstLayoutList() {
      Assert.Empty(_engine.ActiveProfiles);
    }

    private void Publish(LayoutList list) {
      if (list.Refresh()) {
        _engine.Handle(new LayoutsInstalledChanged(TimeSpan.Zero, list.Layouts, []));
      }
    }

    private Language[] ActiveLanguages() {
      return [.. _engine.ActiveProfiles.Select(profile => profile.Language)];
    }

    private sealed class NoVariants : ILayoutRegistry {
      public IReadOnlyList<string> Preload { get; } = [];

      public IReadOnlyDictionary<string, string> Substitutes { get; } =
          new Dictionary<string, string>();

      public IReadOnlyDictionary<ushort, LayoutId> VariantIds { get; } =
          new Dictionary<ushort, LayoutId>();
    }
  }
}
