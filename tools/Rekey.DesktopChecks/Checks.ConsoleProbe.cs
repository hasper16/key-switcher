using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Automation;
using Rekey.Core.Languages;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace Rekey.DesktopChecks {
  // Finds out how a console takes typed text, for Rekey's replacement there. Runs without a
  // Word Evaluator: every key is the probe's own. Each way is tried in the default console
  // (Windows Terminal or conhost, as Windows is set) and in a classic conhost window. Reports
  // booleans, window classes and counts only, never text.
  internal sealed partial class Checks {
    private const string _defaultTitle = "Rekey probe default console";
    private const string _conhostTitle = "Rekey probe conhost";

    public int RunConsoleProbe() {
      Begin(minutes: 2);
      string shell = Path.Join(Environment.SystemDirectory, "cmd.exe");
      string conhost = Path.Join(Environment.SystemDirectory, "conhost.exe");
      ProbeConsole("default console", OpenProbeConsole(shell, [], _defaultTitle), _defaultTitle);
      ProbeConsole(
          "conhost", OpenProbeConsole(conhost, [shell], _conhostTitle), _conhostTitle);
      return End();
    }

    private static string ClassOf(HWND window) {
      Span<char> name = stackalloc char[64];
      int length = PInvoke.GetClassName(window, name);
      return name[..Math.Max(length, 0)].ToString();
    }

    private static string? BufferText(string title) {
      HWND? window = Desktop.FindWindow(title, TimeSpan.FromSeconds(1));
      var hasText = new PropertyCondition(AutomationElement.IsTextPatternAvailableProperty, true);
      AutomationElement? buffer = window is { } found
          ? AutomationElement.FromHandle(found).FindFirst(TreeScope.Subtree, hasText)
          : null;
      return TextOf(buffer);
    }

    // Starts a console running cmd with the title; closed when the checks end.
    private HWND? OpenProbeConsole(string program, string[] arguments, string title) {
      var start = new ProcessStartInfo(program) { UseShellExecute = true };
      foreach (string argument in arguments) {
        start.ArgumentList.Add(argument);
      }

      start.ArgumentList.Add("/k");
      start.ArgumentList.Add($"title {title}");
      Process process = Process.Start(start)
          ?? throw new InvalidOperationException("The console did not start.");
      _restorer.Defer($"{title} window", () => {
        Kill(process);
        process.Dispose();
      });
      return Desktop.FindWindow(title, TimeSpan.FromSeconds(10));
    }

    private void ProbeConsole(string host, HWND? window, string title) {
      if (window is not { } found) {
        Expect($"console probe ({host})", false, "the console window did not appear");
        return;
      }

      Point at = Desktop.Center(found);
      Expect($"console probe ({host}): window class", true, ClassOf(found));
      LayoutId english = SpecWords.EnglishUs;
      LayoutId russian = SpecWords.Russian;
      Probe(host, title, "Backspaces then Unicode, one call", at, english, () => {
        Input.Type("ghbdtn");
        Input.TypeUnicode("привет ", backspaces: 6, oneCallEach: false);
      });
      Probe(host, title, "Unicode alone, one call", at, english, () => {
        Input.TypeUnicode("привет ", backspaces: 0, oneCallEach: false);
      });
      Probe(host, title, "Backspaces then Unicode, one call per key", at, english, () => {
        Input.Type("ghbdtn");
        Input.TypeUnicode("привет ", backspaces: 6, oneCallEach: true);
      });
      Probe(host, title, "Backspaces, 150 ms pause, then Unicode", at, english, () => {
        Input.Type("ghbdtn");
        Input.TypeUnicode(
            "привет ", 6, oneCallEach: false, TimeSpan.FromMilliseconds(150));
      });
      Probe(host, title, "the keys typed in the Russian Layout", at, russian, () => {
        Input.Type("ghbdtn");
        Input.Space();
      });
      Probe(host, title, "the keys by scan code alone in the Russian Layout", at, russian, () => {
        Input.TypeByScanCode("ghbdtn ");
      });
      Probe(host, title, "erase, verified switch to Russian, retype the keys", at, english, () => {
        Input.Type("ghbdtn");
        Input.Backspaces(6);
        _ = SwitchIn(at, russian);
        Input.Type("ghbdtn");
        Input.Space();
      });
    }

    // Empties the prompt line in the Layout, types, then reports what the console shows.
    private void Probe(
        string host, string title, string variant, Point at, LayoutId layout, Action type) {
      bool switched = SwitchIn(at, layout);
      Input.Escape();
      type();
      Thread.Sleep(_settle);
      string? text = BufferText(title);
      bool corrected = text?.Contains("привет ", StringComparison.Ordinal) == true;
      bool typed = text?.Contains("ghbdtn", StringComparison.Ordinal) == true;
      string detail = $"Layout switched {switched}, corrected Word and Space shown {corrected}, "
          + $"typed Word left {typed}, buffer read {text is not null}";
      Expect($"console probe ({host}): {variant}", corrected && !typed, detail);
      Input.Escape();
    }
  }
}
