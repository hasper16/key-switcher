using Rekey.Core.Languages;
using Rekey.Win32.Focus;
using Windows.Win32;

namespace Rekey.Win32.Layouts {
  /// <summary>
  /// Reads and requests the Layout of the window that receives typing.
  /// </summary>
  /// <remarks>
  /// Reading the focused window's thread covers both Windows Layout modes: one Layout for all
  /// windows and a Layout per window.
  /// </remarks>
  internal sealed class ForegroundLayout : ISwitchTarget {
    private readonly LayoutList _layouts;

    public ForegroundLayout(LayoutList layouts) {
      _layouts = layouts;
    }

    /// <inheritdoc />
    public LayoutId? CurrentLayout() {
      if (FocusTarget.Read() is not { } target) {
        return null;
      }

      nint handle = PInvoke.GetKeyboardLayout(target.ThreadId);
      return _layouts.LayoutOf((uint)handle);
    }

    /// <inheritdoc />
    public void RequestLayout(LayoutId layout) {
      if (FocusTarget.Read() is { } target && _layouts.HandleOf(layout) is { } handle) {
        // A 64-bit HKL is its 32-bit value sign-extended.
        PInvoke.PostMessage(
            target.Window, PInvoke.WM_INPUTLANGCHANGEREQUEST, default, (nint)(int)handle);
      }
    }
  }
}
