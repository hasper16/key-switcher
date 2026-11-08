using System.Security.Principal;
using Xunit;

namespace Rekey.Win32.Tests {
  public sealed class SingleInstanceGuardTests {
    // A unique name keeps the tests away from a Rekey instance running on the same machine.
    private readonly string _name = $@"Local\Rekey-Test-{Guid.NewGuid():N}";

    [Fact]
    public void SecondAcquireInTheSameSessionFails() {
      using SingleInstanceGuard? first = SingleInstanceGuard.TryAcquire(_name);
      using SingleInstanceGuard? second = SingleInstanceGuard.TryAcquire(_name);

      Assert.NotNull(first);
      Assert.Null(second);
    }

    [Fact]
    public void ReleasedNameCanBeAcquiredAgain() {
      SingleInstanceGuard? first = SingleInstanceGuard.TryAcquire(_name);
      Assert.NotNull(first);
      first.Dispose();

      using SingleInstanceGuard? again = SingleInstanceGuard.TryAcquire(_name);

      Assert.NotNull(again);
    }

    [Fact]
    public void CurrentUserNameIsSessionScopedAndCarriesTheUserSid() {
      string sid = WindowsIdentity.GetCurrent().User!.Value;

      string name = SingleInstanceGuard.NameForCurrentUser();

      Assert.StartsWith(@"Local\", name, StringComparison.Ordinal);
      Assert.Contains(sid, name, StringComparison.Ordinal);
    }
  }
}
