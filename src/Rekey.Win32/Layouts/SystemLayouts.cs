using System.Globalization;
using Microsoft.Win32;
using Rekey.Core.Languages;
using Windows.Win32;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace Rekey.Win32.Layouts {
  /// <summary>
  /// Reads the Layout handles and registry values of this Windows session.
  /// </summary>
  internal sealed class SystemLayouts : ILayoutRegistry {
    private const string _systemLayouts = @"SYSTEM\CurrentControlSet\Control\Keyboard Layouts";

    private SystemLayouts() {
      Preload = ReadValues(@"Keyboard Layout\Preload")
          .OrderBy(pair => int.TryParse(pair.Key, CultureInfo.InvariantCulture, out int n) ? n : 0)
          .Select(pair => pair.Value)
          .ToList();
      Substitutes = ReadValues(@"Keyboard Layout\Substitutes")
          .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
      VariantIds = ReadVariantIds();
    }

    /// <inheritdoc />
    public IReadOnlyList<string> Preload { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Substitutes { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<ushort, LayoutId> VariantIds { get; }

    /// <summary>Reads the registry values now.</summary>
    public static SystemLayouts Read() {
      return new SystemLayouts();
    }

    /// <summary>Returns the handles of the Layouts loaded for the user, in list order.</summary>
    public static IReadOnlyList<uint> ReadHandles() {
      Span<HKL> handles = stackalloc HKL[64];
      int count = PInvoke.GetKeyboardLayoutList(handles);
      uint[] result = new uint[count];
      for (int i = 0; i < count; i++) {
        nint handle = handles[i];
        result[i] = (uint)handle;
      }

      return result;
    }

    private static IEnumerable<KeyValuePair<string, string>> ReadValues(string userKey) {
      using RegistryKey? key = Registry.CurrentUser.OpenSubKey(userKey);
      if (key is null) {
        return [];
      }

      return [.. key.GetValueNames()
          .Select(name => KeyValuePair.Create(name, key.GetValue(name) as string ?? string.Empty))];
    }

    private static Dictionary<ushort, LayoutId> ReadVariantIds() {
      var ids = new Dictionary<ushort, LayoutId>();
      using RegistryKey? layouts = Registry.LocalMachine.OpenSubKey(_systemLayouts);
      foreach (string klid in layouts?.GetSubKeyNames() ?? []) {
        using RegistryKey? layout = layouts!.OpenSubKey(klid);
        if (layout?.GetValue("Layout Id") is string text
            && ushort.TryParse(
                text, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out ushort id)
            && LayoutId.TryParse(klid, out LayoutId parsed)) {
          ids.TryAdd(id, parsed);
        }
      }

      return ids;
    }
  }
}
