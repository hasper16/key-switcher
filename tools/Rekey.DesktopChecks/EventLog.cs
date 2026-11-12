using Rekey.Core;
using Rekey.Win32.Replacement;

namespace Rekey.DesktopChecks {
  // What the worker saw, recorded from its thread and read from the checks' thread.
  internal sealed class EventLog {
    private static readonly TimeSpan _pollPeriod = TimeSpan.FromMilliseconds(50);

    private readonly Lock _gate = new();
    private readonly List<Seen> _events = [];
    private readonly List<IncidentType> _incidents = [];
    private readonly List<DateTimeOffset?> _selfPauses = [];
    private readonly List<ReplacementResult> _replacements = [];
    private int _undos;

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
        return new Position(
            _events.Count, _incidents.Count, _selfPauses.Count, _replacements.Count, _undos);
      }
    }

    public void Add(InputEvent inputEvent, int wordKeys, string? note = null) {
      lock (_gate) {
        _events.Add(new Seen(inputEvent, wordKeys, note));
      }
    }

    public void AddIncident(Incident incident) {
      lock (_gate) {
        _incidents.Add(incident.Type);
      }
    }

    public void AddReplacement(ReplacementResult result) {
      lock (_gate) {
        _replacements.Add(result);
      }
    }

    // Counts an Undo; the texts it carries are never kept.
    public void AddUndo() {
      lock (_gate) {
        _undos++;
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

    public List<ReplacementResult> ReplacementsSince(Position mark) {
      lock (_gate) {
        return _replacements[mark.Replacements..];
      }
    }

    public int UndosSince(Position mark) {
      lock (_gate) {
        return _undos - mark.Undos;
      }
    }

    public List<KeyDown> KeyDownsSince(Position mark) {
      return [.. EventsSince(mark).Select(seen => seen.Event).OfType<KeyDown>()];
    }

    internal readonly record struct Position(
        int Events, int Incidents, int SelfPauses, int Replacements, int Undos);

    // Note: for a focus report, why focus counted as moved (codes and class names only).
    internal sealed record Seen(InputEvent Event, int WordKeys, string? Note = null);
  }
}
