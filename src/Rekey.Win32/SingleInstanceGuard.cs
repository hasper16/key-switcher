using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace Rekey.Win32 {
  /// <summary>
  /// Holds the claim that this process is the one Rekey instance of the user's session.
  /// </summary>
  /// <remarks>
  /// The claim is a named mutex. A <c>Local\</c> name is scoped to the Windows session, so other
  /// sessions run their own instance. Disposing the guard releases the claim.
  /// </remarks>
  public sealed class SingleInstanceGuard : IDisposable {
    private readonly SafeFileHandle _mutex;

    private SingleInstanceGuard(SafeFileHandle mutex) {
      _mutex = mutex;
    }

    /// <summary>
    /// Returns the claim name for the current user in the current session.
    /// </summary>
    /// <returns>The name <c>Local\Rekey-</c> followed by the user's security identifier.</returns>
    public static string NameForCurrentUser() {
      using WindowsIdentity user = WindowsIdentity.GetCurrent();
      SecurityIdentifier sid =
          user.User ?? throw new InvalidOperationException("The process has no user identity.");
      return $@"Local\Rekey-{sid.Value}";
    }

    /// <summary>
    /// Claims <paramref name="name" /> for this process.
    /// </summary>
    /// <param name="name">
    /// The claim name, such as the one from <see cref="NameForCurrentUser" />.
    /// </param>
    /// <returns>
    /// The guard that holds the claim, or <see langword="null" /> when another holder has it.
    /// </returns>
    /// <exception cref="Win32Exception">Windows cannot create the named mutex.</exception>
    public static SingleInstanceGuard? TryAcquire(string name) {
      SafeFileHandle mutex = PInvoke.CreateMutex(null, false, name);
      int error = Marshal.GetLastPInvokeError();
      if (mutex.IsInvalid) {
        throw new Win32Exception(error);
      }

      if (error == (int)WIN32_ERROR.ERROR_ALREADY_EXISTS) {
        mutex.Dispose();
        return null;
      }

      return new SingleInstanceGuard(mutex);
    }

    /// <inheritdoc />
    public void Dispose() {
      _mutex.Dispose();
    }
  }
}
