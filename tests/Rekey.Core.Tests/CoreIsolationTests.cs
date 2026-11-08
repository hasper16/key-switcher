using System.Reflection;
using Xunit;

namespace Rekey.Core.Tests {
  public sealed class CoreIsolationTests {
    private static readonly string[] _windowsAssemblyPrefixes =
        ["Microsoft.Windows", "System.Windows"];

    [Fact]
    public void CoreReferencesNoWindowsAssemblies() {
      Assembly core = Assembly.Load(new AssemblyName("Rekey.Core"));

      string[] windowsReferences = core.GetReferencedAssemblies()
          .Select(reference => reference.Name ?? string.Empty)
          .Where(IsWindowsAssembly)
          .ToArray();

      Assert.Empty(windowsReferences);
    }

    private static bool IsWindowsAssembly(string name) {
      return _windowsAssemblyPrefixes.Any(
          prefix => name.StartsWith(prefix, StringComparison.Ordinal));
    }
  }
}
