using System.Globalization;

namespace Rekey.DesktopChecks {
  // The user's Windows language list with each language's settable properties, as PowerShell's
  // Get-WinUserLanguageList shows it, and the scripts that read it and put it back.
  internal sealed record LanguageList(IReadOnlyList<LanguageList.Language> Languages) {
    // Get-WinUserLanguageList writes the whole list as one object, which both ForEach-Object and
    // foreach see as a single item, so the script indexes into it: one line per language,
    // "tag|tip,tip|spellchecking|handwriting".
    public const string ReadScript =
        "$list = Get-WinUserLanguageList; for ($i = 0; $i -lt $list.Count; $i++) { "
        + "$list[$i].LanguageTag + '|' + ($list[$i].InputMethodTips -join ',') + '|' "
        + "+ $list[$i].Spellchecking + '|' + $list[$i].Handwriting }";

    // Parses ReadScript's output; throws rather than guess, so no change is made from a bad
    // snapshot.
    public static LanguageList Parse(string output) {
      const StringSplitOptions lines =
          StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;
      var languages = new List<Language>();
      foreach (string[] parts in output.Split('\n', lines).Select(line => line.Split('|'))) {
        if (parts.Length != 4 || parts[0].Length == 0
            || !bool.TryParse(parts[2], out bool spellchecking)
            || !bool.TryParse(parts[3], out bool handwriting)) {
          throw new FormatException($"Unexpected language list line {languages.Count + 1}.");
        }

        languages.Add(new Language(
            parts[0], [.. parts[1].Split(',', lines)], spellchecking, handwriting));
      }

      if (languages.Count == 0) {
        throw new FormatException("The language list is empty.");
      }

      return new LanguageList(languages);
    }

    // A script that removes languages from the user's list and leaves every other language
    // object, with all its settings, as it is.
    public static string RemoveScript(IEnumerable<string> tags) {
      string quoted = string.Join(", ", tags.Select(Quote));
      return "$l = Get-WinUserLanguageList; $remove = @(" + quoted + "); "
          + "for ($i = $l.Count - 1; $i -ge 0; $i--) { "
          + "if ($remove -contains $l[$i].LanguageTag) { $l.RemoveAt($i) } }; "
          + "Set-WinUserLanguageList $l -Force";
    }

    public bool Has(string tag) {
      return Languages.Any(
          language => string.Equals(language.Tag, tag, StringComparison.OrdinalIgnoreCase));
    }

    // The snapshot file's text: ReadScript's format, one language per line.
    public string ToText() {
      return string.Join('\n', Languages.Select(language => language.ToText()));
    }

    public bool SameAs(LanguageList other) {
      return Languages.Count == other.Languages.Count
          && Languages.Zip(other.Languages).All(pair => pair.First.SameAs(pair.Second));
    }

    private static string Quote(string value) {
      return "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";
    }

    internal sealed record Language(
        string Tag, IReadOnlyList<string> Tips, bool Spellchecking, bool Handwriting) {
      public bool SameAs(Language other) {
        return string.Equals(Tag, other.Tag, StringComparison.OrdinalIgnoreCase)
            && Tips.SequenceEqual(other.Tips, StringComparer.OrdinalIgnoreCase)
            && Spellchecking == other.Spellchecking
            && Handwriting == other.Handwriting;
      }

      public string ToText() {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{Tag}|{string.Join(',', Tips)}|{Spellchecking}|{Handwriting}");
      }
    }
  }
}
