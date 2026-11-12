namespace Rekey.Win32.Focus {
  /// <summary>
  /// Specifies whether the input that could have moved focus was typing (design D7).
  /// </summary>
  internal enum Typing {
    /// <summary>A non-typing key or a click may have moved focus.</summary>
    No,

    /// <summary>Only typing keys could have moved focus.</summary>
    Yes,

    /// <summary>Later input is still waiting to be read, so the cause is not known yet.</summary>
    Waiting,
  }
}
