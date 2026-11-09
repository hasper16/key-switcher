using Rekey.Core.Languages;

namespace Rekey.Core.Detection {
  /// <summary>
  /// Holds the Words to replace and the Layout to switch to; a confirmed run of Deferred Words is
  /// corrected together with the Word that confirmed it.
  /// </summary>
  /// <param name="Target">The Layout to switch to.</param>
  /// <param name="Texts">The Renderings that replace the Words, oldest first.</param>
  public sealed record Correction(LayoutId Target, IReadOnlyList<string> Texts) {
    /// <inheritdoc />
    public bool Equals(Correction? other) {
      return other is not null && Target == other.Target && Texts.SequenceEqual(other.Texts);
    }

    /// <inheritdoc />
    public override int GetHashCode() {
      return HashCode.Combine(Target, Texts.Count);
    }
  }
}
