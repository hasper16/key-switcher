using Rekey.Core.Languages;

namespace Rekey.Win32.Layouts {
  /// <summary>
  /// Keeps the installed Layouts and their handles, refreshed by polling.
  /// </summary>
  /// <remarks>
  /// Windows tells only the window that changed about a new Layout, so the worker polls this list
  /// every 2 seconds (design D5). Not thread-safe: the worker thread owns it.
  /// </remarks>
  internal sealed class LayoutList {
    private readonly Func<IReadOnlyList<uint>> _readHandles;
    private readonly Func<ILayoutRegistry> _readRegistry;
    private uint[] _handles = [];
    private Dictionary<uint, LayoutId> _byHandle = [];

    public LayoutList(Func<IReadOnlyList<uint>> readHandles, Func<ILayoutRegistry> readRegistry) {
      _readHandles = readHandles;
      _readRegistry = readRegistry;
    }

    /// <summary>Gets the installed Layouts, in Windows' list order.</summary>
    public IReadOnlyList<LayoutId> Layouts { get; private set; } = [];

    /// <summary>
    /// Reads the installed Layouts again and returns whether they changed.
    /// </summary>
    public bool Refresh() {
      uint[] handles = [.. _readHandles()];
      if (handles.SequenceEqual(_handles)) {
        return false;
      }

      // The registry changes only with the list, so it is read again only then.
      var resolver = new LayoutIdResolver(_readRegistry());
      var byHandle = new Dictionary<uint, LayoutId>();
      foreach (uint handle in handles) {
        if (resolver.Resolve(handle) is { } layout) {
          byHandle[handle] = layout;
        }
      }

      _handles = handles;
      _byHandle = byHandle;
      IReadOnlyList<LayoutId> layouts = [.. byHandle.Values.Distinct()];
      bool changed = !layouts.SequenceEqual(Layouts);
      Layouts = layouts;
      return changed;
    }

    /// <summary>Returns the Layout of a handle, or <see langword="null" /> when unknown.</summary>
    public LayoutId? LayoutOf(uint handle) {
      return _byHandle.TryGetValue(handle, out LayoutId layout) ? layout : null;
    }

    /// <summary>
    /// Returns a handle of a Layout, or <see langword="null" /> when it is not installed.
    /// </summary>
    public uint? HandleOf(LayoutId layout) {
      foreach ((uint handle, LayoutId candidate) in _byHandle) {
        if (candidate == layout) {
          return handle;
        }
      }

      return null;
    }
  }
}
