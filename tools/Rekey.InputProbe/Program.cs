using System.Globalization;
using Rekey.Core;
using Rekey.Core.Languages;
using Rekey.Win32;
using Windows.Win32;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace Rekey.InputProbe {
  /// <summary>
  /// Prints what Rekey's worker sees, for the manual checks in
  /// <c>docs/manual-tests/add-keyboard-capture-and-layouts.md</c>.
  /// </summary>
  /// <remarks>
  /// Events print through their text form, which never carries typed characters.
  /// </remarks>
  internal static class Program {
    private static int Main(string[] args) {
      using var worker = new InputWorker(new Settings(), Print, TimeProvider.System);
      worker.Observed += inputEvent => Console.WriteLine(Describe(inputEvent));
      worker.Start();
      Console.WriteLine("Rekey input probe. Press Ctrl+C in this window to quit.");

      if (args.Contains("--inject")) {
        Inject();
      }

      int switchAt = Array.IndexOf(args, "--switch");
      if (switchAt >= 0 && switchAt + 1 < args.Length
          && LayoutId.TryParse(args[switchAt + 1], out LayoutId target)) {
        Console.WriteLine($"Focus the window to switch; switching to {Klid(target)} in 5 seconds.");
        Thread.Sleep(TimeSpan.FromSeconds(5));
        worker.SwitchLayout(target);
      }

      using var quit = new ManualResetEventSlim();
      Console.CancelKeyPress += (_, e) => {
        e.Cancel = true;
        quit.Set();
      };
      quit.Wait();
      return 0;
    }

    private static string Describe(InputEvent inputEvent) {
      return inputEvent switch {
        LayoutsInstalledChanged installed =>
            $"LayoutsInstalledChanged {string.Join(' ', installed.Layouts.Select(Klid))}",
        LayoutChanged changed => $"LayoutChanged {Klid(changed.Layout)}",
        _ => inputEvent.ToString(),
      };
    }

    private static string Klid(LayoutId layout) {
      return layout.Value.ToString("X8", CultureInfo.InvariantCulture);
    }

    private static void Print(Incident incident) {
      Console.WriteLine($"INCIDENT {incident.Type} {string.Join(',', incident.Details)}");
    }

    // Types "a" twice into the focused window: first tagged as Rekey's own input, then untagged.
    private static void Inject() {
      Console.WriteLine("Focus a text editor; injecting in 5 seconds.");
      Thread.Sleep(TimeSpan.FromSeconds(5));
      Console.WriteLine("Injecting a tagged key: expect no KeyDown line.");
      Press(InjectedInput.Tag);
      Thread.Sleep(TimeSpan.FromSeconds(1));
      Console.WriteLine("Injecting an untagged key: expect a KeyDown line.");
      Press(0);
    }

    private static void Press(nuint extraInfo) {
      INPUT[] inputs = [Key(extraInfo, 0), Key(extraInfo, KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP)];
      PInvoke.SendInput(inputs, System.Runtime.InteropServices.Marshal.SizeOf<INPUT>());
    }

    private static INPUT Key(nuint extraInfo, KEYBD_EVENT_FLAGS flags) {
      return new INPUT {
        type = INPUT_TYPE.INPUT_KEYBOARD,
        Anonymous = new INPUT._Anonymous_e__Union {
          ki = new KEYBDINPUT { wVk = VIRTUAL_KEY.VK_A, dwFlags = flags, dwExtraInfo = extraInfo },
        },
      };
    }
  }
}
