namespace Rekey.Win32.Tests {
  // A clock the test moves by hand; timestamps count milliseconds.
  internal sealed class ManualTime : TimeProvider {
    private DateTimeOffset _now = new(2026, 11, 9, 12, 0, 0, TimeSpan.Zero);
    private long _timestamp = 1_000_000;

    public override long TimestampFrequency => 1000;

    public override DateTimeOffset GetUtcNow() {
      return _now;
    }

    public override long GetTimestamp() {
      return _timestamp;
    }

    public void Advance(TimeSpan by) {
      _now += by;
      _timestamp += (long)by.TotalMilliseconds;
    }
  }
}
