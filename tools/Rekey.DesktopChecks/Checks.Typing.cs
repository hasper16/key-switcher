using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Automation;
using Rekey.Core;
using Rekey.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace Rekey.DesktopChecks {
  // Checks 2.1, 3.2 and 3.3: what reaches the worker, and that Password Fields keep nothing.
  internal sealed partial class Checks {
    private const string _pageTitle = "Rekey desktop check";
    private const int _rounds = 10;

    private static Point? FindBox(HWND window, string id) {
      AutomationElement? box = AutomationElement.FromHandle(window).FindFirst(
          TreeScope.Descendants,
          new PropertyCondition(AutomationElement.AutomationIdProperty, id));
      if (box is null) {
        return null;
      }

      System.Windows.Rect bounds = box.Current.BoundingRectangle;
      return new Point(
          (int)(bounds.Left + (bounds.Width / 2)), (int)(bounds.Top + (bounds.Height / 2)));
    }

    private void CheckHooks(EventLog.Position start) {
      List<IncidentType> early = _log.IncidentsSince(start);
      Expect("2.1 no Incident at start-up", early.Count == 0, $"{early.Count} Incidents");

      Focus(First().Ordinary);
      EventLog.Position tagged = _log.Mark();
      Input.Press(VIRTUAL_KEY.VK_A, 0x1E, InjectedInput.Tag);
      Thread.Sleep(_settle);
      int seenTagged = _log.KeyDownsSince(tagged).Count;
      Expect("2.1 Rekey's own keys are ignored", seenTagged == 0, $"{seenTagged} KeyDowns");

      EventLog.Position untagged = _log.Mark();
      Input.Press(VIRTUAL_KEY.VK_A, 0x1E);
      List<KeyDown> other = WaitForKeys(untagged, 1);
      Expect(
          "2.1 other injected key is a Character",
          other.Count == 1 && other[0].Kind == KeyKind.Character,
          $"{other.Count} KeyDowns, kind {other.FirstOrDefault()?.Kind}");

      EventLog.Position typed = _log.Mark();
      Input.Type("qwe");
      List<KeyDown> keys = WaitForKeys(typed, 3);
      int wordKeys = _log.EventsSince(typed).LastOrDefault()?.WordKeys ?? 0;
      Expect(
          "2.1 typed keys build the Word",
          keys.Count == 3 && wordKeys >= 3,
          $"{keys.Count} KeyDowns, Word keys {wordKeys}");

      EventLog.Position click = _log.Mark();
      Input.Click(First().Ordinary);
      bool clicked = EventLog.WaitFor(
          () => _log.EventsSince(click).Exists(seen => seen.Event is MouseClick), _eventWait);
      Expect("2.1 a click reaches the worker", clicked, clicked ? "MouseClick seen" : "none");
    }

    private void CheckMaskedEdit() {
      EventLog.Position mark = _log.Mark();
      Focus(First().Masked);
      bool password = EventLog.WaitFor(
          () => _log.EventsSince(mark).Exists(seen => seen.Event is FocusChanged {
            IsPassword: true,
          }),
          _eventWait);
      Expect("3.2 Win32 masked edit is a Password Field", password, $"IsPassword {password}");
      ExpectNothingKept("3.2 typing in the masked edit keeps nothing", mark);

      EventLog.Position back = _log.Mark();
      Focus(First().Ordinary);
      bool ordinary = EventLog.WaitFor(
          () => _log.EventsSince(back).Exists(seen => seen.Event is FocusChanged {
            IsPassword: false,
          }),
          _eventWait);
      Expect("3.2 ordinary edit is not a Password Field", ordinary, $"seen {ordinary}");
    }

    private void CheckBrowser() {
      string page = Path.Join(AppContext.BaseDirectory, "check-page.html");
      Process.Start(new ProcessStartInfo(page) { UseShellExecute = true })?.Dispose();
      HWND? window = Desktop.FindWindow(_pageTitle, TimeSpan.FromSeconds(20));
      if (window is null) {
        Expect("3.3 test page opens", false, "no browser window with the test page");
        return;
      }

      Thread.Sleep(TimeSpan.FromSeconds(2));
      Point? user = FindBox(window.Value, "check-user");
      Point? secret = FindBox(window.Value, "check-pass");
      if (user is null || secret is null) {
        Expect("3.3 test page boxes found", false, "UI Automation did not find the boxes");
        return;
      }

      _browserBox = user;
      _restorer.Defer("test page tab", () => {
        Input.Click(user.Value);
        Input.CloseTab();
      });

      var userWords = new List<int>();
      var secretKeys = new List<int>();
      for (int round = 0; round < _rounds; round++) {
        Input.Click(user.Value);
        userWords.Add(TypeTwoWords("qwe", "rty"));
        Input.Click(secret.Value);
        secretKeys.Add(TypeTwoWords("asd", "fgh"));
      }

      ExpectRounds("3.3 click", userWords, secretKeys);

      userWords.Clear();
      secretKeys.Clear();
      Input.Click(user.Value);
      for (int round = 0; round < _rounds; round++) {
        Input.Tab();
        secretKeys.Add(TypeTwoWords("asd", "fgh"));
        Input.Tab(shift: true);
        userWords.Add(TypeTwoWords("qwe", "rty"));
      }

      ExpectRounds("3.3 Tab and Shift+Tab", userWords, secretKeys);
    }

    // Types two Words at once and returns the most Word keys the worker held meanwhile.
    private int TypeTwoWords(string first, string second) {
      EventLog.Position mark = _log.Mark();
      Input.Type(first);
      Input.Space();
      Input.Type(second);
      WaitForKeys(mark, first.Length + second.Length + 1);
      return _log.EventsSince(mark).Select(seen => seen.WordKeys).DefaultIfEmpty(0).Max();
    }

    private void ExpectRounds(string path, List<int> userWords, List<int> secretKeys) {
      int counted = userWords.Count(keys => keys > 0);
      Expect(
          $"{path}: the user-name box builds Words",
          counted == userWords.Count,
          $"{counted} of {userWords.Count} rounds counted up");
      int kept = secretKeys.Count(keys => keys > 0);
      Expect(
          $"{path}: the password box keeps nothing",
          kept == 0,
          $"Word keys above 0 in {kept} of {secretKeys.Count} rounds");
    }

    private void ExpectNothingKept(string item, EventLog.Position mark) {
      EventLog.Position typed = _log.Mark();
      Input.Type("asd");
      WaitForKeys(typed, 3);
      int most = _log.EventsSince(mark).Select(seen => seen.WordKeys).DefaultIfEmpty(0).Max();
      Expect(item, most == 0, $"most Word keys {most}");
    }
  }
}
