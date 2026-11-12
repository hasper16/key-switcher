using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Rekey.Win32.Focus {
  /// <summary>
  /// Finds the console host thread that runs a classic console window.
  /// </summary>
  /// <remarks>
  /// For compatibility, Windows reports a classic console window (<c>ConsoleWindowClass</c>)
  /// as owned by its client, such as cmd.exe. That thread has no windows and keeps the Layout
  /// it started with, while the console host thread translates the keys; reading the client's
  /// thread gives a stale Layout. Call it on the worker thread.
  /// </remarks>
  internal static class ConsoleThreads {
    private const string _consoleClass = "ConsoleWindowClass";

    // The latest console window and its host thread, checked again before each use.
    private static HWND _lastWindow;
    private static uint _lastThread;

    /// <summary>
    /// Returns the thread whose active window is the console window.
    /// </summary>
    /// <param name="window">The console window.</param>
    /// <param name="threads">The threads that may run it.</param>
    /// <param name="activeWindowOf">
    /// Returns a thread's active window, or <see langword="null" /> when it has no GUI state.
    /// </param>
    /// <returns>The thread, or <see langword="null" /> when none runs the window.</returns>
    public static uint? Pick(
        HWND window, IEnumerable<uint> threads, Func<uint, HWND?> activeWindowOf) {
      foreach (uint thread in threads) {
        if (activeWindowOf(thread) == window) {
          return thread;
        }
      }

      return null;
    }

    /// <summary>
    /// Returns the thread that runs the window when it is a classic console window, or
    /// <see langword="null" /> for any other window or when no host thread is found.
    /// </summary>
    public static uint? HostThreadOf(HWND window) {
      Span<char> name = stackalloc char[32];
      int length = PInvoke.GetClassName(window, name);
      if (!name[..Math.Max(length, 0)].SequenceEqual(_consoleClass)) {
        return null;
      }

      if (window == _lastWindow && ActiveWindowOf(_lastThread) == window) {
        return _lastThread;
      }

      uint? thread = Pick(window, HostThreads(), ActiveWindowOf);
      (_lastWindow, _lastThread) = (window, thread ?? 0);
      return thread;
    }

    private static IEnumerable<uint> HostThreads() {
      foreach (string name in new[] { "conhost", "OpenConsole" }) {
        foreach (Process process in Process.GetProcessesByName(name)) {
          using (process) {
            uint[] ids;
            try {
              ids = [.. process.Threads.Cast<ProcessThread>().Select(t => (uint)t.Id)];
            } catch (InvalidOperationException) {
              // The process ended while it was listed.
              continue;
            }

            foreach (uint id in ids) {
              yield return id;
            }
          }
        }
      }
    }

    private static HWND? ActiveWindowOf(uint thread) {
      var info = new GUITHREADINFO { cbSize = (uint)Marshal.SizeOf<GUITHREADINFO>() };
      return thread != 0 && PInvoke.GetGUIThreadInfo(thread, ref info) ? info.hwndActive : null;
    }
  }
}
