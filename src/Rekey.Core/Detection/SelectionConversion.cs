using System.Globalization;
using System.Text;

namespace Rekey.Core.Detection {
  /// <summary>
  /// Converts a selected text typed in one Layout to what the same keys type in another (design
  /// D4 of add-manual-correction).
  /// </summary>
  public static class SelectionConversion {
    /// <summary>
    /// Returns the text the keys that typed <paramref name="text" /> in one Layout type in
    /// another.
    /// </summary>
    /// <remarks>
    /// Each character goes back to its key, the unshifted position first; a character the
    /// source Layout cannot type, or whose key types nothing in the target Layout, stays as it
    /// is.
    /// </remarks>
    /// <param name="text">The selected text.</param>
    /// <param name="source">The Key Map of the Layout the text was typed in.</param>
    /// <param name="target">The Key Map of the Layout to render it in.</param>
    /// <returns>The converted text.</returns>
    public static string Convert(string text, KeyMap source, KeyMap target) {
      var converted = new StringBuilder(text.Length);
      TextElementEnumerator characters = StringInfo.GetTextElementEnumerator(text);
      while (characters.MoveNext()) {
        string character = characters.GetTextElement();
        string? rendered = source.Type(character, capsLock: false) is { } keys
            ? target.Render(keys)
            : null;
        converted.Append(string.IsNullOrEmpty(rendered) ? character : rendered);
      }

      return converted.ToString();
    }
  }
}
