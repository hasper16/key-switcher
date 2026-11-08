using System.Text;
using System.Text.Json;
using Rekey.Core.Languages;

namespace Rekey.ProfileBuilder {
  /// <summary>
  /// Generates a Language Profile for each Language folder in the sources folder.
  /// </summary>
  internal static class BuildCommand {
    /// <summary>
    /// The exit code for success.
    /// </summary>
    public const int Success = 0;

    /// <summary>
    /// The exit code for sources that the sources record does not list.
    /// </summary>
    public const int UnlistedSources = 1;

    private const string _definitionFile = "language.json";
    private const string _shortWordsFile = "short-words.txt";

    /// <summary>
    /// Checks the sources record, then writes <c>&lt;code&gt;.json</c> for each Language folder.
    /// </summary>
    /// <param name="sourcesFolder">The folder with one subfolder per Language.</param>
    /// <param name="outputFolder">The folder to write the profiles to.</param>
    /// <param name="log">The writer for progress lines.</param>
    /// <param name="error">The writer for problems.</param>
    /// <returns>The exit code.</returns>
    public static int Run(
        string sourcesFolder, string outputFolder, TextWriter log, TextWriter error) {
      IReadOnlyList<string> unlisted = SourcesRecord.Unlisted(sourcesFolder);
      if (unlisted.Count > 0) {
        error.WriteLine($"{SourcesRecord.FileName} does not list: {string.Join(", ", unlisted)}");
        return UnlistedSources;
      }

      Directory.CreateDirectory(outputFolder);
      IEnumerable<string> folders =
          Directory.GetDirectories(sourcesFolder).Order(StringComparer.Ordinal);
      foreach (string path in folders.Select(folder => BuildOne(folder, outputFolder))) {
        log.WriteLine($"Wrote {path}");
      }

      return Success;
    }

    private static string BuildOne(string folder, string outputFolder) {
      LanguageDefinition definition;
      using (FileStream stream = File.OpenRead(Path.Join(folder, _definitionFile))) {
        definition = JsonSerializer.Deserialize(
            stream, LanguageDefinitionJsonContext.Default.LanguageDefinition)
            ?? throw new InvalidDataException($"{_definitionFile} in {folder} is null.");
      }

      // A path could read a file outside the folder that SOURCES.md checks.
      if (Path.GetFileName(definition.FrequencyList) != definition.FrequencyList) {
        throw new InvalidDataException(
            $"frequencyList in {folder} must be a file name in the same folder.");
      }

      LanguageProfileDocument document = ProfileGenerator.Build(
          definition,
          File.ReadLines(Path.Join(folder, definition.FrequencyList)),
          File.ReadLines(Path.Join(folder, _shortWordsFile)),
          new BuildOptions());
      string path = Path.Join(outputFolder, LanguageProfileJson.FileNameOf(definition.Code));
      File.WriteAllText(path, LanguageProfileJson.Write(document), new UTF8Encoding(false));
      return path;
    }
  }
}
