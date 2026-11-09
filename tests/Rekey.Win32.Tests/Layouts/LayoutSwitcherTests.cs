using Rekey.Core;
using Rekey.Core.Languages;
using Rekey.Win32.Layouts;
using Xunit;

namespace Rekey.Win32.Tests.Layouts {
  // Spec layout-management: "Verified Layout Switch".
  public sealed class LayoutSwitcherTests {
    private static readonly LayoutId _englishUs = new(0x409);
    private static readonly LayoutId _russian = new(0x419);

    private readonly ManualTime _time = new();
    private readonly FakeWindow _window = new() { Layout = _englishUs };

    [Fact]
    public void SwitchAsksTheWindowForTheLayout() {
      new LayoutSwitcher(_window, _time).Switch(_russian);

      Assert.Equal([_russian], _window.Requests);
    }

    [Fact]
    public void LandedSwitchIsAdoptedWithoutIncident() {
      var switcher = new LayoutSwitcher(_window, _time);
      switcher.Switch(_russian);

      _time.Advance(TimeSpan.FromMilliseconds(50));
      _window.Layout = _russian;

      Assert.Equal(new SwitchResult(_russian, null), switcher.Check());
      Assert.False(switcher.IsPending);
    }

    [Fact]
    public void SwitchIsPendingUntilFiveHundredMilliseconds() {
      var switcher = new LayoutSwitcher(_window, _time);
      switcher.Switch(_russian);

      _time.Advance(TimeSpan.FromMilliseconds(499));

      Assert.Null(switcher.Check());
      Assert.True(switcher.IsPending);
    }

    [Fact]
    public void SwitchThatDidNotLandAdoptsTheRealLayoutAndRecordsAnIncident() {
      var switcher = new LayoutSwitcher(_window, _time);
      switcher.Switch(_russian);

      _time.Advance(TimeSpan.FromMilliseconds(500));
      SwitchResult? result = switcher.Check();

      Assert.Equal(_englishUs, result?.Adopted);
      Assert.Equal(IncidentType.SwitchNotLanded, result?.Incident?.Type);
      Assert.Equal(_time.GetUtcNow(), result?.Incident?.Time);
      Assert.False(switcher.IsPending);
    }

    [Fact]
    public void NothingIsCheckedWithoutASwitch() {
      Assert.Null(new LayoutSwitcher(_window, _time).Check());
    }

    private sealed class FakeWindow : ISwitchTarget {
      public LayoutId? Layout { get; set; }

      public List<LayoutId> Requests { get; } = [];

      public LayoutId? CurrentLayout() {
        return Layout;
      }

      public void RequestLayout(LayoutId layout) {
        Requests.Add(layout);
      }
    }
  }
}
