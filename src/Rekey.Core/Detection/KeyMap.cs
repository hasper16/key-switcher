using System.Collections.Frozen;
using System.Globalization;
using System.Text;
using Rekey.Core.Languages;

namespace Rekey.Core.Detection {
  /// <summary>
  /// Holds what every printable key types in one Layout.
  /// </summary>
  public sealed class KeyMap {
    private readonly FrozenDictionary<ushort, KeyMapEntry> _byScanCode;
    private readonly FrozenDictionary<string, Keystroke> _byText;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyMap" /> class.
    /// </summary>
    /// <param name="layout">The Layout the keys belong to.</param>
    /// <param name="entries">The keys, one entry per scan code.</param>
    /// <exception cref="ArgumentException">Two entries share a scan code.</exception>
    public KeyMap(LayoutId layout, IEnumerable<KeyMapEntry> entries) {
      Layout = layout;
      Entries = [.. entries.OrderBy(entry => entry.ScanCode)];
      _byScanCode = Entries.ToFrozenDictionary(entry => entry.ScanCode);
      _byText = IndexByText(Entries);
    }

    /// <summary>
    /// Gets the Layout the keys belong to.
    /// </summary>
    public LayoutId Layout { get; }

    /// <summary>
    /// Gets the keys, ordered by scan code.
    /// </summary>
    public IReadOnlyList<KeyMapEntry> Entries { get; }

    /// <summary>
    /// Returns the Rendering of a Word in this Layout.
    /// </summary>
    /// <remarks>
    /// Caps Lock swaps Shift only on keys it affects, as Windows does.
    /// </remarks>
    /// <param name="word">The Keystrokes of the Word.</param>
    /// <returns>
    /// The Rendering, or <see langword="null" /> when a Keystroke uses AltGr, hits a dead key or a
    /// key this Layout does not list.
    /// </returns>
    public string? Render(IReadOnlyList<Keystroke> word) {
      var text = new StringBuilder(word.Count);
      foreach (Keystroke keystroke in word) {
        if (keystroke.AltGr || !_byScanCode.TryGetValue(keystroke.ScanCode, out KeyMapEntry? key)) {
          return null;
        }

        bool shifted = keystroke.Shift ^ (keystroke.CapsLock && key.CapsLock);
        if (shifted ? key.DeadShifted : key.DeadNormal) {
          return null;
        }

        text.Append(shifted ? key.Shifted : key.Normal);
      }

      return text.ToString();
    }

    /// <summary>
    /// Returns the Keystrokes that type a text in this Layout.
    /// </summary>
    /// <param name="text">The text to type.</param>
    /// <param name="capsLock">
    /// <see langword="true" /> to type with Caps Lock on; otherwise, <see langword="false" />.
    /// </param>
    /// <returns>
    /// The Keystrokes, or <see langword="null" /> when a character has no key in this Layout.
    /// </returns>
    internal IReadOnlyList<Keystroke>? Type(string text, bool capsLock) {
      var word = new List<Keystroke>(text.Length);
      TextElementEnumerator characters = StringInfo.GetTextElementEnumerator(text);
      while (characters.MoveNext()) {
        if (!_byText.TryGetValue(characters.GetTextElement(), out Keystroke keystroke)) {
          return null;
        }

        bool flipsWithCapsLock = capsLock && _byScanCode[keystroke.ScanCode].CapsLock;
        word.Add(keystroke with {
          Shift = keystroke.Shift ^ flipsWithCapsLock,
          CapsLock = capsLock,
        });
      }

      return word;
    }

    private static FrozenDictionary<string, Keystroke> IndexByText(
        IEnumerable<KeyMapEntry> entries) {
      var byText = new Dictionary<string, Keystroke>();
      foreach (KeyMapEntry entry in entries.Where(entry => !entry.DeadNormal)) {
        byText.TryAdd(entry.Normal, new Keystroke(entry.ScanCode, Shift: false, CapsLock: false));
      }

      foreach (KeyMapEntry entry in entries.Where(entry => !entry.DeadShifted)) {
        byText.TryAdd(entry.Shifted, new Keystroke(entry.ScanCode, Shift: true, CapsLock: false));
      }

      return byText.ToFrozenDictionary();
    }
  }
}
