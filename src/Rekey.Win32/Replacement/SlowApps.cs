namespace Rekey.Win32.Replacement {
  /// <summary>
  /// Remembers for the session which apps answered UI Automation too slowly, so their text is
  /// no longer checked (design D3; the original also remembers slow apps).
  /// </summary>
  internal sealed class SlowApps {
    private const int _timeoutsToSlow = 2;

    private readonly Dictionary<string, int> _timeouts = new(StringComparer.OrdinalIgnoreCase);

    public bool IsSlow(string appId) {
      return _timeouts.GetValueOrDefault(appId) >= _timeoutsToSlow;
    }

    public void RecordTimeout(string appId) {
      _timeouts[appId] = _timeouts.GetValueOrDefault(appId) + 1;
    }
  }
}
