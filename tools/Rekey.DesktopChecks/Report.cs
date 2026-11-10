namespace Rekey.DesktopChecks {
  // One line per checklist item. Details carry counts, kinds and codes only, never typed text.
  internal sealed class Report {
    private readonly List<(string Item, bool Passed)> _results = [];

    public bool Failed => _results.Exists(result => !result.Passed);

    public void Add(string item, bool passed, string detail) {
      _results.Add((item, passed));
      Console.WriteLine($"{(passed ? "✅" : "❌")} {item}: {detail}");
    }

    public void PrintSummary() {
      int passed = _results.Count(result => result.Passed);
      Console.WriteLine();
      Console.WriteLine($"{passed} of {_results.Count} checks passed.");
      foreach ((string item, _) in _results.Where(result => !result.Passed)) {
        Console.WriteLine($"  failed: {item}");
      }
    }
  }
}
