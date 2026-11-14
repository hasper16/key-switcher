using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Rekey.Core;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.Threading;

namespace Rekey.Win32.Focus {
  /// <summary>
  /// Reads the identity of a process and whether it runs with higher privileges than Rekey.
  /// </summary>
  [SuppressMessage(
      "Security",
      "S6640:Avoid using unsafe code blocks",
      Justification = "GetTokenInformation writes TOKEN_ELEVATION through a pointer.")]
  internal static unsafe class ProcessFacts {
    private static readonly bool _rekeyElevated = ReadRekeyElevation();

    /// <summary>
    /// Returns the application identity of a process (its full image path and, for a packaged
    /// app, its application user model id) and whether it is more privileged than Rekey.
    /// </summary>
    /// <remarks>
    /// A process Rekey cannot even query counts as more privileged: Windows hides its input from
    /// Rekey's hook the same way.
    /// </remarks>
    public static (AppId App, bool IsElevated) Read(uint processId) {
      using SafeFileHandle process = PInvoke.OpenProcess_SafeHandle(
          PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
      if (process.IsInvalid) {
        return (new AppId(string.Empty), !_rekeyElevated);
      }

      var app = new AppId(ImagePath(process), Aumid(process));
      return (app, !_rekeyElevated && (IsElevated(process) ?? true));
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

    // Null for a process without package identity, which is most desktop apps.
    private static string? Aumid(SafeHandle process) {
      // APPLICATION_USER_MODEL_ID_MAX_LENGTH is 130 characters.
      Span<char> id = stackalloc char[256];
      uint length = (uint)id.Length;
      WIN32_ERROR error = PInvoke.GetApplicationUserModelId(process, ref length, id);

      // The length counts the terminating null.
      return error == WIN32_ERROR.ERROR_SUCCESS && length > 1
          ? id[..(int)(length - 1)].ToString()
          : null;
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
