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
    private static string Compare(string? text, string expected) {
      return $"as expected {text == expected}, length {text?.Length} of {expected.Length}";
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
      Process.Start(new ProcessStartInfo(notepadPath, $"\"{file}\"") {
        UseShellExecute = true,
      })?.Dispose();
      HWND? window = Desktop.FindWindow(name, TimeSpan.FromSeconds(15));
      if (window is not { } notepad) {
        Expect("4.1 Notepad", false, "Notepad did not open the test file");
        return;
      }

      Point editor = Desktop.Center(notepad);
      _restorer.Defer("Notepad tab", () => {
        // Saved empty first, so closing asks nothing and the session keeps no draft.
        Focus(editor);
        Input.ClearBox();
        Input.Control('s');
        Thread.Sleep(_settle);
        Input.CloseTab();
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

    private void CheckConsoleReplacement() {
      if (StartConsole() is not { } console) {
        Expect("4.1 console", false, "the console window did not appear");
        return;
      }

      ReplacementResult? result = CorrectIn(
          console,
          () => {
            Input.Type("ghbdtn");
            Input.Space();
          },
          clear: false);
      HWND? window = Desktop.FindWindow(_consoleTitle, TimeSpan.FromSeconds(1));
      var hasText = new PropertyCondition(AutomationElement.IsTextPatternAvailableProperty, true);
      AutomationElement? buffer = window is { } found
          ? AutomationElement.FromHandle(found).FindFirst(TreeScope.Subtree, hasText)
          : null;
      string? text = TextOf(buffer);
      bool corrected = text?.Contains("привет ", StringComparison.Ordinal) == true
          && !text.Contains("ghbdtn", StringComparison.Ordinal);
      Expect(
          "4.1 console: the corrected Word, then the Space",
          corrected,
          $"as expected {corrected}, replacement {result}");
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
          $"{Compare(text, "привет ")}, replacement {result}");
      Expect(
          $"5.1 {item}: the text was checked through UI Automation first",
          result == ReplacementResult.Verified,
          $"replacement {result}");
    }

    // Switches the box at the point to English (US), empties it, types and waits for the
    // worker to replace text or decide not to.
    private ReplacementResult? CorrectIn(Point at, Action type, bool clear = true) {
      SwitchIn(at, SpecWords.EnglishUs);
      if (clear) {
        Input.ClearBox();
      }

      EventLog.Position mark = _log.Mark();
      type();
      EventLog.WaitFor(() => _log.ReplacementsSince(mark).Count > 0, _replaceWait);
      Thread.Sleep(TimeSpan.FromMilliseconds(300));
      List<ReplacementResult> results = _log.ReplacementsSince(mark);
      return results.Count == 0 ? null : results[^1];
    }
  }
}
