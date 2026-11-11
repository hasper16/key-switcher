using Rekey.Core.Languages;

namespace Rekey.Core {
  /// <summary>
  /// Reports that the Layout of the foreground window changed.
  /// </summary>
  /// <param name="Timestamp">The time the change was seen, measured from system start.</param>
  /// <param name="Layout">The Layout now active in the foreground window.</param>
  public sealed record LayoutChanged(TimeSpan Timestamp, LayoutId Layout) : InputEvent(Timestamp);
}
