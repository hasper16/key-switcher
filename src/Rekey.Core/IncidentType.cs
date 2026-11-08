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
  }
}
