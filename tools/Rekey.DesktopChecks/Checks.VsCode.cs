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
    private const string _vsCodeFile = "rekey-check.txt";

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
      string file = Path.Join(folder, _vsCodeFile);
      File.WriteAllText(file, string.Empty);
      _restorer.Defer("VS Code test folder", () => Directory.Delete(folder, recursive: true));
      Process vsCode = StartVsCode(code, folder, file);
      _restorer.Defer("VS Code", () => {
        if (!vsCode.HasExited) {
          vsCode.Kill(entireProcessTree: true);
          vsCode.WaitForExit(TimeSpan.FromSeconds(5));
        }

        vsCode.Dispose();
      });
      HWND? window = Desktop.FindWindow(_vsCodeFile, TimeSpan.FromSeconds(40));
      if (window is not { } found) {
        Expect("5.4 VS Code", false, "the VS Code window did not appear");
        return;
      }

      // Below the always-on-top test windows, so clicks reach the editor.
      PInvoke.MoveWindow(found, 80, 320, 900, 500, true);
      Thread.Sleep(TimeSpan.FromSeconds(2));
      Point editor = Desktop.Center(found);
      var results = new List<ReplacementResult?>();
      for (int attempt = 0; attempt < 3; attempt++) {
        results.Add(CorrectIn(
            editor,
            () => {
              Input.Type("ghbdtn");
              Input.Space();
            },
            clear: false));
      }

      Input.Control('s');
      Thread.Sleep(TimeSpan.FromSeconds(1));
      ExpectVsCodeText(File.ReadAllText(file), results);
    }

    private void ExpectVsCodeText(string text, List<ReplacementResult?> results) {
      string[] words = text.Split([' ', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
      bool damaged = words.Any(word => word is not ("ghbdtn" or "привет"));
      bool lastCorrected = words.Length > 0 && words[^1] == "привет";
      string detail = $"words {words.Length} of 3, damaged {damaged}, last corrected "
          + $"{lastCorrected}, replacements {string.Join('/', results)} {_trace}";
      Expect(
          "5.4 VS Code: no Word damaged, and the third Word corrected",
          words.Length == 3 && !damaged && lastCorrected,
          detail);
    }
  }
}
