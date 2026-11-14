namespace Rekey.Core {
  /// <summary>
  /// Tells the host that a word the user added was refused because its list holds only words
  /// the user added, up to the limit.
  /// </summary>
  public sealed record WordListFull : EngineAction;
}
