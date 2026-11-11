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

    // Finds a page box by its HTML id or its label, retrying while the browser builds its
    // accessibility tree (Firefox starts it on the first UI Automation request).
    private static Point? FindBox(HWND window, string id, string label) {
      var condition = new OrCondition(
          new PropertyCondition(AutomationElement.AutomationIdProperty, id),
          new PropertyCondition(AutomationElement.NameProperty, label));
      Point? found = null;
      EventLog.WaitFor(
          () => {
            AutomationElement? box = AutomationElement.FromHandle(window)
                .FindFirst(TreeScope.Descendants, condition);
            System.Windows.Rect? bounds = box?.Current.BoundingRectangle;
            if (bounds is { IsEmpty: false, Width: > 0 } rect) {
              found = new Point(
                  (int)(rect.Left + (rect.Width / 2)), (int)(rect.Top + (rect.Height / 2)));
            }

            return found is not null;
          },
          TimeSpan.FromSeconds(10));
      return found;
    }

    // Opens the test page in the default browser, or in Edge when the default browser does not
    // show the page's boxes to UI Automation; returns the window and both boxes.
    private (Point User, Point Secret)? OpenTestPage() {
      string page = Path.Join(AppContext.BaseDirectory, "check-page.html");
      string[] browsers = [page, "microsoft-edge:" + new Uri(page).AbsoluteUri];
      foreach (string target in browsers) {
        Process.Start(new ProcessStartInfo(target) { UseShellExecute = true })?.Dispose();
        HWND? window = Desktop.FindWindow(_pageTitle, TimeSpan.FromSeconds(20));
        if (window is null) {
          continue;
        }

        Point? user = FindBox(window.Value, "check-user", "Nickname");
        Point? secret = FindBox(window.Value, "check-pass", "Secret");
        if (user is not null && secret is not null) {
          _report.Add(
              "3.3 test page boxes found",
              true,
              target == page ? "in the default browser" : "in Edge");
          _restorer.Defer("test page tab", () => {
            Input.Click(user.Value);
            Input.CloseTab();
          });
          return (user.Value, secret.Value);
        }

        // Close this browser's test tab before trying the next one.
        Input.Click(Desktop.Center(window.Value));
        Input.CloseTab();
        Thread.Sleep(_settle);
      }

      return null;
    }

    private void CheckHooks(EventLog.Position start) {
      List<IncidentType> early = _log.IncidentsSince(start);
      Expect("2.1 no Incident at start-up", early.Count == 0, $"{early.Count} Incidents");

      Focus(First().Ordinary);

      // The untagged key after the tagged one is the sentinel: once the worker reported it, it
      // has read the tagged key too, so exactly one KeyDown means the tagged one was ignored.
      EventLog.Position tagged = _log.Mark();
      Input.Press(VIRTUAL_KEY.VK_A, 0x1E, InjectedInput.Tag);
      Input.Press(VIRTUAL_KEY.VK_A, 0x1E);
      WaitForKeys(tagged, 1);
      Thread.Sleep(_settle);
      List<KeyDown> other = _log.KeyDownsSince(tagged);
      Expect("2.1 Rekey's own keys are ignored", other.Count == 1, $"{other.Count} KeyDowns");
      Expect(
          "2.1 other injected key is a Character",
          other.Count >= 1 && other[^1].Kind == KeyKind.Character,
          $"kind {other.LastOrDefault()?.Kind}");

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
      if (OpenTestPage() is not var (user, secret)) {
        Expect(
            "3.3 test page boxes found",
            false,
            "neither the default browser nor Edge showed the boxes to UI Automation");
        return;
      }

      _browserBox = user;

      var userWords = new List<int>();
      var secretKeys = new List<int>();
      for (int round = 0; round < _rounds; round++) {
        Input.Click(user);
        userWords.Add(TypeTwoWords("qwe", "rty"));
        Input.Click(secret);
        secretKeys.Add(TypeTwoWords("asd", "fgh"));
      }

      ExpectRounds("3.3 click", userWords, secretKeys);

      userWords.Clear();
      secretKeys.Clear();
      Input.Click(user);
      for (int round = 0; round < _rounds; round++) {
        Input.Tab();
        secretKeys.Add(TypeTwoWords("asd", "fgh"));
        Input.Tab(shift: true);
        userWords.Add(TypeTwoWords("qwe", "rty"));
      }

      ExpectRounds("3.3 Tab and Shift+Tab", userWords, secretKeys);
    }

    // Types two Words at once and returns the most Word keys the worker held meanwhile.
    // Returns -1 when the worker did not report every key typed.
    private int TypeTwoWords(string first, string second) {
      EventLog.Position mark = _log.Mark();
      Input.Type(first);
      Input.Space();
      Input.Type(second);
      int expected = first.Length + second.Length + 1;
      if (WaitForKeys(mark, expected).Count < expected) {
        return -1;
      }

      return _log.EventsSince(mark).Select(seen => seen.WordKeys).DefaultIfEmpty(0).Max();
    }

    private void ExpectRounds(string path, List<int> userWords, List<int> secretKeys) {
      int unseen = userWords.Concat(secretKeys).Count(keys => keys < 0);
      Expect(
          $"{path}: every typed key reached the worker",
          unseen == 0,
          $"keys missing in {unseen} of {userWords.Count + secretKeys.Count} rounds");
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
      int keys = WaitForKeys(typed, 3).Count;
      int most = _log.EventsSince(mark).Select(seen => seen.WordKeys).DefaultIfEmpty(0).Max();
      Expect(item, keys == 3 && most == 0, $"{keys} of 3 keys seen, most Word keys {most}");
    }
  }
}
