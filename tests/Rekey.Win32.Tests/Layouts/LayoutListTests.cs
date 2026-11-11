using Rekey.Core.Languages;
using Rekey.Win32.Layouts;
using Xunit;

namespace Rekey.Win32.Tests.Layouts {
  // Spec layout-management: "Installed Layouts are known and kept current".
  public sealed class LayoutListTests {
    private const uint _englishUs = 0x04090409;
    private const uint _russian = 0x04190419;
    private const uint _ukrainian = 0x04220422;

    private readonly List<uint> _handles = [_englishUs, _russian];

    [Fact]
    public void FirstRefreshReportsTheInstalledLayouts() {
      var list = NewList();

      Assert.True(list.Refresh());
      Assert.Equal([new LayoutId(0x409), new LayoutId(0x419)], list.Layouts);
    }

    [Fact]
    public void UnchangedListReportsNoChange() {
      var list = NewList();
      list.Refresh();

      Assert.False(list.Refresh());
    }

    [Fact]
    public void AddedLayoutIsReported() {
      var list = NewList();
      list.Refresh();
      _handles.Add(_ukrainian);

      Assert.True(list.Refresh());
      Assert.Contains(new LayoutId(0x422), list.Layouts);
    }

    [Fact]
    public void RemovedLayoutIsReported() {
      var list = NewList();
      list.Refresh();
      _handles.Remove(_russian);

      Assert.True(list.Refresh());
      Assert.Equal([new LayoutId(0x409)], list.Layouts);
    }

    [Fact]
    public void HandlesAndLayoutsMapBothWays() {
      var list = NewList();
      list.Refresh();

      Assert.Equal(new LayoutId(0x419), list.LayoutOf(_russian));
      Assert.Equal(_russian, list.HandleOf(new LayoutId(0x419)));
      Assert.Null(list.LayoutOf(_ukrainian));
    }

    private LayoutList NewList() {
      var registry = new EmptyRegistry();
      return new LayoutList(() => [.. _handles], () => registry);
    }

    private sealed class EmptyRegistry : ILayoutRegistry {
      public IReadOnlyList<string> Preload { get; } = [];

      public IReadOnlyDictionary<string, string> Substitutes { get; } =
          new Dictionary<string, string>();

      public IReadOnlyDictionary<ushort, LayoutId> VariantIds { get; } =
          new Dictionary<ushort, LayoutId>();
    }
  }
}
