namespace Rekey.Win32.Hotkeys {
  /// <summary>
  /// Specifies what a Hotkey would take over.
  /// </summary>
  public enum HotkeyConflict {
    /// <summary>Nothing: the Hotkey is free.</summary>
    None,

    /// <summary>A Windows shortcut that switches Layout.</summary>
    WindowsLayoutSwitch,

    /// <summary>A hotkey another running application has registered.</summary>
    TakenByAnotherApp,
  }
}
