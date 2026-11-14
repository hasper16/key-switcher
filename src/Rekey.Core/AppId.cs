namespace Rekey.Core {
  /// <summary>
  /// Identifies the application that owns the focused window.
  /// </summary>
  /// <param name="ExePath">
  /// The full image path of the application's process, or an empty string when it cannot be
  /// read.
  /// </param>
  /// <param name="Aumid">
  /// The application user model id of a packaged Windows app, or <see langword="null" /> for
  /// an app without one. The default is <see langword="null" />.
  /// </param>
  public sealed record AppId(string ExePath, string? Aumid = null) {
    /// <summary>
    /// Gets the identity Rekey tells applications apart by: the application user model id when
    /// the app has one, because a packaged app's path changes with every update and a frame
    /// process hosts several apps; otherwise the image path in its compared form.
    /// </summary>
    public string Key => Aumid ?? Normalise(ExePath);

    /// <summary>
    /// Returns an application identity in the form it is compared in.
    /// </summary>
    /// <remarks>
    /// An image path loses surrounding quotes and the <c>\\?\</c> prefix (<c>\\?\UNC\</c>
    /// becomes <c>\\</c>), gets <c>\</c>
    /// separators and has its <c>.</c> and <c>..</c> segments resolved; anything else is only
    /// trimmed. Identities compare without case; see <see cref="ExcludedAppList" />.
    /// </remarks>
    /// <param name="identity">An image path or an application user model id.</param>
    /// <returns>The identity in its compared form.</returns>
    public static string Normalise(string identity) {
      const string longPathPrefix = @"\\?\";
      const string longNetworkPrefix = @"\\?\UNC\";
      string path = identity.Trim().Trim('"').Trim().Replace('/', '\\');
      if (path.StartsWith(longNetworkPrefix, StringComparison.OrdinalIgnoreCase)) {
        // "\\?\UNC\server\share" is the long form of "\\server\share".
        path = @"\\" + path[longNetworkPrefix.Length..];
      } else if (path.StartsWith(longPathPrefix, StringComparison.Ordinal)) {
        path = path[longPathPrefix.Length..];
      } else {
        // Already in its ordinary form.
      }

      return IsDrivePath(path) ? ResolveSegments(path) : path;
    }

    // "C:\..." only: Path.GetFullPath would resolve other forms too, but against the current
    // folder or drive, which says nothing about the application.
    private static bool IsDrivePath(string path) {
      return path.Length >= 3 && char.IsAsciiLetter(path[0]) && path[1] == ':' && path[2] == '\\';
    }

    private static string ResolveSegments(string path) {
      var kept = new List<string>();
      foreach (string segment in path[3..].Split('\\', StringSplitOptions.RemoveEmptyEntries)) {
        if (segment == "..") {
          kept.RemoveRange(Math.Max(0, kept.Count - 1), Math.Min(1, kept.Count));
        } else if (segment != ".") {
          kept.Add(segment);
        } else {
          // "." names the folder it is in.
        }
      }

      return path[..3] + string.Join('\\', kept);
    }
  }
}
