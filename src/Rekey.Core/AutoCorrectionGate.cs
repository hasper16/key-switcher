namespace Rekey.Core {
  /// <summary>
  /// Decides whether Auto Correction applies to the Word being typed.
  /// </summary>
  /// <remarks>
  /// The one place this is decided: the engine asks it for every Trigger Key, and the keys the
  /// hook holds back follow its answer.
  /// </remarks>
  public static class AutoCorrectionGate {
    /// <summary>
    /// Returns whether Auto Correction applies, or the first reason it does not.
    /// </summary>
    /// <param name="settings">The user's settings.</param>
    /// <param name="focus">
    /// The focused control, or <see langword="null" /> while it is unknown; an unknown control
    /// may be a Password Field and counts as one.
    /// </param>
    /// <param name="selfPaused">
    /// <see langword="true" /> if Rekey paused itself after repeated keyboard failures;
    /// otherwise, <see langword="false" />.
    /// </param>
    /// <param name="layoutSwitchedByUser">
    /// <see langword="true" /> if the user switched Layout themselves during the current Word;
    /// otherwise, <see langword="false" />.
    /// </param>
    /// <param name="now">The current time, compared with the end of a Pause.</param>
    /// <returns>One of the enumeration values that specifies the answer.</returns>
    public static GateResult Decide(
        Settings settings,
        Context? focus,
        bool selfPaused,
        bool layoutSwitchedByUser,
        DateTimeOffset now) {
      if (!settings.AutoCorrectionEnabled) {
        return GateResult.Off;
      }

      if (now < settings.PausedUntil) {
        return GateResult.Paused;
      }

      if (selfPaused) {
        return GateResult.SelfPaused;
      }

      return InFocus(settings, focus)
          ?? (layoutSwitchedByUser && settings.SkipAfterManualLayoutSwitch
              ? GateResult.LayoutSwitchedByUser
              : GateResult.Applies);
    }

    private static GateResult? InFocus(Settings settings, Context? focus) {
      if (focus is not null && settings.ExcludedApps.Contains(focus.App)) {
        return GateResult.ExcludedApp;
      }

      if (focus is not { IsPasswordField: false }) {
        return GateResult.PasswordField;
      }

      return focus.IsElevated ? GateResult.Elevated : null;
    }
  }
}
