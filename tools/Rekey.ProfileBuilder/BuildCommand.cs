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
      foreach (string folder in folders) {
        string path = BuildOne(folder, outputFolder);
        log.WriteLine($"Wrote {path}");
      }

      return Success;
    }

    private static string BuildOne(string folder, string outputFolder) {
      LanguageDefinition definition;
      using (FileStream stream = File.OpenRead(Path.Combine(folder, _definitionFile))) {
        definition = JsonSerializer.Deserialize(
            stream, LanguageDefinitionJsonContext.Default.LanguageDefinition)
            ?? throw new InvalidDataException($"{_definitionFile} in {folder} is null.");
      }

      LanguageProfileDocument document = ProfileGenerator.Build(
          definition,
          File.ReadLines(Path.Combine(folder, definition.FrequencyList)),
          File.ReadLines(Path.Combine(folder, _shortWordsFile)),
          new BuildOptions());
      string path = Path.Combine(outputFolder, LanguageProfileJson.FileNameOf(definition.Code));
      File.WriteAllText(path, LanguageProfileJson.Write(document), new UTF8Encoding(false));
      return path;
    }
  }
}
