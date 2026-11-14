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
    /// <summary>
    /// The wait before each part of a text that follows a space (design D4 of
    /// add-manual-correction).
    /// </summary>
    public static readonly TimeSpan PauseAfterSpace = TimeSpan.FromMilliseconds(50);

    private readonly ITextBeforeCaret _reader;
    private readonly Action<TimeSpan> _pause;
    private readonly bool _verify;
    private readonly Func<IReadOnlyList<VIRTUAL_KEY>> _heldModifiers;
    private readonly Func<INPUT[], bool> _send;
    private readonly Func<TextTarget> _target;
    private readonly AppStrikes _slowApps = new();
    private readonly AppStrikes _unreadableApps = new();

    // The text the latest replacement expected, when what was read differed from it.
    private string? _mismatchExpected;
    private MismatchKind _mismatchKind;

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
        : this(reader, verify, heldModifiers, send, () => TextTarget.Editable) {
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
    /// <param name="target">Returns how the focused window takes a replacement.</param>
    /// <param name="pause">
    /// Waits between the parts of a text with spaces, or <see langword="null" /> to sleep the
    /// calling thread. The default is <see langword="null" />.
    /// </param>
    public TextReplacer(
        ITextBeforeCaret reader,
        bool verify,
        Func<IReadOnlyList<VIRTUAL_KEY>> heldModifiers,
        Func<INPUT[], bool> send,
        Func<TextTarget> target,
        Action<TimeSpan>? pause = null) {
      _reader = reader;
      _verify = verify;
      _heldModifiers = heldModifiers;
      _send = send;
      _target = target;
      _pause = pause ?? Thread.Sleep;
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
      // Never erase where the typed Rendering is not proven to arrive: that deletes the Word.
      TextTarget target = _target();
      if (target == TextTarget.Unsupported) {
        return ReplacementResult.Unsupported;
      }

      _mismatchExpected = null;
      bool check = _verify
          && !_slowApps.HasStruckOut(appId)
          && !_unreadableApps.HasStruckOut(appId);
      ITextBeforeCaret? reader = check ? _reader : null;
      ReplacementResult? staleBeforeSelect = null;
      Func<bool> stillValid = () => {
        staleBeforeSelect = recheck();
        return staleBeforeSelect is null;
      };
      ReplacementMethod method = VerifiedReplacement.Prepare(
          reader,
          replace.Expected,
          stillValid,
          canSelect: target == TextTarget.Editable,
          out MismatchKind mismatch);
      if (reader is { TimedOut: true }) {
        _slowApps.Record(appId);
      }

      switch (method) {
        case ReplacementMethod.Cancel:
          (_mismatchExpected, _mismatchKind) = (replace.Expected, mismatch);
          return ReplacementResult.Mismatch;
        case ReplacementMethod.Unrelated:
          (_mismatchExpected, _mismatchKind) = (replace.Expected, mismatch);
          _unreadableApps.Record(appId);
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
      if (!SendInParts(erase, replace.Replacement)) {
        // Windows refuses input to this window, so only UI Automation can clear the selection.
        if (!retype) {
          _reader.Deselect(Deselection.IfStillSelected);
        }

        return ReplacementResult.Blocked;
      }

      return retype ? ReplacementResult.Retyped : ReplacementResult.Verified;
    }

    /// <summary>
    /// Types a text over the selection the host read, so the selection is replaced without
    /// erasing anything first (design D4 of add-manual-correction).
    /// </summary>
    /// <param name="typeOver">The selected text and the text to type over it.</param>
    /// <param name="recheck">
    /// Returns why the replacement can no longer happen, or <see langword="null" /> if it still
    /// can; asked just before typing.
    /// </param>
    /// <returns>One of the enumeration values that specifies what became of it.</returns>
    public ReplacementResult TypeOver(
        TypeOverSelection typeOver, Func<ReplacementResult?> recheck) {
      if (_target() == TextTarget.Unsupported) {
        return ReplacementResult.Unsupported;
      }

      // The app may have moved focus inside the window since the selection was read, or the
      // selection may have changed in the same control.
      if (!_reader.FocusUnchanged()) {
        return ReplacementResult.FocusMoved;
      }

      if (!_reader.SelectionIs(typeOver.Selected)) {
        return ReplacementResult.Mismatch;
      }

      if (recheck() is { } stale) {
        return stale;
      }

      return SendInParts(0, typeOver.Replacement)
          ? ReplacementResult.Verified
          : ReplacementResult.Blocked;
    }

    /// <summary>
    /// Describes how the text read in the latest replacement differed from the Word, without
    /// either text; call it after the held keys went on, as it asks the app for more.
    /// </summary>
    /// <returns>
    /// The comparison, or <see langword="null" /> when the latest replacement did not differ.
    /// </returns>
    public ReplacementTrace? DescribeMismatch() {
      return _mismatchExpected is { } expected
          ? _reader.Describe(expected) with { Kind = _mismatchKind }
          : null;
    }

    // Design D4 of add-manual-correction: each part after a space waits a moment, as Notepad
    // garbles what follows a space in the same burst. Stops at the first part Windows refuses.
    private bool SendInParts(int erase, string text) {
      IReadOnlyList<INPUT[]> parts = InputBuilder.ReplacementParts(erase, text, _heldModifiers());
      for (int i = 0; i < parts.Count; i++) {
        if (i > 0) {
          _pause(PauseAfterSpace);
        }

        if (!_send(parts[i])) {
          return false;
        }
      }

      return true;
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
