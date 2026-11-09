using Rekey.Core;
using Rekey.Core.Languages;

namespace Rekey.Win32.Layouts {
  /// <summary>
  /// Requests a Layout Switch and confirms it took effect within 500 ms.
  /// </summary>
  /// <remarks>
  /// The worker calls <see cref="Check" /> every 25 ms while a switch is pending, so it never
  /// blocks waiting. Not thread-safe: the worker thread owns it.
  /// </remarks>
  internal sealed class LayoutSwitcher {
    // Design decision (spec layout-management: "Verified Layout Switch").
    private static readonly TimeSpan _deadline = TimeSpan.FromMilliseconds(500);

    private readonly ISwitchTarget _window;
    private readonly TimeProvider _time;
    private LayoutId? _target;
    private long _requestedAt;

    public LayoutSwitcher(ISwitchTarget window, TimeProvider time) {
      _window = window;
      _time = time;
    }

    /// <summary>Gets a value indicating whether a switch waits for confirmation.</summary>
    public bool IsPending => _target is not null;

    /// <summary>Requests a switch of the window to a Layout.</summary>
    public void Switch(LayoutId target) {
      _window.RequestLayout(target);
      _target = target;
      _requestedAt = _time.GetTimestamp();
    }

    /// <summary>
    /// Returns how the pending switch ended, or <see langword="null" /> while it may still land
    /// or when none is pending.
    /// </summary>
    public SwitchResult? Check() {
      if (_target is not { } target) {
        return null;
      }

      LayoutId? current = _window.CurrentLayout();
      if (current == target) {
        _target = null;
        return new SwitchResult(target, null);
      }

      if (_time.GetElapsedTime(_requestedAt) < _deadline) {
        return null;
      }

      _target = null;
      var incident = new Incident(_time.GetUtcNow(), IncidentType.SwitchNotLanded, []);
      return new SwitchResult(current, incident);
    }
  }
}
