using Rekey.Core.Languages;

namespace Rekey.Accuracy {
  /// <summary>
  /// Reads the accuracy corpus (see data/accuracy/SOURCES.md).
  /// </summary>
  internal static class Corpus {
    private static readonly (Language Language, string Code)[] _languages = [
        (Language.English, "en"), (Language.Russian, "ru"), (Language.Ukrainian, "uk")];

    /// <summary>
    /// Reads the held-out sentences of every Language.
    /// </summary>
    /// <param name="folder">The corpus folder.</param>
    /// <param name="sample">The number of sentences to read per Language, from the start.</param>
    /// <returns>The sentences.</returns>
    public static IReadOnlyList<Passage> ReadNatural(string folder, int sample) {
      return [.. _languages.SelectMany(language => File
          .ReadLines(Path.Join(folder, $"{language.Code}.tsv"))
          .Take(sample)
          .Select(line => new Passage(language.Language, Field(line))))];
    }

    /// <summary>
    /// Reads the technical and mixed-language lines.
    /// </summary>
    /// <param name="folder">The corpus folder.</param>
    /// <returns>The lines.</returns>
    public static IReadOnlyList<Passage> ReadTechnical(string folder) {
      return [.. File.ReadLines(Path.Join(folder, "technical.tsv"))
          .Select(line => new Passage(LanguageOf(line), Field(line)))];
    }

    private static Language LanguageOf(string line) {
      string code = line[..line.IndexOf('\t', StringComparison.Ordinal)];
      return _languages.Single(language => language.Code == code).Language;
    }

    private static string Field(string line) {
      return line[(line.IndexOf('\t', StringComparison.Ordinal) + 1)..];
    }
  }
}
