using Rekey.Core.Detection;

namespace Rekey.Win32.Tests {
  // Key Maps captured from Windows with tools/Rekey.KeyMapCapture.
  internal static class KeyMapFixtures {
    public static readonly string[] Klids = [
        "00000409", "00000419", "00000422", "00020422", "00000809"];

    public static KeyMap EnglishUs { get; } = Load("00000409");

    public static KeyMap Russian { get; } = Load("00000419");

    public static KeyMap Ukrainian { get; } = Load("00000422");

    public static KeyMap Load(string klid) {
      string path = Path.Join(AppContext.BaseDirectory, "fixtures", "keymaps", $"{klid}.json");
      using FileStream stream = File.OpenRead(path);
      return KeyMapJson.Read(stream);
    }
  }
}
