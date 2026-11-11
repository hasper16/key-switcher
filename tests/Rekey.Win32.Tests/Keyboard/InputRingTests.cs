using Rekey.Win32.Keyboard;
using Xunit;

namespace Rekey.Win32.Tests.Keyboard {
  public sealed class InputRingTests {
    [Fact]
    public void ReadsInTheOrderWritten() {
      var ring = new InputRing(capacity: 4);
      ring.TryWrite(Input(1));
      ring.TryWrite(Input(2));

      Assert.True(ring.TryRead(out RawInput first));
      Assert.True(ring.TryRead(out RawInput second));
      Assert.False(ring.TryRead(out _));
      Assert.Equal([1L, 2L], [first.Tick, second.Tick]);
    }

    [Fact]
    public void FullRingDropsAndCountsTheNewInput() {
      var ring = new InputRing(capacity: 2);

      bool[] written = [ring.TryWrite(Input(1)), ring.TryWrite(Input(2)), ring.TryWrite(Input(3))];

      Assert.Equal([true, true, false], written);
      Assert.Equal(1, ring.Dropped);
      Assert.Equal(2, ring.Count);
    }

    [Fact]
    public void WrapsAroundAfterReads() {
      var ring = new InputRing(capacity: 2);
      var ticks = new List<long>();
      for (long tick = 1; tick <= 5; tick++) {
        ring.TryWrite(Input(tick));
        ring.TryRead(out RawInput read);
        ticks.Add(read.Tick);
      }

      Assert.Equal([1L, 2L, 3L, 4L, 5L], ticks);
      Assert.Equal(0, ring.Count);
    }

    [Fact]
    public void InputsAreNumberedInWriteOrder() {
      var ring = new InputRing(capacity: 2);
      var sequences = new List<long>();
      for (long tick = 1; tick <= 3; tick++) {
        ring.TryWrite(Input(tick));
        ring.TryRead(out RawInput read);
        sequences.Add(read.Sequence);
      }

      Assert.Equal([0L, 1L, 2L], sequences);
      Assert.Equal(3, ring.Written);
    }

    [Fact]
    public void CapacityMustBeAPowerOfTwo() {
      Assert.Throws<ArgumentOutOfRangeException>(() => new InputRing(capacity: 3));
    }

    private static RawInput Input(long tick) {
      return new RawInput(RawInputKind.KeyDown, 0x41, 0x1E, false, tick);
    }
  }
}
