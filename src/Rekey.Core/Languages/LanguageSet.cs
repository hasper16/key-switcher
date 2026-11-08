namespace Rekey.Core.Languages {
  /// <summary>
  /// Holds the loaded Language Profiles and decides which Languages the installed Layouts make
  /// active.
  /// </summary>
  public sealed class LanguageSet {
    /// <summary>
    /// Initializes a new instance of the <see cref="LanguageSet" /> class.
    /// </summary>
    /// <param name="profiles">The loaded Language Profiles.</param>
    public LanguageSet(IEnumerable<LanguageProfile> profiles) {
      Profiles = [.. profiles];
    }

    /// <summary>
    /// Gets the loaded Language Profiles.
    /// </summary>
    public IReadOnlyList<LanguageProfile> Profiles { get; }

    /// <summary>
    /// Loads the Language Profiles shipped inside Rekey, skipping each one that cannot be read or
    /// fails validation.
    /// </summary>
    /// <param name="now">The time to stamp on Incidents.</param>
    /// <returns>The loaded profiles and an Incident for each skipped one.</returns>
    public static LanguageSetLoad LoadBundled(DateTimeOffset now) {
      return Load(
          Enum.GetValues<Language>().Select(language => KeyValuePair.Create(
              language, (Func<Stream>)(() => OpenBundled(language)))),
          now);
    }

    /// <summary>
    /// Loads Language Profiles, skipping each one that cannot be read or fails validation.
    /// </summary>
    /// <param name="sources">
    /// The Language each file must describe and a way to open the file.
    /// </param>
    /// <param name="now">The time to stamp on Incidents.</param>
    /// <returns>The loaded profiles and an Incident for each skipped one.</returns>
    public static LanguageSetLoad Load(
        IEnumerable<KeyValuePair<Language, Func<Stream>>> sources, DateTimeOffset now) {
      var profiles = new List<LanguageProfile>();
      var incidents = new List<Incident>();
      foreach ((Language language, Func<Stream> open) in sources) {
        LanguageProfile? profile = TryLoad(language, open);
        if (profile is null) {
          incidents.Add(new Incident(now, IncidentType.LanguageProfileSkipped, [language]));
        } else {
          profiles.Add(profile);
        }
      }

      return new LanguageSetLoad(new LanguageSet(profiles), incidents);
    }

    /// <summary>
    /// Returns the profiles of the Languages that the installed Layouts type.
    /// </summary>
    /// <remarks>
    /// Call it again whenever the installed Layouts change; it keeps no state.
    /// </remarks>
    /// <param name="installedLayouts">The Layouts installed for the current user.</param>
    /// <returns>The active profiles, in load order.</returns>
    public IReadOnlyList<LanguageProfile> Activate(IEnumerable<LayoutId> installedLayouts) {
      var active = installedLayouts.Select(ProfileFor).OfType<LanguageProfile>().ToHashSet();
      return [.. Profiles.Where(active.Contains)];
    }

    /// <summary>
    /// Returns the profile of the Language a Layout types.
    /// </summary>
    /// <remarks>
    /// A Layout listed by a profile maps to it. Otherwise a profile listing a Layout with the same
    /// language id (the low word of the KLID) claims it, so unlisted variants still map.
    /// </remarks>
    /// <param name="layout">The Layout to map.</param>
    /// <returns>
    /// The profile, or <see langword="null" /> when no profile covers the Layout.
    /// </returns>
    public LanguageProfile? ProfileFor(LayoutId layout) {
      return Profiles.FirstOrDefault(profile => profile.LayoutIds.Contains(layout))
          ?? Profiles.FirstOrDefault(profile => profile.LayoutIds.Any(
              listed => listed.LanguageId == layout.LanguageId));
    }

    private static Stream OpenBundled(Language language) {
      string name = $"Rekey.Core.Profiles.{LanguageProfileJson.FileNameOf(language)}";
      return typeof(LanguageSet).Assembly.GetManifestResourceStream(name)
          ?? throw new FileNotFoundException("The Language Profile is not bundled.", name);
    }

    private static LanguageProfile? TryLoad(Language language, Func<Stream> open) {
      try {
        using Stream stream = open();
        LanguageProfile profile = LanguageProfileLoader.Load(stream);
        return profile.Language == language ? profile : null;
      } catch (Exception e)
          when (e is ProfileLoadException or IOException or UnauthorizedAccessException) {
        return null;
      }
    }
  }
}
