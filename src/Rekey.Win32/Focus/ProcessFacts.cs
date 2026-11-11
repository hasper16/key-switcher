using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.Security;
using Windows.Win32.System.Threading;

namespace Rekey.Win32.Focus {
  /// <summary>
  /// Reads the image path of a process and whether it runs with higher privileges than Rekey.
  /// </summary>
  [SuppressMessage(
      "Security",
      "S6640:Avoid using unsafe code blocks",
      Justification = "GetTokenInformation writes TOKEN_ELEVATION through a pointer.")]
  internal static unsafe class ProcessFacts {
    private static readonly bool _rekeyElevated = ReadRekeyElevation();

    /// <summary>
    /// Returns the full image path of a process and whether it is more privileged than Rekey.
    /// </summary>
    /// <remarks>
    /// A process Rekey cannot even query counts as more privileged: Windows hides its input from
    /// Rekey's hook the same way.
    /// </remarks>
    public static (string ImagePath, bool IsElevated) Read(uint processId) {
      using SafeFileHandle process = PInvoke.OpenProcess_SafeHandle(
          PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
      if (process.IsInvalid) {
        return (string.Empty, !_rekeyElevated);
      }

      return (ImagePath(process), !_rekeyElevated && (IsElevated(process) ?? true));
    }

    private static bool ReadRekeyElevation() {
      using SafeFileHandle self = PInvoke.GetCurrentProcess_SafeHandle();
      return IsElevated(self) ?? false;
    }

    private static string ImagePath(SafeHandle process) {
      Span<char> path = stackalloc char[1024];
      uint length = (uint)path.Length;
      bool read = PInvoke.QueryFullProcessImageName(
          process, PROCESS_NAME_FORMAT.PROCESS_NAME_WIN32, path, ref length);
      return read ? path[..(int)length].ToString() : string.Empty;
    }

    private static bool? IsElevated(SafeHandle process) {
      if (!PInvoke.OpenProcessToken(
          process, TOKEN_ACCESS_MASK.TOKEN_QUERY, out SafeFileHandle token)) {
        return null;
      }

      using (token) {
        TOKEN_ELEVATION elevation = default;
        bool read = PInvoke.GetTokenInformation(
            token,
            TOKEN_INFORMATION_CLASS.TokenElevation,
            &elevation,
            (uint)sizeof(TOKEN_ELEVATION),
            out _);
        return read ? elevation.TokenIsElevated != 0 : null;
      }
    }
  }
}
