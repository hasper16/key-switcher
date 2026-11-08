namespace Rekey.Core.Languages {
  /// <summary>
  /// Holds the outcome of loading Language Profiles: the usable ones and an Incident for each one
  /// skipped.
  /// </summary>
  /// <param name="Languages">The profiles that loaded.</param>
  /// <param name="Incidents">An Incident for each profile that could not be loaded.</param>
  public sealed record LanguageSetLoad(LanguageSet Languages, IReadOnlyList<Incident> Incidents);
}
