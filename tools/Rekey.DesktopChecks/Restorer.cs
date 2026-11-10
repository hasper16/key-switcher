namespace Rekey.DesktopChecks {
  // Undoes every system change the checks make, newest first, even when a check fails.
  internal sealed class Restorer : IDisposable {
    private readonly Stack<(string What, Action Undo)> _undo = new();

    public void Defer(string what, Action undo) {
      _undo.Push((what, undo));
    }

    public void Dispose() {
      while (_undo.TryPop(out (string What, Action Undo) step)) {
        try {
          step.Undo();
          Console.WriteLine($"   restored: {step.What}");
        } catch (Exception e) when (e is not OutOfMemoryException) {
          Console.WriteLine($"❌ could not restore {step.What}: {e.GetType().Name}");
        }
      }
    }
  }
}
