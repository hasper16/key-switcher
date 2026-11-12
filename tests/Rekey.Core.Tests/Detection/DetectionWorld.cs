using Rekey.Core.Detection;
using Rekey.Core.Languages;

namespace Rekey.Core.Tests.Detection {
  // Bundled profiles with captured Key Maps: English (US), Russian and Ukrainian.
  internal static class DetectionWorld {
    private static readonly LanguageSet _bundled =
        LanguageSet.LoadBundled(DateTimeOffset.UnixEpoch).Languages;

    public static LanguageSet Bundled => _bundled;

    public static LanguageProfile English { get; } = Profile(Language.English);

    public static LanguageProfile Russian { get; } = Profile(Language.Russian);

    public static LanguageProfile Ukrainian { get; } = Profile(Language.Ukrainian);

    public static WrongLayoutDetector Detector { get; } = new([
        new DetectionLayout(KeyMapFixtures.EnglishUs, English),
        new DetectionLayout(KeyMapFixtures.Russian, Russian),
        new DetectionLayout(KeyMapFixtures.Ukrainian, Ukrainian)]);

    public static IReadOnlyList<Keystroke> Keys(KeyMap layout, string typed) {
      return layout.Type(typed, capsLock: false) ?? throw new ArgumentException(typed);
    }

    public static Decision Decide(
        KeyMap layout, string typed, Sensitivity sensitivity = Sensitivity.Balanced) {
      return Detector.Decide(Keys(layout, typed), layout.Layout, sensitivity);
    }

    private static LanguageProfile Profile(Language language) {
      return _bundled.Profiles.Single(profile => profile.Language == language);
    }
  }
}
