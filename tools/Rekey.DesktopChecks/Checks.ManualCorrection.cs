using System.Drawing;
using System.Globalization;
using Rekey.Core;
using Rekey.Core.Languages;
using Rekey.Win32.Hotkeys;
using Rekey.Win32.Replacement;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Rekey.DesktopChecks {
  // The checks of change add-manual-correction: the Hotkey as the user performs it, in the
  // test window, Notepad and a browser. Only the spec's example Words are typed; reports
  // carry no text.
  internal sealed partial class Checks {
    private const ushort _shiftScan = 0x2A;
    private const ushort _ctrlScan = 0x1D;
    private const ushort _altScan = 0x38;
    private const ushort _winScan = 0x5B;
    private const ushort _spaceScan = 0x39;
    private const ushort _kScan = 0x25;

    private static readonly TimeSpan _tapGap = TimeSpan.FromMilliseconds(60);
    private static readonly Settings _manualOnly = new(AutoCorrectionEnabled: false);
    private static readonly Hotkey _ctrlAltK =
        Hotkey.Custom(KeyModifiers.Control | KeyModifiers.Alt, (ushort)VIRTUAL_KEY.VK_K);

    public int RunManualCorrection() {
      Begin(minutes: 3);
      LayoutId[] installed = [.. InstalledLayouts()];
      if (!installed.Contains(SpecWords.EnglishUs) || !installed.Contains(SpecWords.Russian)) {
        Expect(
            "prerequisites",
            false,
            "install the English (US) and Russian Layouts, then run again");
        return End();
      }

      uint clipboard = PInvoke.GetClipboardSequenceNumber();
      Step("1.2 double tap of Shift", CheckDoubleShift);
      Step("1.3 custom combination", CheckCustomCombo);
      Step("1.4 Win+Space", CheckWinSpace);
      Step("2.1 the Hotkey reverts a Correction", CheckRevert);
      Step("3.2 selection", CheckSelection);
      Step("4.2 conflicts", CheckConflicts);
      bool untouched = PInvoke.GetClipboardSequenceNumber() == clipboard;
      Expect("3.2 the clipboard is untouched", untouched, $"same sequence number {untouched}");
      Apply(new Settings());
      return End();
    }

    private static void DoubleTapShift() {
      for (int tap = 0; tap < 2; tap++) {
        Input.Key(VIRTUAL_KEY.VK_LSHIFT, _shiftScan, up: false);
        Thread.Sleep(_tapGap);
        Input.Key(VIRTUAL_KEY.VK_LSHIFT, _shiftScan, up: true);
        Thread.Sleep(_tapGap);
      }
    }

    private static void CtrlAltK() {
      Input.Key(VIRTUAL_KEY.VK_LCONTROL, _ctrlScan, up: false);
      Input.Key(VIRTUAL_KEY.VK_LMENU, _altScan, up: false);
      Input.Press(VIRTUAL_KEY.VK_K, _kScan);
      Input.Key(VIRTUAL_KEY.VK_LMENU, _altScan, up: true);
      Input.Key(VIRTUAL_KEY.VK_LCONTROL, _ctrlScan, up: true);
    }

    private static void WinSpace() {
      Input.Key(VIRTUAL_KEY.VK_LWIN, _winScan, up: false);
      Thread.Sleep(_tapGap);
      Input.Press(VIRTUAL_KEY.VK_SPACE, _spaceScan);
      Thread.Sleep(_tapGap);
      Input.Key(VIRTUAL_KEY.VK_LWIN, _winScan, up: true);
    }

    private static int CommonPrefix(string? text, string expected) {
      int same = 0;
      while (text is not null && same < Math.Min(text.Length, expected.Length)
          && text[same] == expected[same]) {
        same++;
      }

      return same;
    }

    private static bool Shuffled(string? text, string expected, int same) {
      return text is not null && text.Length == expected.Length
          && text[same..].Order().SequenceEqual(expected[same..].Order());
    }

    // What kinds of characters a text holds, such as "Cyr-low|space"; never the characters.
    private static string KindsOf(string? text) {
      if (string.IsNullOrEmpty(text)) {
        return "empty";
      }

      return string.Join('|', text.Select(character => character switch {
        ' ' => "space",
        >= 'Ѐ' and <= 'ӿ' => char.IsUpper(character) ? "Cyr-up" : "Cyr-low",
        >= 'A' and <= 'Z' => "Lat-up",
        >= 'a' and <= 'z' => "Lat-low",
        _ when char.IsDigit(character) => "digit",
        _ when char.IsPunctuation(character) => "punctuation",
        _ => "other",
      }));
    }

    private void CheckDoubleShift() {
      Apply(_manualOnly);
      Point box = First().Ordinary;
      Attempt word = PerformHotkey(box, "ghbdtn", DoubleTapShift);
      Expect(
          "1.2 a double tap of Shift corrects the Word and switches to Russian",
          word.Replaced && word.Text == "привет" && WaitForLayout(SpecWords.Russian),
          $"{word}, {Compare(word.Text, "привет")}");

      Attempt recent = PerformHotkey(box, "yt ", DoubleTapShift);
      Expect(
          "2.1 a double tap after \"yt \" gives \"не \"",
          recent.Replaced && recent.Text == "не ",
          $"{recent}, {Compare(recent.Text, "не ")}");
    }

    private void CheckCustomCombo() {
      if (OpenNotepad() is not { } notepad) {
        Expect("1.3 custom combination", false, "Notepad did not open the test file");
        return;
      }

      Apply(_manualOnly with { Hotkey = _ctrlAltK });
      Attempt combo = PerformHotkey(notepad, "ghbdtn", CtrlAltK);
      Expect(
          "1.3 Ctrl+Alt+K corrects the Word in Notepad and K never reaches it",
          combo.Replaced && combo.Text == "привет",
          $"{combo}, {Compare(combo.Text, "привет")}");
    }

    private void CheckWinSpace() {
      Apply(_manualOnly with { Hotkey = Hotkey.WinSpace });
      Point box = First().Ordinary;
      Attempt word = PerformHotkey(box, "ghbdtn", WinSpace);

      // Read after the pause, before FocusShows clicks the test window and would close Start.
      Thread.Sleep(_quiet);
      HWND front = PInvoke.GetForegroundWindow();
      bool once = CurrentLayout() == SpecWords.Russian
          && FocusShows(box, SpecWords.Russian);
      string detail = $"{word}, {Compare(word.Text, "привет")}, one switch {once}, "
          + $"test window in front {IsTestWindow(front)}";
      Expect(
          "1.4 Win+Space corrects the Word, switches to Russian once and leaves Start closed",
          word.Replaced && word.Text == "привет" && once && IsTestWindow(front),
          detail);

      // With nothing to correct the Layout after English (US) in Windows' order comes next.
      IReadOnlyList<LayoutId> order = InstalledLayouts();
      LayoutId next = order[(order.ToList().IndexOf(SpecWords.EnglishUs) + 1) % order.Count];
      Attempt empty = PerformHotkey(box, string.Empty, WinSpace);
      Thread.Sleep(_quiet);
      front = PInvoke.GetForegroundWindow();
      bool switched = FocusShows(box, next);
      Expect(
          "1.4 Win+Space in an empty box switches to the next Layout and leaves Start closed",
          empty.Result is null && empty.Text == string.Empty && switched && IsTestWindow(front),
          $"{empty}, switched {switched}, test window in front {IsTestWindow(front)}");
    }

    private void CheckRevert() {
      Apply(new Settings());
      Point box = First().Ordinary;
      Attempt corrected = Corrected(box, Input.Space);
      EventLog.Position mark = _log.Mark();
      DoubleTapShift();
      EventLog.WaitFor(() => _log.UndosSince(mark) > 0, _replaceWait);
      Thread.Sleep(TimeSpan.FromMilliseconds(300));
      string? text = FocusedText();
      bool reverted = corrected.Replaced && text == "ghbdtn " && _log.UndosSince(mark) == 1
          && WaitForLayout(SpecWords.EnglishUs);
      Expect(
          "2.1 a double tap right after a Correction reverts it as an Undo",
          reverted,
          $"{corrected}, Undos {_log.UndosSince(mark)}, {Compare(text, "ghbdtn ")}");
    }

    private void CheckSelection() {
      Apply(_manualOnly);
      ExpectSelectionConverted("3.2 selection in the test window", First().Ordinary);
      if (OpenNotepad() is { } notepad) {
        ExpectSelectionConverted("3.2 selection in Notepad", notepad);

        // Tell apart what the Notepad case depends on: the Shift taps, or the second Word.
        Apply(_manualOnly with { Hotkey = _ctrlAltK });
        ExpectSelectionConverted("3.2 selection in Notepad with Ctrl+Alt+K", notepad, CtrlAltK);
        Apply(_manualOnly);
        ExpectSelectionConverted(
            "3.2 one-Word selection in Notepad", notepad, typed: "ghbdtn", expected: "привет");

        // Controls without Rekey's Hotkey: the same text typed over the selection the way
        // Rekey types it, in one burst, one key at a time, and in one burst followed at once
        // by a Layout Switch to Russian, as after a Manual Correction.
        ExpectTypedOver("3.2 control: Notepad takes the text in one burst", notepad, () => {
          Input.TypeUnicode("привет мир", 0, oneCallEach: false);
          return true;
        });
        ExpectTypedOver("3.2 control: Notepad takes the text key by key", notepad, () => {
          Input.TypeUnicode("привет мир", 0, oneCallEach: true);
          return true;
        });
        ExpectTypedOver("3.2 control: Notepad takes a burst and a Layout Switch", notepad, () => {
          Input.TypeUnicode("привет мир", 0, oneCallEach: false);
          _worker.SwitchLayout(SpecWords.Russian);
          return WaitForLayout(SpecWords.Russian);
        });
      } else {
        Expect("3.2 selection in Notepad", false, "Notepad did not open the test file");
      }

      if (_browserBox is null && OpenTestPage() is var (user, _)) {
        _browserBox = user;
      }

      if (_browserBox is { } page) {
        ExpectSelectionConverted("3.2 selection in the browser", page);
      } else {
        Expect("3.2 selection in the browser", false, "the test page's boxes were not found");
      }
    }

    private void ExpectSelectionConverted(
        string item,
        Point at,
        Action? hotkey = null,
        string typed = "ghbdtn vbh",
        string expected = "привет мир") {
      SwitchIn(at, SpecWords.EnglishUs);
      Input.ClearBox();
      Input.Type(typed);
      Input.Control('a');
      Thread.Sleep(_settle);
      EventLog.Position mark = _log.Mark();
      (hotkey ?? DoubleTapShift)();
      EventLog.WaitFor(() => _log.ReplacementsSince(mark).Count > 0, _replaceWait);
      Thread.Sleep(_settle);
      Attempt attempt = Read(mark);

      // Where the texts part: whether the rest is still as typed, and how much the worker
      // read as selected; lengths and booleans only.
      int? read = _log.EventsSince(mark)
          .Select(seen => seen.Event)
          .OfType<ManualCorrectionRequested>()
          .LastOrDefault()?.Selection?.Length;
      int same = CommonPrefix(attempt.Text, expected);
      bool restAsTyped = attempt.Text is { } text && text.Length == typed.Length
          && text[same..] == typed[same..];
      string detail = $"{attempt}, {Compare(attempt.Text, expected)}, selection read "
          + $"{read?.ToString(CultureInfo.InvariantCulture) ?? "none"} of {typed.Length}, "
          + $"rest still as typed {restAsTyped}, same but for case "
          + $"{string.Equals(attempt.Text, expected, StringComparison.OrdinalIgnoreCase)}, "
          + $"rest is {KindsOf(attempt.Text?[same..])}, rest has the expected letters in "
          + $"another order {Shuffled(attempt.Text, expected, same)}";
      Expect(
          $"{item}: \"{typed}\" becomes \"{expected}\"",
          attempt.Replaced && attempt.Text == expected,
          detail);
    }

    // The typing returns whether everything besides the text happened, such as a switch.
    private void ExpectTypedOver(string item, Point at, Func<bool> typeOver) {
      const string expected = "привет мир";
      SwitchIn(at, SpecWords.EnglishUs);
      Input.ClearBox();
      Input.Type("ghbdtn vbh");
      Input.Control('a');
      Thread.Sleep(_settle);
      bool rest = typeOver();
      Thread.Sleep(_settle);
      Focus(at);
      string? text = FocusedText();
      int same = CommonPrefix(text, expected);
      string detail = $"{Compare(text, expected)}, rest is {KindsOf(text?[same..])}, "
          + $"rest has the expected letters in another order {Shuffled(text, expected, same)}";
      Expect(item, rest && text == expected, $"{detail}, the rest happened {rest}");
    }

    private void CheckConflicts() {
      IReadOnlySet<KeyChord> windows = LayoutSwitchShortcuts.Read();
      HotkeyConflict winSpace = HotkeyConflicts.Check(
          Hotkey.Custom(KeyModifiers.Windows, Hotkey.SpaceKey), windows, HotkeyProbe.IsTaken);
      Expect(
          "4.2 Win+Space is reported as the Windows Layout Switch shortcut",
          winSpace == HotkeyConflict.WindowsLayoutSwitch,
          $"conflict {winSpace}, Windows shortcuts {windows.Count}");

      // Another registrant: a thread that registers Ctrl+Alt+K as its hotkey.
      var chord = new KeyChord(KeyModifiers.Control | KeyModifiers.Alt, (ushort)VIRTUAL_KEY.VK_K);
      HotkeyConflict free = HotkeyConflicts.Check(_ctrlAltK, windows, HotkeyProbe.IsTaken);
      using var registered = new ManualResetEventSlim();
      using var release = new ManualResetEventSlim();
      var other = new Thread(() => {
        const HOT_KEY_MODIFIERS modifiers =
            HOT_KEY_MODIFIERS.MOD_CONTROL | HOT_KEY_MODIFIERS.MOD_ALT;
        bool ok = PInvoke.RegisterHotKey(HWND.Null, 1, modifiers, chord.VirtualKey);
        registered.Set();
        release.Wait();
        if (ok) {
          PInvoke.UnregisterHotKey(HWND.Null, 1);
        }
      });
      other.Start();
      registered.Wait();
      HotkeyConflict taken = HotkeyConflicts.Check(_ctrlAltK, windows, HotkeyProbe.IsTaken);
      release.Set();
      other.Join();
      Expect(
          "4.2 a combination another registrant holds is reported as taken",
          free == HotkeyConflict.None && taken == HotkeyConflict.TakenByAnotherApp,
          $"before {free}, while registered {taken}");
    }

    // Types the text in the emptied box in English (US), performs the Hotkey and waits for the
    // replacement, if one comes.
    private Attempt PerformHotkey(Point box, string text, Action hotkey) {
      SwitchIn(box, SpecWords.EnglishUs);
      Input.ClearBox();
      Input.Type(text);
      EventLog.Position mark = _log.Mark();
      hotkey();
      EventLog.WaitFor(() => _log.ReplacementsSince(mark).Count > 0, _replaceWait);
      Thread.Sleep(TimeSpan.FromMilliseconds(300));
      return Read(mark);
    }

    private bool IsTestWindow(HWND window) {
      return window == PInvoke.GetAncestor(
          PInvoke.WindowFromPoint(First().Ordinary),
          GET_ANCESTOR_FLAGS.GA_ROOT);
    }
  }
}
