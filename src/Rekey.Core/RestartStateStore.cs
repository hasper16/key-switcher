using System.Globalization;

namespace Rekey.Core {
  /// <summary>
  /// Keeps the time Rekey last restarted itself after a freeze, next to the settings file, so
  /// the limit of one restart per 10 minutes holds across restarts.
  /// </summary>
  public sealed class RestartStateStore {
    private readonly string _folder;

    /// <summary>
    /// Initializes a new instance of the <see cref="RestartStateStore" /> class.
    /// </summary>
    /// <param name="folder">The folder that holds the file; it is created when missing.</param>
    public RestartStateStore(string folder) {
      _folder = folder;
      FilePath = Path.Combine(folder, "restart-state.txt");
    }

    /// <summary>
    /// Gets the full path of the restart state file.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Returns the time of the last restart after a freeze.
    /// </summary>
    /// <returns>
    /// The time, or <see langword="null" /> when the file is missing or unreadable.
    /// </returns>
    public DateTimeOffset? LoadLastRestart() {
      try {
        string text = File.ReadAllText(FilePath).Trim();
        bool parsed = DateTimeOffset.TryParseExact(
            text, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset time);
        return parsed ? time : null;
      } catch (Exception e) when (e is IOException or UnauthorizedAccessException) {
        return null;
      }
    }

    /// <summary>
    /// Writes the time of a restart after a freeze.
    /// </summary>
    /// <param name="time">The time of the restart.</param>
    public void SaveLastRestart(DateTimeOffset time) {
      Directory.CreateDirectory(_folder);
      File.WriteAllText(FilePath, time.ToString("O", CultureInfo.InvariantCulture));
    }
  }
}
