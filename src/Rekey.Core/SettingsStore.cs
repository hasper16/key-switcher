using System.Text.Json;

namespace Rekey.Core {
  /// <summary>
  /// Loads and saves the user's settings in a JSON file.
  /// </summary>
  public sealed class SettingsStore {
    private readonly string _folder;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsStore" /> class.
    /// </summary>
    /// <param name="folder">
    /// The folder that holds the settings file; it is created when missing.
    /// </param>
    public SettingsStore(string folder) {
      _folder = folder;
      FilePath = Path.Combine(folder, "settings.json");
    }

    /// <summary>
    /// Gets the full path of the settings file.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Returns the stored settings.
    /// </summary>
    /// <remarks>
    /// When the file is missing, writes the defaults and returns them. When the file cannot be
    /// parsed, renames it with a <c>.bak</c> suffix, then does the same.
    /// </remarks>
    /// <returns>The stored settings, or the defaults when there are none to read.</returns>
    public Settings Load() {
      if (!File.Exists(FilePath)) {
        return SaveDefaults();
      }

      Settings? settings = TryRead();
      if (settings is not null) {
        return settings;
      }

      File.Move(FilePath, FilePath + ".bak", overwrite: true);
      return SaveDefaults();
    }

    /// <summary>
    /// Writes the settings to the file, replacing it in one step.
    /// </summary>
    /// <param name="settings">The settings to write.</param>
    public void Save(Settings settings) {
      Directory.CreateDirectory(_folder);
      string tempPath = FilePath + ".tmp";
      using (FileStream stream = File.Create(tempPath)) {
        JsonSerializer.Serialize(stream, settings, SettingsJsonContext.Default.Settings);

        // Reach the disk before the swap, so a power loss cannot leave an empty settings file.
        stream.Flush(flushToDisk: true);
      }

      if (File.Exists(FilePath)) {
        File.Replace(tempPath, FilePath, destinationBackupFileName: null);
      } else {
        File.Move(tempPath, FilePath);
      }
    }

    private Settings? TryRead() {
      try {
        using FileStream stream = File.OpenRead(FilePath);
        return JsonSerializer.Deserialize(stream, SettingsJsonContext.Default.Settings);
      } catch (JsonException) {
        return null;
      }
    }

    private Settings SaveDefaults() {
      var defaults = new Settings();
      Save(defaults);
      return defaults;
    }
  }
}
