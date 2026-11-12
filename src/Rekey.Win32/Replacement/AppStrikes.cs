namespace Rekey.Win32.Replacement {
  /// <summary>
  /// Remembers for the session which apps failed a text check twice in one way, such as
  /// answering UI Automation too slowly or returning text unrelated to the Word, so their text
  /// is no longer checked (design D3; the original also remembers slow apps).
  /// </summary>
  internal sealed class AppStrikes {
    private const int _strikesOut = 2;

    private readonly Dictionary<string, int> _strikes = new(StringComparer.OrdinalIgnoreCase);

    public bool HasStruckOut(string appId) {
      return _strikes.GetValueOrDefault(appId) >= _strikesOut;
    }

    public void Record(string appId) {
      _strikes[appId] = _strikes.GetValueOrDefault(appId) + 1;
    }
  }
}
