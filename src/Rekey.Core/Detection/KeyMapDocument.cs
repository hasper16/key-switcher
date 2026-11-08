namespace Rekey.Core.Detection {
  /// <summary>
  /// Holds a Key Map as its file stores it.
  /// </summary>
  /// <param name="LayoutId">The KLID of the Layout, such as <c>00000419</c>.</param>
  /// <param name="Keys">The keys, ordered by scan code.</param>
  internal sealed record KeyMapDocument(string LayoutId, IReadOnlyList<KeyMapEntry> Keys);
}
