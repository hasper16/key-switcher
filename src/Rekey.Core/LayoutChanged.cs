using Rekey.Core.Languages;

namespace Rekey.Core {
  /// <summary>
  /// Reports that the Layout of the foreground window changed.
  /// </summary>
  /// <param name="Timestamp">The time the change was seen, measured from system start.</param>
  /// <param name="Layout">The Layout now active in the foreground window.</param>
  /// <param name="ByUser">
  /// <see langword="true" /> if the user switched Layout themselves in the window they are
  /// typing in; <see langword="false" /> if Rekey switched it, focus moved to a window with
  /// another Layout, or the cause is unknown. The default is <see langword="false" />.
  /// </param>
  public sealed record LayoutChanged(TimeSpan Timestamp, LayoutId Layout, bool ByUser = false)
      : InputEvent(Timestamp);
}
