using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Rekey.DesktopChecks {
  // Reads and changes the user settings the checks need, and how to put them back.
  [SuppressMessage(
      "Security",
      "S6640:Avoid using unsafe code blocks",
      Justification = "SystemParametersInfo takes the setting through a pointer.")]
  internal static class SystemState {
    // SPI_GETTHREADLOCALINPUTSETTINGS / SPI_SET...: "Let me use a different input method for
    // each app window".
    private const uint _getPerWindowInput = 0x104E;
    private const uint _setPerWindowInput = 0x104F;

    public static string DataFolder { get; } = Path.Join(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Rekey");

    // Where the language list is saved before the checks change it, so a crashed run can still
    // be undone with the restore-languages command.
    public static string LanguageSnapshot { get; } =
        Path.Join(Path.GetTempPath(), "rekey-desktop-checks-languages.txt");

    public static LanguageList ReadLanguages() {
      return LanguageList.Parse(PowerShell(LanguageList.ReadScript));
    }

    public static void AddLanguage(string tag) {
      PowerShell(
          $"$l = Get-WinUserLanguageList; $l.Add({Quote(tag)}); Set-WinUserLanguageList $l -Force");
    }

    // Removes what the checks added and leaves every other language object, with all its
    // settings, as it is; only when that does not match the snapshot does it rebuild the list.
    public static void RestoreLanguages(LanguageList snapshot) {
      IReadOnlyList<string> extra = snapshot.ExtraIn(ReadLanguages());
      if (extra.Count > 0) {
        PowerShell(LanguageList.RemoveScript(extra));
      }

      if (ReadLanguages().SameAs(snapshot)) {
        return;
      }

      PowerShell(snapshot.WriteScript());
      if (!ReadLanguages().SameAs(snapshot)) {
        throw new InvalidOperationException(
            "The language list differs after restoring it; run the restore-languages command.");
      }
    }

    public static unsafe bool PerWindowInput() {
      int value = 0;
      if (!PInvoke.SystemParametersInfo(
          (SYSTEM_PARAMETERS_INFO_ACTION)_getPerWindowInput, 0, &value, 0)) {
        throw new Win32Exception(Marshal.GetLastPInvokeError());
      }

      return value != 0;
    }

    public static unsafe void SetPerWindowInput(bool on) {
      const SYSTEM_PARAMETERS_INFO_UPDATE_FLAGS save =
          SYSTEM_PARAMETERS_INFO_UPDATE_FLAGS.SPIF_UPDATEINIFILE
          | SYSTEM_PARAMETERS_INFO_UPDATE_FLAGS.SPIF_SENDCHANGE;
      if (!PInvoke.SystemParametersInfo(
          (SYSTEM_PARAMETERS_INFO_ACTION)_setPerWindowInput, 0, (void*)(on ? 1 : 0), save)) {
        throw new Win32Exception(Marshal.GetLastPInvokeError());
      }
    }

    public static bool CapsLockOn() {
      return (PInvoke.GetKeyState((int)VIRTUAL_KEY.VK_CAPITAL) & 1) != 0;
    }

    public static Point Cursor() {
      PInvoke.GetCursorPos(out Point point);
      return point;
    }

    // Returns how to put a Rekey data file back as it was, present or absent.
    public static Action KeepFile(string name) {
      string path = Path.Join(DataFolder, name);
      byte[]? before = File.Exists(path) ? File.ReadAllBytes(path) : null;
      return () => {
        if (before is null) {
          File.Delete(path);
        } else {
          File.WriteAllBytes(path, before);
        }
      };
    }

    private static string Quote(string value) {
      return "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";
    }

    private static string PowerShell(string script) {
      string shell = Path.Join(
          Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe");
      var start = new ProcessStartInfo(shell) {
        RedirectStandardOutput = true,
        UseShellExecute = false,
        CreateNoWindow = true,
      };
      foreach (string arg in new[] { "-NoProfile", "-NonInteractive", "-Command", script }) {
        start.ArgumentList.Add(arg);
      }

      using Process process = Process.Start(start)
          ?? throw new InvalidOperationException("PowerShell did not start.");
      string output = process.StandardOutput.ReadToEnd();
      process.WaitForExit();
      if (process.ExitCode != 0) {
        throw new InvalidOperationException($"PowerShell exited with {process.ExitCode}.");
      }

      return output;
    }
  }
}
