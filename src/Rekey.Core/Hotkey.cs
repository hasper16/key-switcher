using System.Collections.Frozen;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rekey.Core {
  /// <summary>
  /// Represents the Hotkey: the gesture that asks for a Manual Correction.
  /// </summary>
  /// <remarks>
  /// A double tap names its modifier in <see cref="TappedModifier" />; a combination names its
  /// modifiers and Windows virtual-key code in <see cref="Modifiers" /> and
  /// <see cref="VirtualKey" />. The Space presets are combinations of Space. The default value
  /// is <see cref="DoubleShift" />.
  /// </remarks>
  [JsonConverter(typeof(Converter))]
  public readonly record struct Hotkey {
    /// <summary>
    /// The virtual-key code of Space.
    /// </summary>
    public const ushort SpaceKey = 0x20;

    // The settings file names a preset by a string and spells out a custom combination.
    internal sealed class Converter : JsonConverter<Hotkey> {
      private static readonly (KeyModifiers Modifier, string Name)[] _modifierNames = [
          (KeyModifiers.Shift, "shift"), (KeyModifiers.Control, "control"),
          (KeyModifiers.Alt, "alt"), (KeyModifiers.Windows, "windows")];

      public override Hotkey Read(
          ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType == JsonTokenType.String) {
          return PresetNamed(reader.GetString()!)
              ?? throw new JsonException("Unknown Hotkey preset.");
        }

        if (reader.TokenType != JsonTokenType.StartObject) {
          throw new JsonException("The Hotkey is a preset name or a combination.");
        }

        KeyModifiers modifiers = KeyModifiers.None;
        ushort? key = null;
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
          string property = reader.GetString()!;
          reader.Read();
          if (property == "modifiers") {
            modifiers = ReadModifiers(ref reader);
          } else if (property == "virtualKey") {
            key = reader.GetUInt16();
          } else {
            reader.Skip();
          }
        }

        try {
          return Custom(modifiers, key ?? throw new JsonException("The Hotkey lacks its key."));
        } catch (ArgumentException e) {
          throw new JsonException(e.Message, e);
        }
      }

      public override void Write(
          Utf8JsonWriter writer, Hotkey value, JsonSerializerOptions options) {
        if (value.Kind != HotkeyKind.Custom) {
          writer.WriteStringValue(NameOf(value.Kind));
          return;
        }

        writer.WriteStartObject();
        writer.WriteStartArray("modifiers");
        foreach ((KeyModifiers modifier, string name) in _modifierNames) {
          if ((value.Modifiers & modifier) != 0) {
            writer.WriteStringValue(name);
          }
        }

        writer.WriteEndArray();
        writer.WriteNumber("virtualKey", value.VirtualKey);
        writer.WriteEndObject();
      }

      private static KeyModifiers ReadModifiers(ref Utf8JsonReader reader) {
        if (reader.TokenType != JsonTokenType.StartArray) {
          throw new JsonException("The Hotkey modifiers are an array of names.");
        }

        KeyModifiers modifiers = KeyModifiers.None;
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray) {
          string? name = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
          (KeyModifiers modifier, string _) = _modifierNames
              .FirstOrDefault(entry => entry.Name == name);
          modifiers |= modifier != KeyModifiers.None
              ? modifier
              : throw new JsonException("Unknown Hotkey modifier.");
        }

        return modifiers;
      }

      private static Hotkey? PresetNamed(string name) {
        return name switch {
          "doubleShift" => DoubleShift,
          "doubleCtrl" => DoubleCtrl,
          "doubleAlt" => DoubleAlt,
          "ctrlSpace" => CtrlSpace,
          "winSpace" => WinSpace,
          _ => null,
        };
      }

      private static string NameOf(HotkeyKind kind) {
        string name = kind.ToString();
        return char.ToLowerInvariant(name[0]) + name[1..];
      }
    }

    // Shift, Ctrl, Alt and Win in their generic, left and right codes.
    private static readonly FrozenSet<ushort> _modifierKeys =
        new ushort[] { 0x10, 0x11, 0x12, 0x5B, 0x5C, 0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5 }
            .ToFrozenSet();

    private Hotkey(HotkeyKind kind, KeyModifiers modifiers, ushort virtualKey) {
      Kind = kind;
      Modifiers = modifiers;
      VirtualKey = virtualKey;
    }

    /// <summary>
    /// Gets the double tap of Shift, the default Hotkey.
    /// </summary>
    public static Hotkey DoubleShift { get; } = new(HotkeyKind.DoubleShift, KeyModifiers.None, 0);

    /// <summary>
    /// Gets the double tap of Ctrl.
    /// </summary>
    public static Hotkey DoubleCtrl { get; } = new(HotkeyKind.DoubleCtrl, KeyModifiers.None, 0);

    /// <summary>
    /// Gets the double tap of Alt.
    /// </summary>
    public static Hotkey DoubleAlt { get; } = new(HotkeyKind.DoubleAlt, KeyModifiers.None, 0);

    /// <summary>
    /// Gets Ctrl+Space.
    /// </summary>
    public static Hotkey CtrlSpace { get; } =
        new(HotkeyKind.CtrlSpace, KeyModifiers.Control, SpaceKey);

    /// <summary>
    /// Gets Win+Space, which replaces the Windows Layout Switch shortcut.
    /// </summary>
    public static Hotkey WinSpace { get; } =
        new(HotkeyKind.WinSpace, KeyModifiers.Windows, SpaceKey);

    /// <summary>
    /// Gets the gesture.
    /// </summary>
    /// <value>One of the enumeration values that specifies the gesture.</value>
    public HotkeyKind Kind { get; }

    /// <summary>
    /// Gets the modifiers of a combination.
    /// </summary>
    /// <value>
    /// A bitwise combination of the enumeration values that specifies the modifiers held with
    /// <see cref="VirtualKey" />, or <see cref="KeyModifiers.None" /> for a double tap.
    /// </value>
    public KeyModifiers Modifiers { get; }

    /// <summary>
    /// Gets the Windows virtual-key code of a combination's key, or 0 for a double tap.
    /// </summary>
    public ushort VirtualKey { get; }

    /// <summary>
    /// Gets the modifier a double tap uses.
    /// </summary>
    /// <value>
    /// The tapped modifier, or <see cref="KeyModifiers.None" /> for a combination.
    /// </value>
    public KeyModifiers TappedModifier => Kind switch {
      HotkeyKind.DoubleShift => KeyModifiers.Shift,
      HotkeyKind.DoubleCtrl => KeyModifiers.Control,
      HotkeyKind.DoubleAlt => KeyModifiers.Alt,
      _ => KeyModifiers.None,
    };

    /// <summary>
    /// Returns a custom combination of modifiers and one key.
    /// </summary>
    /// <param name="modifiers">
    /// A bitwise combination of the enumeration values that specifies the modifiers; at least
    /// one.
    /// </param>
    /// <param name="virtualKey">
    /// The Windows virtual-key code of the key, which is not itself a modifier.
    /// </param>
    /// <returns>The combination.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="modifiers" /> is empty, or <paramref name="virtualKey" /> is not a key
    /// code or is a modifier.
    /// </exception>
    public static Hotkey Custom(KeyModifiers modifiers, ushort virtualKey) {
      if (modifiers == KeyModifiers.None) {
        throw new ArgumentException("A combination needs a modifier.", nameof(modifiers));
      }

      if (virtualKey is 0 or > 0xFE || _modifierKeys.Contains(virtualKey)) {
        throw new ArgumentException(
            "A combination needs one key that is not a modifier.", nameof(virtualKey));
      }

      return new Hotkey(HotkeyKind.Custom, modifiers, virtualKey);
    }
  }
}
