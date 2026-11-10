namespace Rekey.Core {
  /// <summary>
  /// Specifies the kind of an Incident.
  /// </summary>
  /// <remarks>
  /// Later changes add a value for each Incident they record.
  /// </remarks>
  public enum IncidentType {
    /// <summary>
    /// A Language Profile could not be read or failed validation, so its Language is unavailable.
    /// Details: the <see cref="Languages.Language" /> skipped.
    /// </summary>
    LanguageProfileSkipped,

    /// <summary>
    /// A keyboard or mouse hook callback took longer than 1 ms at least once since the last such
    /// Incident; Windows removes a hook that is too slow. No details.
    /// </summary>
    SlowHookCallback,

    /// <summary>
    /// A Layout Switch Rekey requested did not take effect within 500 ms; Rekey adopted the
    /// window's real Layout. No details.
    /// </summary>
    SwitchNotLanded,

    /// <summary>
    /// Windows stopped delivering input to Rekey's hooks while the user typed; Rekey removed and
    /// installed them again. No details.
    /// </summary>
    HookReinstalled,

    /// <summary>
    /// Keyboard observation failed twice within 10 minutes; Rekey paused Auto Correction for
    /// 10 minutes. No details.
    /// </summary>
    SelfPaused,

    /// <summary>
    /// Rekey's input processing stopped for more than 5 seconds with input waiting, and Rekey
    /// did not restart for it. No details.
    /// </summary>
    InputFrozen,

    /// <summary>
    /// Rekey's input processing froze a second time within an hour; Rekey restarted itself.
    /// No details.
    /// </summary>
    FreezeRestart,
  }
}
