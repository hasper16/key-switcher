using Rekey.Core.Detection;
using Rekey.Core.Languages;
using Rekey.Win32;

namespace Rekey.KeyMapCapture {
  /// <summary>
  /// Captures Key Map test fixtures from the Layouts of this Windows machine.
  /// </summary>
  internal static class Program {
    private const int _usageError = 2;

    private static int Main(string[] args) {
      if (args.Length < 2) {
        Console.Error.WriteLine("Usage: Rekey.KeyMapCapture <output folder> <KLID>...");
        return _usageError;
      }

      foreach (string klid in args[1..]) {
        if (!LayoutId.TryParse(klid, out LayoutId layout)) {
          Console.Error.WriteLine($"Not an 8-digit KLID: {klid}");
          return _usageError;
        }

        string path = Path.Join(args[0], $"{klid.ToUpperInvariant()}.json");
        File.WriteAllText(path, KeyMapJson.Write(KeyMapReader.Read(layout)));
        Console.WriteLine($"Wrote {path}");
      }

      return 0;
    }
  }
}
