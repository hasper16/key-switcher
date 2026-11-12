using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Automation;
using Rekey.Core;
using Rekey.Core.Languages;
using Rekey.Win32;
using Rekey.Win32.Replacement;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace Rekey.DesktopChecks {
  // The checks of change add-text-replacement: Corrections, held keys and Undo in real apps.
  // Only the spec's example Words are typed and corrected; reports carry no text.
  internal sealed partial class Checks {
    private static readonly TimeSpan _replaceWait = TimeSpan.FromSeconds(3);

    // What the worker saw during the latest CorrectIn, for failure details.
    private string _trace = string.Empty;

    public int RunTextReplacement() {
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
      Step("3.2 test window", CheckSpaceAfterWord);
      Step("3.2 keys typed during a Correction", CheckFastTyping);
      Step("4.1 Deferred run", CheckDeferredRun);
      Step("5.2 changed text", CheckChangedText);
      Step("6.1 Undo", CheckUndo);
      Step("6.1 Ctrl+Z that is no Undo", CheckRefusedUndo);
      Step("4.1 Notepad", CheckNotepad);
      Step("4.1 browser", CheckBrowserReplacement);
      Step("4.1 console", CheckConsoleReplacement);
      Step("5.4 VS Code", CheckVsCode);
      bool untouched = PInvoke.GetClipboardSequenceNumber() == clipboard;
      Expect("4.2 the clipboard is untouched", untouched, $"same sequence number {untouched}");
      return End();
    }

    // The text of the focused control, through UI Automation.
    private static string? FocusedText() {
      return TextOf(AutomationElement.FocusedElement);
    }

    private static string? TextOf(AutomationElement? element) {
      if (element is null) {
        return null;
      }

      if (element.TryGetCurrentPattern(ValuePattern.Pattern, out object value)) {
        return ((ValuePattern)value).Current.Value;
      }

      return element.TryGetCurrentPattern(TextPattern.Pattern, out object text)
          ? ((TextPattern)text).DocumentRange.GetText(-1)
          : null;
    }

    // Compares without printing either text.
    // Says where the texts part without showing either: their lengths, how many leading
    // characters agree, and whether the text ends with a space.
    private static string Compare(string? text, string expected) {
      int same = 0;
      while (text is not null && same < Math.Min(text.Length, expected.Length)
          && text[same] == expected[same]) {
        same++;
      }

      bool endsWithSpace = text?.EndsWith(' ') == true;
      return $"as expected {text == expected}, length {text?.Length} of {expected.Length}, "
          + $"first {same} characters agree, ends with a space {endsWithSpace}";
    }

    // The Notepad processes running now; the checks close only those they started.
    private static int[] NotepadIds() {
      Process[] running = Process.GetProcessesByName("Notepad");
      int[] ids = [.. running.Select(process => process.Id)];
      foreach (Process process in running) {
        process.Dispose();
      }

      return ids;
    }

    // Closes the Notepad that showed the test file, but only when this run started it: a
    // Notepad that was already running, or that showed something else, keeps running.
    private static void CloseNotepadIfStartedHere(uint ownerId, int[] before) {
      if (before.Contains((int)ownerId)) {
        return;
      }

      try {
        using Process process = Process.GetProcessById((int)ownerId);
        if (process.HasExited || process.ProcessName != "Notepad") {
          return;
        }

        process.CloseMainWindow();
        if (!process.WaitForExit(TimeSpan.FromSeconds(3))) {
          process.Kill();
        }
      } catch (ArgumentException) {
        // Already gone after its tab closed.
      }
    }

    private void CheckSpaceAfterWord() {
      ReplacementResult? result = CorrectIn(First().Ordinary, () => {
        Input.Type("ghbdtn");
        Input.Space();
      });
      string? text = FocusedText();
      Expect(
          "3.2 test window: the corrected Word, then the Space",
          text == "привет ",
          $"{Compare(text, "привет ")}, replacement {result}");
      bool russian = EventLog.WaitFor(
          () => CurrentLayout() == SpecWords.Russian, TimeSpan.FromSeconds(1));
      Expect("3.2 the Layout is Russian after the Correction", russian, $"{russian}");
    }

    private void CheckFastTyping() {
      ReplacementResult? result = CorrectIn(First().Ordinary, () => {
        Input.Type("ghbdtn");
        Input.TypeAtOnce(" rfr");
      });
      Thread.Sleep(_settle);
      string? text = FocusedText();

      // The three keys land after the switch to Russian, or just before it.
      bool inOrder = text is "привет как" or "привет rfr";
      Expect(
          "3.2 keys typed during a Correction follow it in order",
          inOrder,
          $"in order {inOrder}, length {text?.Length} of 10, replacement {result}");
    }

    private void CheckDeferredRun() {
      ReplacementResult? result = CorrectIn(First().Ordinary, () => {
        Input.Type("yt");
        Input.Space();
        Input.Type("pyf.");
        Input.Space();
      });
      string? text = FocusedText();
      Expect(
          "4.1 test window: a Deferred run is replaced with its space",
          text == "не знаю ",
          $"{Compare(text, "не знаю ")}, replacement {result}");
    }

    // Keys tagged as Rekey's own never reach the engine, as if an autocomplete added them.
    private void CheckChangedText() {
      EventLog.Position mark = _log.Mark();
      ReplacementResult? result = CorrectIn(First().Ordinary, () => {
        Input.Type("ghbdtn");
        Input.Type("xz", InjectedInput.Tag);
        Input.Space();
      });
      string? text = FocusedText();
      bool cancelled = _log.IncidentsSince(mark).Contains(IncidentType.CorrectionCancelled);
      Expect(
          "5.2 text that differs from the Word is left alone",
          result == ReplacementResult.Mismatch && text == "ghbdtnxz " && cancelled,
          $"replacement {result}, {Compare(text, "ghbdtnxz ")}, Incident {cancelled}");
    }

    private void CheckUndo() {
      ReplacementResult? result = CorrectIn(First().Ordinary, () => {
        Input.Type("ghbdtn");
        Input.Space();
      });
      Thread.Sleep(TimeSpan.FromSeconds(1));
      int undoKeys = First().UndoKeysSeen();
      EventLog.Position mark = _log.Mark();
      Input.Control('z');
      bool undone = EventLog.WaitFor(() => _log.UndosSince(mark) == 1, _replaceWait);
      Thread.Sleep(_settle);
      string? text = FocusedText();
      bool english = CurrentLayout() == SpecWords.EnglishUs;
      bool consumed = First().UndoKeysSeen() == undoKeys;
      string detail = $"Undo reported {undone}, {Compare(text, "ghbdtn ")}, "
          + $"English (US) {english}, Ctrl+Z kept from the app {consumed}, replacement {result}";
      Expect(
          "6.1 Ctrl+Z within 5 s restores the typed text and English (US)",
          undone && text == "ghbdtn " && english && consumed,
          detail);
    }

    private void CheckRefusedUndo() {
      ExpectUndoKeyReachesApp(
          "6.1 Ctrl+Z after 6 s reaches the app",
          Input.Space,
          () => Thread.Sleep(TimeSpan.FromSeconds(6)));
      ExpectUndoKeyReachesApp(
          "6.1 Ctrl+Z after another letter reaches the app", Input.Space, () => Input.Type("r"));
      ExpectUndoKeyReachesApp(
          "6.1 Ctrl+Z after an Enter-triggered Correction reaches the app", Input.Enter, () => { });
    }

    private void ExpectUndoKeyReachesApp(string item, Action trigger, Action between) {
      ReplacementResult? result = CorrectIn(First().Ordinary, () => {
        Input.Type("ghbdtn");
        trigger();
      });
      between();
      int before = First().UndoKeysSeen();
      EventLog.Position mark = _log.Mark();
      Input.Control('z');
      Thread.Sleep(_settle);
      int seen = First().UndoKeysSeen() - before;
      int undos = _log.UndosSince(mark);
      Expect(
          item,
          seen == 1 && undos == 0,
          $"Ctrl+Z seen by the app {seen}, Undos {undos}, replacement {result}");
    }

    private void CheckNotepad() {
      // A new file of this run's own, so nothing that exists is overwritten or deleted.
      string name = $"rekey-desktop-check-{Guid.NewGuid():N}.txt";
      string file = Path.Join(Path.GetTempPath(), name);
      new FileStream(file, FileMode.CreateNew).Dispose();
      _restorer.Defer("Notepad test file", () => File.Delete(file));
      string notepadPath = Path.Join(
          Environment.GetFolderPath(Environment.SpecialFolder.Windows), "notepad.exe");
      int[] before = NotepadIds();
      Process.Start(new ProcessStartInfo(notepadPath, $"\"{file}\"") {
        UseShellExecute = true,
      })?.Dispose();
      HWND? window = Desktop.FindWindow(name, TimeSpan.FromSeconds(15));
      if (window is not { } notepad) {
        Expect("4.1 Notepad", false, "Notepad did not open the test file");
        return;
      }

      // Below the always-on-top test windows, so clicks meant for Notepad reach it.
      PInvoke.MoveWindow(notepad, 80, 320, 640, 360, true);
      _ = PInvoke.GetWindowThreadProcessId(notepad, out uint ownerId);
      Thread.Sleep(_settle);
      Point editor = Desktop.Center(notepad);
      _restorer.Defer("Notepad", () => {
        // Saved empty first, so closing asks nothing and the session keeps no draft; a
        // Notepad the user had open keeps running; the one this run started is closed.
        Focus(editor);
        Input.ClearBox();
        Input.Control('s');
        Thread.Sleep(_settle);
        Input.CloseTab();
        Thread.Sleep(_settle);
        CloseNotepadIfStartedHere(ownerId, before);
      });
      ExpectCorrected("4.1 Notepad", editor);
    }

    private void CheckBrowserReplacement() {
      if (_browserBox is null && OpenTestPage() is var (user, _)) {
        _browserBox = user;
      }

      if (_browserBox is not { } box) {
        Expect("4.1 browser text box", false, "the test page's boxes were not found");
        return;
      }

      ExpectCorrected("4.1 browser text box", box);
      if (_chatBox is not { } chat || _page is not { } page) {
        Expect("4.2 chat box", false, "the test page's chat box was not found");
        return;
      }

      ReplacementResult? result = CorrectIn(chat, () => {
        Input.Type("ghbdtn");
        Input.Enter();
      });
      var sentBox = new PropertyCondition(AutomationElement.AutomationIdProperty, "check-sent");
      string? sent = TextOf(
          AutomationElement.FromHandle(page).FindFirst(TreeScope.Descendants, sentBox));
      Expect(
          "4.2 Enter in a chat box sends the corrected text",
          sent == "привет",
          $"{Compare(sent, "привет")}, replacement {result}");
    }

    // Corrects in the default console (Windows Terminal or conhost, as Windows is set) and
    // in a classic conhost window. Both are corrected by retyping; a console not proven to
    // take typed text is left as typed.
    private void CheckConsoleReplacement() {
      string shell = Path.Join(Environment.SystemDirectory, "cmd.exe");
      string conhost = Path.Join(Environment.SystemDirectory, "conhost.exe");
      ExpectConsole(
          "default console", OpenProbeConsole(shell, [], _defaultTitle), _defaultTitle);
      ExpectConsole(
          "conhost", OpenProbeConsole(conhost, [shell], _conhostTitle), _conhostTitle);
    }

    private void ExpectConsole(string host, HWND? window, string title) {
      if (window is not { } found) {
        Expect($"4.1 console ({host})", false, "the console window did not appear");
        return;
      }

      bool takesText = ClassOf(found) is "ConsoleWindowClass" or "CASCADIA_HOSTING_WINDOW_CLASS";

      // The digits read the same in every Layout, so seeing them proves the buffer is read.
      ReplacementResult? result = CorrectIn(
          Desktop.Center(found),
          () => {
            Input.Type(_marker + " ghbdtn");
            Input.Space();
          },
          clear: false);
      string? text = BufferText(title);
      bool markerShown = text?.Contains(_marker, StringComparison.Ordinal) == true;
      bool corrected = text?.Contains(_marker + " привет ", StringComparison.Ordinal) == true;
      bool kept = text?.Contains(_marker + " ghbdtn ", StringComparison.Ordinal) == true;
      string detail = $"window class {ClassOf(found)}, buffer read {markerShown}, "
          + $"corrected {corrected}, typed Word kept {kept}, replacement {result} {_trace}";
      if (takesText) {
        Expect(
            $"4.1 console ({host}): the corrected Word, then the Space, retyped",
            markerShown && corrected && result == ReplacementResult.Retyped,
            detail);
      } else {
        Expect(
            $"4.1 console ({host}): not proven to take typed text, the Word left as typed",
            markerShown && kept && result == ReplacementResult.Unsupported,
            detail);
      }

      Input.Escape();
    }

    // Corrects in a box and checks the result and that the text was checked first (5.1).
    private void ExpectCorrected(string item, Point at) {
      ReplacementResult? result = CorrectIn(at, () => {
        Input.Type("ghbdtn");
        Input.Space();
      });
      string? text = FocusedText();
      Expect(
          $"{item}: the corrected Word, then the Space",
          text == "привет ",
          $"{Compare(text, "привет ")}, replacement {result} {_trace}");
      Expect(
          $"5.1 {item}: the text was checked through UI Automation first",
          result == ReplacementResult.Verified,
          $"replacement {result}");
    }

    // Switches the box at the point to English (US), empties it, types and waits for the
    // worker to replace text or decide not to.
    private ReplacementResult? CorrectIn(Point at, Action type, bool clear = true) {
      EventLog.Position before = _log.Mark();
      bool switched = SwitchIn(at, SpecWords.EnglishUs);
      if (clear) {
        Input.ClearBox();
      }

      return TypeAndWait(type, before, switched);
    }

    // Types where the caret is, without a click or a switch, and waits for the worker to
    // replace text or decide not to.
    private ReplacementResult? TypeAndWait(
        Action type, EventLog.Position before, bool switched) {
      bool english = CurrentLayout() == SpecWords.EnglishUs;
      EventLog.Position mark = _log.Mark();
      type();
      EventLog.WaitFor(() => _log.ReplacementsSince(mark).Count > 0, _replaceWait);
      Thread.Sleep(TimeSpan.FromMilliseconds(300));
      List<ReplacementResult> results = _log.ReplacementsSince(mark);
      _trace = Trace(before, mark, switched, english);
      return results.Count == 0 ? null : results[^1];
    }

    // What the worker saw around one attempt, without text: whether the switch to English (US)
    // landed, the Layout Rekey believed in, the keys it held, the focus reports and Incidents.
    private string Trace(
        EventLog.Position before, EventLog.Position mark, bool switched, bool english) {
      List<KeyDown> keys = _log.KeyDownsSince(mark);
      int held = keys.Count(key => key.Held);
      int dropped = keys.Count(key => !key.FocusConfirmed);
      int focusReports = _log.EventsSince(before)
          .Count(seen => seen.Event is FocusChanged or FocusLost);
      string incidents = string.Join('/', _log.IncidentsSince(before));
      return $"[switch landed {switched}, English (US) before typing {english}, "
          + $"keys {keys.Count}, held {held}, unconfirmed {dropped}, "
          + $"focus reports {focusReports}, Incidents [{incidents}]]";
    }
  }
}
