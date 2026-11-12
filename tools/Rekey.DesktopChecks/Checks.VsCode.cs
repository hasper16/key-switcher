using System.Diagnostics;
using System.Drawing;
using System.IO;
using Rekey.Win32.Replacement;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace Rekey.DesktopChecks {
  // Task 5.4, when VS Code is installed: where its text readback is unreliable, the first two
  // Corrections are cancelled and later ones are retyped; no Word may be damaged either way.
  // Runs a separate VS Code with a throwaway profile, never the user's own, and reads the
  // result from the saved file, since VS Code's UI Automation text cannot be trusted. Reports
  // counts only.
  internal sealed partial class Checks {
    private static string? VsCodePath() {
      string[] candidates = [
          Path.Join(
              Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
              "Programs",
              "Microsoft VS Code",
              "Code.exe"),
          Path.Join(
              Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
              "Microsoft VS Code",
              "Code.exe"),
      ];
      return candidates.FirstOrDefault(File.Exists);
    }

    // As in a default install, spelled out so the owner's case does not hang on VS Code's
    // defaults: word-based suggestions pop up while a Word is typed, and the popup's selected row
    // takes UI Automation focus while keys still go to the editor.
    private static void WriteVsCodeSettings(string folder) {
      string user = Path.Join(folder, "profile", "User");
      Directory.CreateDirectory(user);
      File.WriteAllText(
          Path.Join(user, "settings.json"),
          """
          {
            "editor.quickSuggestions": { "other": "on", "comments": "on", "strings": "on" },
            "editor.quickSuggestionsDelay": 10,
            "editor.wordBasedSuggestions": "matchingDocuments",
            "editor.suggest.showWords": true,
            "workbench.startupEditor": "none",
            "update.mode": "none"
          }
          """);
    }

    private static Process StartVsCode(string code, string folder, string file) {
      var start = new ProcessStartInfo(code) { UseShellExecute = false };
      foreach (string argument in new[] {
          "--new-window", "--user-data-dir", Path.Join(folder, "profile"), "--extensions-dir",
          Path.Join(folder, "extensions"), "--disable-extensions", "--disable-workspace-trust",
          "--skip-welcome", "--skip-release-notes", file,
      }) {
        start.ArgumentList.Add(argument);
      }

      return Process.Start(start)
          ?? throw new InvalidOperationException("VS Code did not start.");
    }

    private void CheckVsCode() {
      if (VsCodePath() is not { } code) {
        Console.WriteLine("   skipped: 5.4 VS Code is not installed");
        return;
      }

      string folder = Path.Join(Path.GetTempPath(), $"rekey-desktop-check-{Guid.NewGuid():N}");
      Directory.CreateDirectory(folder);
      string fileName = $"rekey-check-{Guid.NewGuid():N}.txt";
      string file = Path.Join(folder, fileName);
      File.WriteAllText(file, string.Empty);
      _restorer.Defer("VS Code test folder", () => Directory.Delete(folder, recursive: true));
      WriteVsCodeSettings(folder);
      Process vsCode = StartVsCode(code, folder, file);
      _restorer.Defer("VS Code", () => {
        if (!vsCode.HasExited) {
          vsCode.Kill(entireProcessTree: true);
          vsCode.WaitForExit(TimeSpan.FromSeconds(5));
        }

        vsCode.Dispose();
      });
      HWND? window = Desktop.FindWindow(fileName, TimeSpan.FromSeconds(40));
      if (window is not { } found) {
        Expect("5.4 VS Code", false, "the VS Code window did not appear");
        return;
      }

      // Below the always-on-top test windows, so clicks reach the editor.
      PInvoke.MoveWindow(found, 80, 320, 900, 500, true);
      Thread.Sleep(TimeSpan.FromSeconds(2));
      Point editor = Desktop.Center(found);

      // As the owner tried it: one click, then three Words on one line, no click between, at
      // a person's pace, so the suggestion popup opens while the second and third Words are
      // typed.
      Action word = () => {
        foreach (char letter in "ghbdtn") {
          Input.Type(letter.ToString());
          Thread.Sleep(TimeSpan.FromMilliseconds(150));
        }

        Input.Space();
      };
      int keptBefore = Volatile.Read(ref _focusMovesKeptWord);
      var results = new List<ReplacementResult?> { CorrectIn(editor, word, clear: false) };
      var traces = new List<string> { _trace };
      for (int attempt = 1; attempt < 3; attempt++) {
        results.Add(TypeAndWait(word, _log.Mark(), switched: true));
        traces.Add(_trace);
      }

      _trace = string.Join(' ', traces);

      Input.Control('s');
      Thread.Sleep(TimeSpan.FromSeconds(1));
      int kept = Volatile.Read(ref _focusMovesKeptWord) - keptBefore;
      ExpectVsCodeText(File.ReadAllText(file), results, kept);
    }

    // The suggestion list must have taken focus while typing, or the run proves nothing about
    // design D7.
    private void ExpectVsCodeText(string text, List<ReplacementResult?> results, int kept) {
      string[] words = text.Split([' ', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
      bool damaged = words.Any(word => word is not ("ghbdtn" or "привет"));
      bool lastCorrected = words.Length > 0 && words[^1] == "привет";

      // The text alone cannot show a replacement: a Word typed in Russian reads the same.
      bool replaced = results[^1] is ReplacementResult.Verified or ReplacementResult.Retyped;
      string detail = $"words {words.Length} of 3, damaged {damaged}, last corrected "
          + $"{lastCorrected}, replacements {string.Join('/', results)}, focus moves that kept "
          + $"the Word {kept} {_trace}";
      Expect(
          "5.4 VS Code: no Word damaged, and the third Word corrected by a replacement while "
              + "the suggestion list took focus",
          words.Length == 3 && !damaged && lastCorrected && replaced && kept > 0,
          detail);
    }
  }
}
