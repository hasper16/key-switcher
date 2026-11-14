namespace Rekey.Core {
  /// <summary>
  /// Specifies whether Auto Correction applies, or the reason it does not.
  /// </summary>
  public enum GateResult {
    /// <summary>Auto Correction applies.</summary>
    Applies,

    /// <summary>The user turned Auto Correction off.</summary>
    Off,

    /// <summary>The user's Pause has not ended yet.</summary>
    Paused,

    /// <summary>Rekey paused itself after repeated keyboard failures.</summary>
    SelfPaused,

    /// <summary>The focused window belongs to an Excluded App.</summary>
    ExcludedApp,

    /// <summary>The focused control is a Password Field, or is unknown and may be one.</summary>
    PasswordField,

    /// <summary>The focused window's process has higher privileges than Rekey.</summary>
    Elevated,

    /// <summary>
    /// The user switched Layout themselves during the Word, and the setting to skip such Words
    /// is on.
    /// </summary>
    LayoutSwitchedByUser,
  }
}
