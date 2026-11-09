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
  }
}
