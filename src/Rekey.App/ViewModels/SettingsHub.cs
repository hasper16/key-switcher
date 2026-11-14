using Rekey.Core;

namespace Rekey.App.ViewModels {
  // The one place the app's settings change (design D4): the tray and the settings window
  // update it on the UI thread, the engine and the views follow Changed, and the file is written
  // 500 ms after the last change.
  internal sealed class SettingsHub : IDisposable {
    private static readonly TimeSpan _saveDelay = TimeSpan.FromMilliseconds(500);

    private readonly Action<Settings> _save;
    private readonly ITimer _timer;

    // The timer's thread and a flush may save at the same moment.
    private readonly Lock _saveLock = new();

    // Written only on the UI thread; the timer's thread reads _unsaved.
    private Settings _current;
    private Settings? _unsaved;

    public SettingsHub(Settings initial, Action<Settings> save, TimeProvider time) {
      _current = initial;
      _save = save;
      _timer = time.CreateTimer(
          _ => Save(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    public event Action<Settings>? Changed;

    public Settings Current => _current;

    public void Update(Func<Settings, Settings> change) {
      Settings next = change(_current);
      if (next == _current) {
        return;
      }

      _current = next;
      Volatile.Write(ref _unsaved, next);
      _timer.Change(_saveDelay, Timeout.InfiniteTimeSpan);
      Changed?.Invoke(next);
    }

    public void Flush() {
      _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
      Save();
    }

    public void Dispose() {
      _timer.Dispose();
    }

    private void Save() {
      lock (_saveLock) {
        if (Interlocked.Exchange(ref _unsaved, null) is { } settings) {
          _save(settings);
        }
      }
    }
  }
}
