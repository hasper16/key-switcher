using Rekey.Core.Languages;

namespace Rekey.Win32.Layouts {
  /// <summary>
  /// Maps a keyboard layout handle (HKL) to the KLID of its Layout without activating it.
  /// </summary>
  /// <remarks>
  /// The low word of an HKL is the language; the high word names the layout. A plain high word
  /// is the language id of a default Layout (KLID <c>0000xxxx</c>). <c>Fxxx</c> marks a variant
  /// Layout whose <c>Layout Id</c> registry value is <c>xxx</c>. <c>Exxx</c> marks an input
  /// method, whose KLID equals the handle.
  /// </remarks>
  internal sealed class LayoutIdResolver {
    private const uint _kindMask = 0xF000;
    private const uint _variant = 0xF000;
    private const uint _inputMethod = 0xE000;

    private readonly ILayoutRegistry _registry;

    public LayoutIdResolver(ILayoutRegistry registry) {
      _registry = registry;
    }

    /// <summary>
    /// Returns the Layout of a handle, or <see langword="null" /> when the registry does not
    /// know it.
    /// </summary>
    public LayoutId? Resolve(uint hkl) {
      uint device = hkl >> 16;
      return (device & _kindMask) switch {
        _variant => ResolveVariant((ushort)(device & ~_kindMask), (ushort)hkl),
        _inputMethod => new LayoutId(hkl),
        _ => new LayoutId(device),
      };
    }

    private LayoutId? ResolveVariant(ushort variantId, ushort language) {
      if (_registry.VariantIds.TryGetValue(variantId, out LayoutId layout)) {
        return layout;
      }

      // The system list lacks it: a single preloaded variant of the language is the one.
      LayoutId[] preloaded = [.. _registry.Preload
          .Select(klid => _registry.Substitutes.GetValueOrDefault(klid, klid))
          .Select(klid => LayoutId.TryParse(klid, out LayoutId id) ? id : (LayoutId?)null)
          .OfType<LayoutId>()
          .Where(id => id.LanguageId == language && id.Value >> 16 != 0)
          .Distinct()];
      return preloaded.Length == 1 ? preloaded[0] : null;
    }
  }
}
