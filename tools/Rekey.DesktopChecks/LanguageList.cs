using System.Globalization;
using System.Text;

namespace Rekey.DesktopChecks {
  // The user's Windows language list with each language's input methods, as PowerShell's
  // Get-WinUserLanguageList shows it, and the scripts that read and write it back exactly.
  internal sealed record LanguageList(IReadOnlyList<LanguageList.Language> Languages) {
    // Get-WinUserLanguageList writes the whole list as one object, which both ForEach-Object and
    // foreach see as a single item, so the script indexes into it: one line per language,
    // "tag|tip,tip".
    public const string ReadScript =
        "$list = Get-WinUserLanguageList; for ($i = 0; $i -lt $list.Count; $i++) { "
        + "$list[$i].LanguageTag + '|' + ($list[$i].InputMethodTips -join ',') }";

    // Parses ReadScript's output; throws rather than guess, so no change is made from a bad
    // snapshot.
    public static LanguageList Parse(string output) {
      const StringSplitOptions lines =
          StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;
      var languages = new List<Language>();
      foreach (string[] parts in output.Split('\n', lines).Select(line => line.Split('|'))) {
        if (parts.Length != 2 || parts[0].Length == 0) {
          throw new FormatException($"Unexpected language list line {languages.Count + 1}.");
        }

        languages.Add(new Language(parts[0], [.. parts[1].Split(',', lines)]));
      }

      if (languages.Count == 0) {
        throw new FormatException("The language list is empty.");
      }

      return new LanguageList(languages);
    }

    public bool Has(string tag) {
      return Languages.Any(
          language => string.Equals(language.Tag, tag, StringComparison.OrdinalIgnoreCase));
    }

    // A script that makes the user's list exactly this one, input methods included.
    public string WriteScript() {
      var script = new StringBuilder();
      for (int i = 0; i < Languages.Count; i++) {
        Language language = Languages[i];
        string tag = Quote(language.Tag);
        script.Append(i == 0 ? $"$l = New-WinUserLanguageList {tag}; " : $"$l.Add({tag}); ");
        script.Append(CultureInfo.InvariantCulture, $"$l[{i}].InputMethodTips.Clear(); ");
        foreach (string tip in language.Tips) {
          script.Append(
              CultureInfo.InvariantCulture, $"$l[{i}].InputMethodTips.Add({Quote(tip)}); ");
        }
      }

      script.Append("Set-WinUserLanguageList $l -Force");
      return script.ToString();
    }

    // The snapshot file's text: ReadScript's format, one language per line.
    public string ToText() {
      return string.Join(
          '\n', Languages.Select(language => $"{language.Tag}|{string.Join(',', language.Tips)}"));
    }

    public bool SameAs(LanguageList other) {
      return Languages.Count == other.Languages.Count
          && Languages.Zip(other.Languages).All(pair => pair.First.SameAs(pair.Second));
    }

    private static string Quote(string value) {
      return "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";
    }

    internal sealed record Language(string Tag, IReadOnlyList<string> Tips) {
      public bool SameAs(Language other) {
        return string.Equals(Tag, other.Tag, StringComparison.OrdinalIgnoreCase)
            && Tips.SequenceEqual(other.Tips, StringComparer.OrdinalIgnoreCase);
      }
    }
  }
}
