using Rekey.Core.Languages;

namespace Rekey.Win32.Layouts {
  /// <summary>
  /// Reads the registry values that tie a keyboard layout handle (HKL) to its KLID.
  /// </summary>
  internal interface ILayoutRegistry {
    /// <summary>
    /// Gets the KLIDs under <c>HKCU\Keyboard Layout\Preload</c>, in list order.
    /// </summary>
    IReadOnlyList<string> Preload { get; }

    /// <summary>
    /// Gets <c>HKCU\Keyboard Layout\Substitutes</c>: a preloaded KLID and the KLID that
    /// replaces it.
    /// </summary>
    IReadOnlyDictionary<string, string> Substitutes { get; }

    /// <summary>
    /// Gets each variant Layout's <c>Layout Id</c> value under
    /// <c>HKLM\SYSTEM\CurrentControlSet\Control\Keyboard Layouts</c> and the KLID it belongs to.
    /// </summary>
    IReadOnlyDictionary<ushort, LayoutId> VariantIds { get; }
  }
}
