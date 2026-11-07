namespace Rekey.Core {
  /// <summary>
  /// Holds the user's settings as an immutable snapshot; a change produces a new snapshot.
  /// </summary>
  public sealed record Settings {
    /// <summary>
    /// Gets the schema version of the settings.
    /// </summary>
    /// <value>The schema version. The default is 1.</value>
    public int Version { get; } = 1;

    /// <summary>
    /// Gets a value indicating whether Auto Correction is on.
    /// </summary>
    /// <value>
    /// <see langword="true" /> if Rekey corrects wrong-layout Words on its own; otherwise,
    /// <see langword="false" />. The default is <see langword="true" />.
    /// </value>
    public bool AutoCorrectionEnabled { get; init; } = true;
  }
}
