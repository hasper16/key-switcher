using Rekey.Core;

namespace Rekey.DesktopChecks {
  // What the worker saw, recorded from its thread and read from the checks' thread.
  internal sealed class EventLog {
    private static readonly TimeSpan _pollPeriod = TimeSpan.FromMilliseconds(50);

    private readonly Lock _gate = new();
    private readonly List<Seen> _events = [];
    private readonly List<IncidentType> _incidents = [];
    private readonly List<DateTimeOffset?> _selfPauses = [];

    // Returns whether the condition held before the timeout.
    public static bool WaitFor(Func<bool> condition, TimeSpan timeout) {
      DateTime deadline = DateTime.UtcNow + timeout;
      while (DateTime.UtcNow < deadline) {
        if (condition()) {
          return true;
        }

        Thread.Sleep(_pollPeriod);
      }

      return condition();
    }

    public Position Mark() {
      lock (_gate) {
        return new Position(_events.Count, _incidents.Count, _selfPauses.Count);
      }
    }

    public void Add(InputEvent inputEvent, int wordKeys) {
      lock (_gate) {
        _events.Add(new Seen(inputEvent, wordKeys));
      }
    }

    public void AddIncident(Incident incident) {
      lock (_gate) {
        _incidents.Add(incident.Type);
      }
    }

    public void AddSelfPause(DateTimeOffset? until) {
      lock (_gate) {
        _selfPauses.Add(until);
      }
    }

    public List<Seen> EventsSince(Position mark) {
      lock (_gate) {
        return _events[mark.Events..];
      }
    }

    public List<IncidentType> IncidentsSince(Position mark) {
      lock (_gate) {
        return _incidents[mark.Incidents..];
      }
    }

    public List<DateTimeOffset?> SelfPausesSince(Position mark) {
      lock (_gate) {
        return _selfPauses[mark.SelfPauses..];
      }
    }

    public List<KeyDown> KeyDownsSince(Position mark) {
      return [.. EventsSince(mark).Select(seen => seen.Event).OfType<KeyDown>()];
    }

    internal readonly record struct Position(int Events, int Incidents, int SelfPauses);

    internal sealed record Seen(InputEvent Event, int WordKeys);
  }
}
