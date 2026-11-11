namespace Rekey.DesktopChecks {
  // The language list as it was before the run, and the one language the run added; a restore
  // removes only that language, so one the user adds later is never touched.
  internal sealed record LanguageSnapshot(LanguageList List, string Added) {
    private const string _addedPrefix = "added=";

    // The snapshot file: the added language on the first line, then the list.
    public static LanguageSnapshot Parse(string text) {
      string[] lines = text.Split('\n', 2);
      string first = lines[0].Trim();
      if (!first.StartsWith(_addedPrefix, StringComparison.Ordinal)
          || first.Length == _addedPrefix.Length || lines.Length < 2) {
        throw new FormatException("The language snapshot does not name the added language.");
      }

      return new LanguageSnapshot(LanguageList.Parse(lines[1]), first[_addedPrefix.Length..]);
    }

    public string ToText() {
      return _addedPrefix + Added + "\n" + List.ToText();
    }

    // The languages to remove from the current list: the added one, if it is still there.
    public IReadOnlyList<string> ToRemoveFrom(LanguageList current) {
      return current.Has(Added) ? [Added] : [];
    }
  }
}
