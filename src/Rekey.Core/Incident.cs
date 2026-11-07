namespace Rekey.Core {
  /// <summary>
  /// Describes a moment when Rekey lost or recovered its view of the keyboard, or stopped itself
  /// to stay safe.
  /// </summary>
  /// <remarks>
  /// Details are enum values only. With no field for free text, typed text cannot reach an
  /// Incident.
  /// </remarks>
  /// <param name="Time">The time the Incident happened.</param>
  /// <param name="Type">One of the enumeration values that specifies the kind of Incident.</param>
  /// <param name="Details">The enum-coded facts about the Incident.</param>
  public sealed record Incident(
      DateTimeOffset Time, IncidentType Type, IReadOnlyList<Enum> Details);
}
