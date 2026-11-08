namespace Rekey.ProfileBuilder {
  /// <summary>
  /// Checks that the sources record names every source file with its origin and licence.
  /// </summary>
  internal static class SourcesRecord {
    /// <summary>
    /// The name of the sources record in the sources folder.
    /// </summary>
    public const string FileName = "SOURCES.md";

    /// <summary>
    /// Returns the source files that the sources record does not name in backticks.
    /// </summary>
    /// <param name="sourcesFolder">The folder that holds the sources and the record.</param>
    /// <returns>The unlisted files as relative paths with <c>/</c>, in ordinal order.</returns>
    public static IReadOnlyList<string> Unlisted(string sourcesFolder) {
      string recordPath = Path.Join(sourcesFolder, FileName);
      string record = File.Exists(recordPath) ? File.ReadAllText(recordPath) : string.Empty;
      return [.. Directory.EnumerateFiles(sourcesFolder, "*", SearchOption.AllDirectories)
          .Select(path => Path.GetRelativePath(sourcesFolder, path).Replace('\\', '/'))
          .Where(path => path != FileName)
          .Where(path => !record.Contains($"`{path}`", StringComparison.Ordinal))
          .Order(StringComparer.Ordinal)];
    }
  }
}
