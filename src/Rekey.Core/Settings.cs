using Rekey.Core.Detection;

namespace Rekey.Core {
  /// <summary>
  /// Holds the user's settings as an immutable snapshot; a change produces a new snapshot.
  /// </summary>
  /// <param name="AutoCorrectionEnabled">
  /// <see langword="true" /> to let Rekey correct wrong-layout Words on its own; otherwise,
  /// <see langword="false" />. The default is <see langword="true" />.
  /// </param>
  /// <param name="Sensitivity">
  /// One of the enumeration values that specifies how much evidence Rekey needs before
  /// correcting. The default is <see cref="Detection.Sensitivity.Balanced" />.
  /// </param>
  // Each setting is a constructor parameter with a default: in .NET 10, source-generated JSON
  // gives an init-only property that the file omits its type default, not its initializer.
  public sealed record Settings(
      bool AutoCorrectionEnabled = true, Sensitivity Sensitivity = Sensitivity.Balanced) {
    /// <summary>
    /// Gets the schema version of the settings.
    /// </summary>
    /// <value>The schema version. The default is 1.</value>
    public int Version { get; } = 1;
  }
}
