namespace Rekey.DesktopChecks {
  // Undoes every system change the checks make, newest first, even when a check fails.
  internal sealed class Restorer : IDisposable {
    private readonly Stack<(string What, Action Undo)> _undo = new();
    private readonly Report _report;

    // A failed restoration fails the run, so a changed setting never goes unnoticed.
    public Restorer(Report report) {
      _report = report;
    }

    public void Defer(string what, Action undo) {
      _undo.Push((what, undo));
    }

    public void Dispose() {
      while (_undo.TryPop(out (string What, Action Undo) step)) {
        try {
          step.Undo();
          Console.WriteLine($"   restored: {step.What}");
        } catch (Exception e) when (e is not OutOfMemoryException) {
          _report.Add($"restore {step.What}", false, $"failed with {e.GetType().Name}");
          if (step.What == "language list") {
            Console.WriteLine(
                "   run: dotnet run --project tools/Rekey.DesktopChecks -- restore-languages");
          }
        }
      }
    }
  }
}
