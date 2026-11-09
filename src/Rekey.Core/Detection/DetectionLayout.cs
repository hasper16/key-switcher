namespace Rekey.Core.Detection {
  /// <summary>
  /// Pairs an active Layout's Key Map with the Language Profile of the Language it types.
  /// </summary>
  /// <param name="KeyMap">What the Layout's keys type.</param>
  /// <param name="Profile">The profile of the Language the Layout types.</param>
  public sealed record DetectionLayout(KeyMap KeyMap, Languages.LanguageProfile Profile);
}
