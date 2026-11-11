using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;

namespace Rekey.Win32.Replacement {
  /// <summary>
  /// Reads and selects the text before the caret through UI Automation's TextPattern (design
  /// D3).
  /// </summary>
  /// <remarks>
  /// Create and call it on the worker thread only. Every call is bounded: UI Automation's own
  /// timeouts stop a call to a hung app, and the reader gives up once 50 ms are spent in all.
  /// </remarks>
  internal sealed class UiaTextBeforeCaret : ITextBeforeCaret {
    // Design D3: the whole check takes at most this long, so a Correction lands well before the
    // hook's 150 ms valve.
    private const uint _budgetMilliseconds = 50;

    // UIA_E_TIMEOUT: the app did not answer within the timeout.
    private const int _timeoutResult = unchecked((int)0x80131505);

    private readonly IUIAutomation? _automation = CreateAutomation();
    private IUIAutomationTextRange? _read;

    // The control the latest Read looked at, and the range SelectRead asked it to select.
    private IUIAutomationElement? _element;
    private IUIAutomationTextRange? _selected;

    // When the latest Read began; Read and SelectRead share one budget.
    private long _started;

    /// <inheritdoc />
    public bool TimedOut { get; private set; }

    /// <inheritdoc />
    public bool SelectionPending { get; private set; }

    /// <inheritdoc />
    public string? Read(int length) {
      _read = null;
      _element = null;
      _selected = null;
      TimedOut = false;
      SelectionPending = false;
      if (_automation is null) {
        return null;
      }

      _started = Stopwatch.GetTimestamp();
      try {
        _element = _automation.GetFocusedElement();
        IUIAutomationTextRange? caret = Caret(_element);
        if (caret is null || OverBudget()) {
          return null;
        }

        IUIAutomationTextRange range = caret.Clone();
        range.MoveEndpointByUnit(
            TextPatternRangeEndpoint.TextPatternRangeEndpoint_Start,
            TextUnit.TextUnit_Character,
            -length);
        string text = TextOf(range, length);
        if (OverBudget()) {
          return null;
        }

        _read = range;
        return text;
      } catch (COMException e) {
        TimedOut = e.HResult == _timeoutResult;
        return null;
      }
    }

    /// <inheritdoc />
    public bool SelectRead() {
      IUIAutomationTextRange? range = _read;
      _read = null;
      if (range is null || OverBudget()) {
        return false;
      }

      _selected = range;
      try {
        range.Select();
        return true;
      } catch (COMException e) {
        TimedOut = e.HResult == _timeoutResult;
        SelectionPending = TimedOut;
        return false;
      }
    }

    /// <inheritdoc />
    public void Deselect(Deselection when) {
      IUIAutomationTextRange? selected = _selected;
      _selected = null;
      if (selected is null || _element is null) {
        return;
      }

      try {
        if (when == Deselection.IfStillSelected && !IsSelection(_element, selected)) {
          return;
        }

        IUIAutomationTextRange end = selected.Clone();
        end.MoveEndpointByRange(
            TextPatternRangeEndpoint.TextPatternRangeEndpoint_Start,
            end,
            TextPatternRangeEndpoint.TextPatternRangeEndpoint_End);
        end.Select();
      } catch (COMException) {
        // An app that cannot answer keeps its selection; nothing else is safe to send.
      }
    }

    /// <inheritdoc />
    public bool FocusUnchanged() {
      if (_automation is null || _element is null) {
        return true;
      }

      // Bounded by the same 50 ms timeouts; an app that cannot answer counts as moved.
      try {
        return _automation.CompareElements(_automation.GetFocusedElement(), _element);
      } catch (COMException) {
        return false;
      }
    }

    private static IUIAutomation? CreateAutomation() {
      try {
        var automation = (IUIAutomation)new CUIAutomation();
        if (OperatingSystem.IsWindowsVersionAtLeast(8)
            && automation is IUIAutomation2 limited) {
          limited.ConnectionTimeout = _budgetMilliseconds;
          limited.TransactionTimeout = _budgetMilliseconds;
        }

        return automation;
      } catch (COMException) {
        return null;
      }
    }

    private static bool IsSelection(IUIAutomationElement element, IUIAutomationTextRange range) {
      if (element.GetCurrentPattern(UIA_PATTERN_ID.UIA_TextPatternId)
          is not IUIAutomationTextPattern text
          || text.GetSelection() is not { Length: 1 } selection) {
        return false;
      }

      IUIAutomationTextRange current = selection.GetElement(0);
      return current.CompareEndpoints(
              TextPatternRangeEndpoint.TextPatternRangeEndpoint_Start,
              range,
              TextPatternRangeEndpoint.TextPatternRangeEndpoint_Start) == 0
          && current.CompareEndpoints(
              TextPatternRangeEndpoint.TextPatternRangeEndpoint_End,
              range,
              TextPatternRangeEndpoint.TextPatternRangeEndpoint_End) == 0;
    }

    // The caret as an empty range; a control with a selection or several carets is retyped.
    private static IUIAutomationTextRange? Caret(IUIAutomationElement element) {
      object? pattern = element.GetCurrentPattern(UIA_PATTERN_ID.UIA_TextPatternId);
      if (pattern is not IUIAutomationTextPattern text
          || text.GetSelection() is not { Length: 1 } selection) {
        return null;
      }

      IUIAutomationTextRange caret = selection.GetElement(0);
      int width = caret.CompareEndpoints(
          TextPatternRangeEndpoint.TextPatternRangeEndpoint_Start,
          caret,
          TextPatternRangeEndpoint.TextPatternRangeEndpoint_End);
      return width == 0 ? caret : null;
    }

    private static string TextOf(IUIAutomationTextRange range, int length) {
      BSTR text = range.GetText(length);
      try {
        return text.ToString() ?? string.Empty;
      } finally {
        Marshal.FreeBSTR((nint)text);
      }
    }

    private bool OverBudget() {
      TimedOut = Stopwatch.GetElapsedTime(_started).TotalMilliseconds > _budgetMilliseconds;
      return TimedOut;
    }
  }
}
