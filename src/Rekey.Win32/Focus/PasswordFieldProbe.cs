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
    private readonly IUIAutomation? _automation = CreateAutomation();

    /// <summary>
    /// Returns whether the focused control is a Password Field.
    /// </summary>
    /// <param name="focus">The focused window.</param>
    public bool IsPassword(HWND focus) {
      return AutomationSaysPassword() || IsPasswordEdit(focus);
    }

    private static IUIAutomation? CreateAutomation() {
      try {
        return (IUIAutomation)new CUIAutomation();
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

    private bool AutomationSaysPassword() {
      if (_automation is null) {
        return false;
      }

      try {
        return _automation.GetFocusedElement().CurrentIsPassword;
      } catch (COMException) {
        // The focused app may not answer UI Automation; the Edit style check still runs.
        return false;
      }
    }
  }
}
