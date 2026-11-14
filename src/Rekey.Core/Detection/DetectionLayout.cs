using System.Collections.Frozen;
using Rekey.Core.Languages;

namespace Rekey.Core.Detection {
  /// <summary>
  /// Pairs an active Layout's Key Map with the Language Profile of the Language it types.
  /// </summary>
  /// <param name="KeyMap">What the Layout's keys type.</param>
  /// <param name="Profile">The profile of the Language the Layout types.</param>
  /// <param name="Learned">The user's Learned Words of that Language, normalised.</param>
  public sealed record DetectionLayout(
      KeyMap KeyMap, LanguageProfile Profile, IReadOnlySet<string> Learned) {
    /// <summary>
    /// Initializes a new instance of the <see cref="DetectionLayout" /> class without Learned
    /// Words.
    /// </summary>
    /// <param name="keyMap">What the Layout's keys type.</param>
    /// <param name="profile">The profile of the Language the Layout types.</param>
    public DetectionLayout(KeyMap keyMap, LanguageProfile profile)
        : this(keyMap, profile, FrozenSet<string>.Empty) {
    }
  }
}
