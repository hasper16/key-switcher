using Rekey.Core;
using Rekey.Win32.Keyboard;
using Xunit;

namespace Rekey.Win32.Tests.Keyboard {
  // Design D1: a callback over 1 ms is counted and reported as an Incident in aggregate.
  public sealed class SlowCallbackMeterTests {
    private readonly ManualTime _time = new();

    [Fact]
    public void FastCallbacksReportNothing() {
      var meter = new SlowCallbackMeter(_time);
      Callback(meter, TimeSpan.FromMilliseconds(1));

      Assert.Null(meter.TakeIncident());
    }

    [Fact]
    public void SlowCallbackIsReportedOnce() {
      var meter = new SlowCallbackMeter(_time);
      Callback(meter, TimeSpan.FromMilliseconds(2));

      Incident? incident = meter.TakeIncident();

      Assert.Equal(IncidentType.SlowHookCallback, incident?.Type);
      Assert.Equal(_time.GetUtcNow(), incident?.Time);
      Assert.Null(meter.TakeIncident());
    }

    [Fact]
    public void SlowCallbacksWithinTenMinutesShareOneIncident() {
      var meter = new SlowCallbackMeter(_time);
      Callback(meter, TimeSpan.FromMilliseconds(5));
      meter.TakeIncident();

      _time.Advance(TimeSpan.FromMinutes(9));
      Callback(meter, TimeSpan.FromMilliseconds(5));

      Assert.Null(meter.TakeIncident());
    }

    [Fact]
    public void SlowCallbackAfterTenMinutesIsReportedAgain() {
      var meter = new SlowCallbackMeter(_time);
      Callback(meter, TimeSpan.FromMilliseconds(5));
      meter.TakeIncident();

      _time.Advance(TimeSpan.FromMinutes(9));
      Callback(meter, TimeSpan.FromMilliseconds(5));
      meter.TakeIncident();
      _time.Advance(TimeSpan.FromMinutes(1));

      Assert.NotNull(meter.TakeIncident());
    }

    private void Callback(SlowCallbackMeter meter, TimeSpan duration) {
      long started = _time.GetTimestamp();
      _time.Advance(duration);
      meter.Record(started, _time.GetTimestamp());
    }
  }
}
