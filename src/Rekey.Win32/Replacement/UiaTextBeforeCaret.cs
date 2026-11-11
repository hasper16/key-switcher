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

    // When the latest Read began; Read and SelectRead share one budget.
    private long _started;

    /// <inheritdoc />
    public bool TimedOut { get; private set; }

    /// <inheritdoc />
    public string? Read(int length) {
      _read = null;
      TimedOut = false;
      if (_automation is null) {
        return null;
      }

      _started = Stopwatch.GetTimestamp();
      try {
        IUIAutomationTextRange? caret = Caret(_automation);
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

      try {
        range.Select();
        return true;
      } catch (COMException e) {
        TimedOut = e.HResult == _timeoutResult;
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

    // The caret as an empty range; a control with a selection or several carets is retyped.
    private static IUIAutomationTextRange? Caret(IUIAutomation automation) {
      object? pattern = automation.GetFocusedElement()
          .GetCurrentPattern(UIA_PATTERN_ID.UIA_TextPatternId);
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
