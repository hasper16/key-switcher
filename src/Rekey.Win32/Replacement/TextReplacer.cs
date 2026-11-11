using System.Globalization;
using Rekey.Core;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace Rekey.Win32.Replacement {
  /// <summary>
  /// Executes <see cref="ReplaceText" />: checks the text before the caret where it can, then
  /// erases or selects it and types the replacement in one <c>SendInput</c> call.
  /// </summary>
  /// <remarks>
  /// Not thread-safe: the worker thread owns it.
  /// </remarks>
  internal sealed class TextReplacer {
    private readonly ITextBeforeCaret _reader;
    private readonly bool _verify;
    private readonly Func<IReadOnlyList<VIRTUAL_KEY>> _heldModifiers;
    private readonly Func<INPUT[], bool> _send;
    private readonly SlowApps _slowApps = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="TextReplacer" /> class.
    /// </summary>
    /// <param name="reader">The focused control's text.</param>
    /// <param name="verify">
    /// <see langword="true" /> to check the text before replacing it (setting Verified
    /// Replacement); otherwise, <see langword="false" />.
    /// </param>
    /// <param name="heldModifiers">Returns the Ctrl, Alt and Windows keys down now.</param>
    /// <param name="send">
    /// Sends inputs to Windows; returns <see langword="false" /> unless Windows took them all.
    /// </param>
    public TextReplacer(
        ITextBeforeCaret reader,
        bool verify,
        Func<IReadOnlyList<VIRTUAL_KEY>> heldModifiers,
        Func<INPUT[], bool> send) {
      _reader = reader;
      _verify = verify;
      _heldModifiers = heldModifiers;
      _send = send;
    }

    /// <summary>
    /// Replaces the text before the caret.
    /// </summary>
    /// <param name="replace">What to replace and with what.</param>
    /// <param name="appId">The app that has focus.</param>
    /// <returns>Whether and how the text was replaced.</returns>
    public ReplacementResult Replace(ReplaceText replace, string appId) {
      ITextBeforeCaret? reader = _verify && !_slowApps.IsSlow(appId) ? _reader : null;
      ReplacementMethod method = VerifiedReplacement.Prepare(reader, replace.Expected);
      if (reader is { TimedOut: true }) {
        _slowApps.RecordTimeout(appId);
      }

      if (method == ReplacementMethod.Cancel) {
        return ReplacementResult.Mismatch;
      }

      // One character per Keystroke in the supported Layouts, so one Backspace each.
      bool retype = method == ReplacementMethod.Retype;
      int erase = retype ? new StringInfo(replace.Expected).LengthInTextElements : 0;
      if (!_send(InputBuilder.Replacement(erase, replace.Replacement, _heldModifiers()))) {
        return ReplacementResult.Blocked;
      }

      return retype ? ReplacementResult.Retyped : ReplacementResult.Verified;
    }
  }
}
