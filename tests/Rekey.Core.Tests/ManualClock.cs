namespace Rekey.Core.Tests {
  // A wall clock the test moves by hand.
  internal sealed class ManualClock : TimeProvider {
    public DateTimeOffset Now { get; private set; } = new(2026, 11, 12, 12, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() {
      return Now;
    }

    public void Advance(TimeSpan by) {
      Now += by;
    }
  }
}
