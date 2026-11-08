namespace Rekey.ProfileBuilder {
  /// <summary>
  /// Generates the Language Profiles from the sources folder (see docs/language-profiles.md).
  /// </summary>
  internal static class Program {
    private const int _usageError = 2;

    private static int Main(string[] args) {
      if (args.Length != 2) {
        Console.Error.WriteLine("Usage: Rekey.ProfileBuilder <sources folder> <output folder>");
        return _usageError;
      }

      return BuildCommand.Run(args[0], args[1], Console.Out, Console.Error);
    }
  }
}
