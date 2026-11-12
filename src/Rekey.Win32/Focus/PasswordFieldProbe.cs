using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Rekey.Win32.Focus {
  /// <summary>
  /// Tells whether the focused control is a Password Field.
  /// </summary>
  /// <remarks>
  /// UI Automation covers modern controls, including browser and WPF password boxes; the
  /// <c>ES_PASSWORD</c> style covers classic Edit controls whose UI Automation answer fails.
  /// Create and call it on the worker thread only.
  /// </remarks>
  internal sealed class PasswordFieldProbe {
    // Design decision: long enough for a responsive app, short enough not to stall typing.
    private const uint _timeoutMilliseconds = 200;

    private readonly IUIAutomation? _automation = CreateAutomation();

    /// <summary>
    /// Describes an element by its class name and control type, for diagnostics; never text.
    /// </summary>
    public static string Describe(IUIAutomationElement? element) {
      if (element is null) {
        return "unknown";
      }

      try {
        string className = UiaStrings.Take(element.CurrentClassName);
        return $"{className}/{(int)element.CurrentControlType}";
      } catch (COMException) {
        return "no answer";
      }
    }

    /// <summary>
    /// Returns whether the focused control is a Password Field.
    /// </summary>
    /// <param name="focus">The focused window.</param>
    /// <returns>
    /// <see langword="true" /> if it is one, or if UI Automation cannot tell and the Edit style
    /// does not settle it: an unknown answer is treated as a Password Field.
    /// </returns>
    public bool IsPassword(HWND focus) {
      return IsPasswordEdit(focus) || (AutomationSaysPassword() ?? true);
    }

    /// <summary>
    /// Returns the focused UI Automation element, or <see langword="null" /> when UI Automation
    /// cannot tell.
    /// </summary>
    public IUIAutomationElement? FocusedElement() {
      try {
        return _automation?.GetFocusedElement();
      } catch (COMException) {
        return null;
      }
    }

    /// <summary>
    /// Returns whether two elements are the same control; an unknown element never matches.
    /// </summary>
    public bool Same(IUIAutomationElement? first, IUIAutomationElement? second) {
      return Compare(first, second) == true;
    }

    /// <summary>
    /// Returns whether two elements are the same control, or <see langword="null" /> when an
    /// element is unknown or the app cannot answer.
    /// </summary>
    public bool? Compare(IUIAutomationElement? first, IUIAutomationElement? second) {
      if (_automation is null || first is null || second is null) {
        return null;
      }

      try {
        return _automation.CompareElements(first, second);
      } catch (COMException) {
        return null;
      }
    }

    private static IUIAutomation? CreateAutomation() {
      try {
        var automation = (IUIAutomation)new CUIAutomation();

        // The query runs on the worker; a hung app must not stall typing for the 20 s default.
        if (OperatingSystem.IsWindowsVersionAtLeast(8)
            && automation is IUIAutomation2 limited) {
          limited.ConnectionTimeout = _timeoutMilliseconds;
          limited.TransactionTimeout = _timeoutMilliseconds;
        }

        return automation;
      } catch (COMException) {
        return null;
      }
    }

    private static bool IsPasswordEdit(HWND window) {
      Span<char> name = stackalloc char[64];
      int length = PInvoke.GetClassName(window, name);
      bool isEdit = name[..Math.Max(length, 0)]
          .Contains("EDIT".AsSpan(), StringComparison.OrdinalIgnoreCase);
      int style = PInvoke.GetWindowLong(window, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
      return isEdit && (style & PInvoke.ES_PASSWORD) != 0;
    }

    private bool? AutomationSaysPassword() {
      if (_automation is null) {
        return null;
      }

      try {
        return _automation.GetFocusedElement().CurrentIsPassword;
      } catch (COMException) {
        // The focused app did not answer in time, or cannot answer at all.
        return null;
      }
    }
  }
}
