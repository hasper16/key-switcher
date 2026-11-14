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
  /// <param name="Hotkey">
  /// The Hotkey that asks for a Manual Correction. The default is
  /// <see cref="Core.Hotkey.DoubleShift" />.
  /// </param>
  /// <param name="ShowCursorIndicator">
  /// <see langword="true" /> to show the new Language next to the caret after Rekey switched
  /// Layout; otherwise, <see langword="false" />. The default is <see langword="true" />.
  /// </param>
  /// <param name="PlaySwitchSound">
  /// <see langword="true" /> to play <paramref name="SwitchSound" /> after each Correction;
  /// otherwise, <see langword="false" />. The default is <see langword="false" />.
  /// </param>
  /// <param name="SwitchSound">
  /// One of the enumeration values that specifies the switch sound. The default is
  /// <see cref="Core.SwitchSound.KeyClick" />.
  /// </param>
  /// <param name="LaunchAtSignIn">
  /// <see langword="true" /> to start Rekey when the user signs in to Windows; otherwise,
  /// <see langword="false" />. The default is <see langword="false" />; on start the app reads
  /// the real state from Windows, which the user may change in Task Manager.
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
      bool VerifiedReplacement = true,
      Hotkey Hotkey = default,
      bool ShowCursorIndicator = true,
      bool PlaySwitchSound = false,
      SwitchSound SwitchSound = SwitchSound.KeyClick,
      bool LaunchAtSignIn = false) {
    /// <summary>
    /// The <see cref="PausedUntil" /> value of a Pause that lasts until Rekey restarts.
    /// </summary>
    public static readonly DateTimeOffset UntilRestart = DateTimeOffset.MaxValue;

    /// <summary>
    /// Gets the schema version of the settings.
    /// </summary>
    /// <value>The schema version. The default is 1.</value>
    public int Version { get; } = 1;

    /// <summary>
    /// Returns the settings a newly started Rekey uses: a Pause until restart has ended.
    /// </summary>
    /// <returns>The settings for the new session.</returns>
    public Settings ForNewSession() {
      return PausedUntil == UntilRestart ? this with { PausedUntil = null } : this;
    }
  }
}
