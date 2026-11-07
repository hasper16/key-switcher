namespace Rekey.Core {
  /// <summary>
  /// Asks the host to record an Incident.
  /// </summary>
  /// <param name="Incident">The Incident to record.</param>
  public sealed record RecordIncident(Incident Incident) : EngineAction;
}
