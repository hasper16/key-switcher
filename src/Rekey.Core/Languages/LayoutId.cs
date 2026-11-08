using System.Globalization;

namespace Rekey.Core.Languages {
  /// <summary>
  /// Identifies a Windows Layout by its KLID, such as <c>00000419</c> for Russian.
  /// </summary>
  /// <param name="Value">The KLID as a number.</param>
  public readonly record struct LayoutId(uint Value) {
    /// <summary>
    /// Gets the language id of the Layout: the low word of its KLID.
    /// </summary>
    public ushort LanguageId => (ushort)(Value & 0xFFFF);

    /// <summary>
    /// Converts the 8-digit hexadecimal text of a KLID to a Layout id.
    /// </summary>
    /// <param name="text">The KLID text, such as <c>00000419</c>.</param>
    /// <param name="id">
    /// When this method returns, contains the Layout id if the text is a KLID. This parameter is
    /// treated as uninitialized.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the text is a KLID; otherwise, <see langword="false" />.
    /// </returns>
    public static bool TryParse(string? text, out LayoutId id) {
      uint value = 0;
      bool parsed = text is { Length: 8 } && uint.TryParse(
          text, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value);
      id = new LayoutId(value);
      return parsed;
    }
  }
}
