namespace Rekey.Win32.Replacement {
  /// <summary>
  /// Describes the control a text check read, without its text.
  /// </summary>
  /// <param name="Framework">The UI Automation framework ID, such as Win32, Chrome or Qt.</param>
  /// <param name="ClassName">The control's class name.</param>
  /// <param name="ControlType">The UI Automation control type ID.</param>
  /// <param name="Moved">
  /// How many characters the start of the read range moved back from the caret; less than
  /// asked when the control has less text before the caret.
  /// </param>
  public sealed record ReaderFacts(string Framework, string ClassName, int ControlType, int Moved);
}
