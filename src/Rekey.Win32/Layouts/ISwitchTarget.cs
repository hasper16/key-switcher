using Rekey.Core.Languages;

namespace Rekey.Win32.Layouts {
  /// <summary>
  /// Reads and requests the Layout of the window the user types into.
  /// </summary>
  internal interface ISwitchTarget {
    /// <summary>
    /// Returns the Layout of the window's input thread.
    /// </summary>
    /// <returns>The Layout, or <see langword="null" /> when it cannot be read.</returns>
    LayoutId? CurrentLayout();

    /// <summary>
    /// Asks the window to switch to a Layout; the switch happens later, if at all.
    /// </summary>
    /// <param name="layout">The Layout to switch to.</param>
    void RequestLayout(LayoutId layout);
  }
}
