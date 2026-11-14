namespace Rekey.Core {
  /// <summary>
  /// Tells the host that the Protected Words or Learned Words changed, so it saves a snapshot of
  /// <see cref="Engine.Words" />.
  /// </summary>
  public sealed record WordListsChanged : EngineAction;
}
