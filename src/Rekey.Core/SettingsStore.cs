using System.Text.Json;

namespace Rekey.Core {
  /// <summary>
  /// Loads and saves the user's settings in a JSON file.
  /// </summary>
  public sealed class SettingsStore {
    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsStore" /> class.
    /// </summary>
    /// <param name="folder">
    /// The folder that holds the settings file; it is created when missing.
    /// </param>
    public SettingsStore(string folder) {
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
    /// parsed, renames it with a <c>.bak</c> suffix (a dated one when that backup is locked), then
    /// does the same.
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

      JsonFile.KeepUnreadable(FilePath);
      return SaveDefaults();
    }

    /// <summary>
    /// Writes the settings to the file, replacing it in one step.
    /// </summary>
    /// <param name="settings">The settings to write.</param>
    public void Save(Settings settings) {
      JsonFile.WriteAtomically(FilePath, stream =>
          JsonSerializer.Serialize(stream, settings, SettingsJsonContext.Default.Settings));
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
