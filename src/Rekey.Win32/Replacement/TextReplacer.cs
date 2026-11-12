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
    private readonly Func<bool> _isConsole;
    private readonly SlowApps _slowApps = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="TextReplacer" /> class.
    /// </summary>
    /// <param name="reader">The focused control's text.</param>
    /// <param name="verify">
    /// <see langword="true" /> to check the text before replacing it (setting Verified
    /// Replacement); otherwise, <see langword="false" />.
    /// </param>
    /// <param name="heldModifiers">Returns the modifier keys down now.</param>
    /// <param name="send">
    /// Sends inputs to Windows; returns <see langword="false" /> unless Windows took them all.
    /// </param>
    public TextReplacer(
        ITextBeforeCaret reader,
        bool verify,
        Func<IReadOnlyList<VIRTUAL_KEY>> heldModifiers,
        Func<INPUT[], bool> send)
        : this(reader, verify, heldModifiers, send, () => false) {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextReplacer" /> class.
    /// </summary>
    /// <param name="reader">The focused control's text.</param>
    /// <param name="verify">
    /// <see langword="true" /> to check the text before replacing it (setting Verified
    /// Replacement); otherwise, <see langword="false" />.
    /// </param>
    /// <param name="heldModifiers">Returns the modifier keys down now.</param>
    /// <param name="send">
    /// Sends inputs to Windows; returns <see langword="false" /> unless Windows took them all.
    /// </param>
    /// <param name="isConsole">
    /// Returns whether the focused window is a console, where nothing is replaced.
    /// </param>
    public TextReplacer(
        ITextBeforeCaret reader,
        bool verify,
        Func<IReadOnlyList<VIRTUAL_KEY>> heldModifiers,
        Func<INPUT[], bool> send,
        Func<bool> isConsole) {
      _reader = reader;
      _verify = verify;
      _heldModifiers = heldModifiers;
      _send = send;
      _isConsole = isConsole;
    }

    /// <summary>
    /// Replaces the text before the caret.
    /// </summary>
    /// <param name="replace">What to replace and with what.</param>
    /// <param name="appId">The app that has focus.</param>
    /// <returns>Whether and how the text was replaced.</returns>
    public ReplacementResult Replace(ReplaceText replace, string appId) {
      return Replace(replace, appId, () => null);
    }

    /// <summary>
    /// Replaces the text before the caret, unless the request turned stale while the text was
    /// checked.
    /// </summary>
    /// <param name="replace">What to replace and with what.</param>
    /// <param name="appId">The app that has focus.</param>
    /// <param name="recheck">
    /// Called just before the input is sent; returns why the request is no longer valid, or
    /// <see langword="null" /> when it still is.
    /// </param>
    /// <returns>Whether and how the text was replaced.</returns>
    public ReplacementResult Replace(
        ReplaceText replace, string appId, Func<ReplacementResult?> recheck) {
      // A console took the Backspaces but dropped the typed Rendering on the owner's desktop,
      // deleting the Word, and its UI Automation selection is the terminal's own; until a way
      // in is proven there, a console is left as it is.
      if (_isConsole()) {
        return ReplacementResult.Unsupported;
      }

      ITextBeforeCaret? reader = _verify && !_slowApps.IsSlow(appId) ? _reader : null;
      ReplacementResult? staleBeforeSelect = null;
      Func<bool> stillValid = () => {
        staleBeforeSelect = recheck();
        return staleBeforeSelect is null;
      };
      ReplacementMethod method = VerifiedReplacement.Prepare(
          reader, replace.Expected, stillValid, canSelect: true);
      if (reader is { TimedOut: true }) {
        _slowApps.RecordTimeout(appId);
      }

      switch (method) {
        case ReplacementMethod.Cancel:
          return ReplacementResult.Mismatch;
        case ReplacementMethod.Stale:
          return staleBeforeSelect ?? ReplacementResult.TooLate;
        case ReplacementMethod.Abandon:
          // The app handles this request after the pending selection, in the control that was
          // read, so no key can reach another control.
          _reader.Deselect(Deselection.Always);
          return ReplacementResult.TimedOut;
        default:
          break;
      }

      // Focus can move inside the window while the text is checked; Backspaces would then
      // erase in another control.
      ReplacementResult? stale = reader is not null && !reader.FocusUnchanged()
          ? ReplacementResult.FocusMoved
          : recheck();
      if (stale is not null) {
        if (method == ReplacementMethod.SelectAndType) {
          Collapse(stale.Value);
        }

        return stale.Value;
      }

      // One character per Keystroke in the supported Layouts, so one Backspace each.
      bool retype = method == ReplacementMethod.Retype;
      int erase = retype ? new StringInfo(replace.Expected).LengthInTextElements : 0;
      if (!_send(InputBuilder.Replacement(erase, replace.Replacement, _heldModifiers()))) {
        // Windows refuses input to this window, so only UI Automation can clear the selection.
        if (!retype) {
          _reader.Deselect(Deselection.IfStillSelected);
        }

        return ReplacementResult.Blocked;
      }

      return retype ? ReplacementResult.Retyped : ReplacementResult.Verified;
    }

    // Collapses a selection so neither a re-injected Space nor later typing replaces the Word.
    // After a click or a focus move, Right would land elsewhere, so the control that was read
    // is collapsed through UI Automation, and only while its selection is still the Word: a
    // caret the click placed stays where it is.
    private void Collapse(ReplacementResult why) {
      if (why is ReplacementResult.Interrupted or ReplacementResult.FocusMoved) {
        _reader.Deselect(Deselection.IfStillSelected);
      } else {
        _send(InputBuilder.CollapseSelection(_heldModifiers()));
      }
    }
  }
}
