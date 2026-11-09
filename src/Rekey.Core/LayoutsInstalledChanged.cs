using Rekey.Core.Languages;

namespace Rekey.Core {
  /// <summary>
  /// Reports the Layouts installed for the user, at start-up and whenever the list changes.
  /// </summary>
  /// <param name="Timestamp">The time the list was read, measured from system start.</param>
  /// <param name="Layouts">The installed Layouts, in Windows' list order.</param>
  public sealed record LayoutsInstalledChanged(TimeSpan Timestamp, IReadOnlyList<LayoutId> Layouts)
      : InputEvent(Timestamp);
}
