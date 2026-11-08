using Rekey.Core.Detection;
using Rekey.Core.Languages;

namespace Rekey.Core.Tests.Detection {
  // Key Maps captured from Windows with tools/Rekey.KeyMapCapture.
  internal static class KeyMapFixtures {
    public static readonly string[] Klids = ["00000409", "00000419", "00000422", "00020422"];

    public static KeyMap EnglishUs { get; } = Load("00000409");

    public static KeyMap Russian { get; } = Load("00000419");

    public static KeyMap Ukrainian { get; } = Load("00000422");

    public static KeyMap UkrainianEnhanced { get; } = Load("00020422");

    public static string PathOf(string klid) {
      return Path.Join(AppContext.BaseDirectory, "fixtures", "keymaps", $"{klid}.json");
    }

    public static KeyMap Load(string klid) {
      using FileStream stream = File.OpenRead(PathOf(klid));
      return KeyMapJson.Read(stream);
    }

    public static LayoutId Id(string klid) {
      return LayoutId.TryParse(klid, out LayoutId id) ? id : throw new ArgumentException(klid);
    }
  }
}
