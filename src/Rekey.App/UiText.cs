using System.Globalization;
using System.Text;

namespace Rekey.App {
  // Fills the placeholders of a UI string from Strings.resx in the user's culture.
  internal static class UiText {
    public static string Format<T>(string format, T argument) {
      // Parsed per call: the format depends on the UI language, which may change.
      return string.Format(CultureInfo.CurrentCulture, CompositeFormat.Parse(format), argument);
    }
  }
}
