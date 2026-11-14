using Rekey.Core.Detection;

namespace Rekey.Core {
  /// <summary>
  /// Holds the user's settings as an immutable snapshot; a change produces a new snapshot.
  /// </summary>
  /// <param name="AutoCorrectionEnabled">
  /// <see langword="true" /> to let Rekey correct wrong-layout Words on its own; otherwise,
  /// <see langword="false" />. The default is <see langword="true" />.
  /// </param>
  /// <param name="TriggerKeys">
  /// A bitwise combination of the enumeration values that specifies the Trigger Keys. The
  /// default is Space and Enter; Tab is left out because it drives autocomplete and focus moves
  /// (design decision).
  /// </param>
  /// <param name="ExcludedApps">The Excluded Apps. The default is none.</param>
  /// <param name="PausedUntil">
  /// The time the user's Pause ends, or <see langword="null" /> when Auto Correction is not
  /// paused. The default is <see langword="null" />.
  /// </param>
  /// <param name="SkipAfterManualLayoutSwitch">
  /// <see langword="true" /> to leave a Word alone when the user switched Layout themselves
  /// while typing it; otherwise, <see langword="false" />. The default is
  /// <see langword="true" />.
  /// </param>
  /// <param name="Sensitivity">
  /// One of the enumeration values that specifies how much evidence Rekey needs before
  /// correcting. The default is <see cref="Detection.Sensitivity.Balanced" />.
  /// </param>
  /// <param name="KeyboardWatchdogEnabled">
  /// <see langword="true" /> to let Rekey notice when Windows stops delivering Keystrokes and
  /// reattach; otherwise, <see langword="false" />. The default is <see langword="true" />.
  /// </param>
  /// <param name="RestartAfterFreezeEnabled">
  /// <see langword="true" /> to let Rekey restart itself after its input processing froze a
  /// second time within an hour; otherwise, <see langword="false" />. The default is
  /// <see langword="true" />.
  /// </param>
  /// <param name="VerifiedReplacement">
  /// <see langword="true" /> to check that the text before the caret is the Word before
  /// replacing it, where the focused control exposes its text; otherwise,
  /// <see langword="false" />. The default is <see langword="true" />.
  /// </param>
  // Each setting is a constructor parameter with a default: in .NET 10, source-generated JSON
  // gives an init-only property that the file omits its type default, not its initializer.
  public sealed record Settings(
      bool AutoCorrectionEnabled = true,
      TriggerKeys TriggerKeys = TriggerKeys.Space | TriggerKeys.Enter,
      ExcludedAppList ExcludedApps = default,
      DateTimeOffset? PausedUntil = null,
      bool SkipAfterManualLayoutSwitch = true,
      Sensitivity Sensitivity = Sensitivity.Balanced,
      bool KeyboardWatchdogEnabled = true,
      bool RestartAfterFreezeEnabled = true,
      bool VerifiedReplacement = true) {
    /// <summary>
    /// Gets the schema version of the settings.
    /// </summary>
    /// <value>The schema version. The default is 1.</value>
    public int Version { get; } = 1;
  }
}
