using Rekey.Core.Languages;
using Rekey.Core.Words;
using Xunit;

namespace Rekey.Core.Tests.Words {
  // Spec word-learning: lists stay bounded; design D4: the least recently used automatic entry
  // goes first and user entries are never dropped.
  public sealed class WordListsTests {
    private static readonly DateTimeOffset _start = new(2026, 11, 16, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void MatchIgnoresCaseAndEdgePunctuation() {
      var lists = new WordLists();

      lists.Protect("Ntcn,", WordSource.User, _start);

      Assert.True(lists.IsProtected("NTCN"));
      Assert.True(lists.IsProtected("(ntcn)"));
      Assert.False(lists.IsProtected("ntc"));
    }

    [Fact]
    public void FullListDropsTheLeastRecentlyUsedAutomaticEntry() {
      WordLists lists = FullOfProtected(WordSource.Auto);
      lists.Protect("word0", WordSource.Auto, At(WordLists.Capacity));

      Assert.True(lists.Protect("zorbit", WordSource.Auto, At(WordLists.Capacity + 1)));

      Assert.True(lists.IsProtected("zorbit"));
      Assert.True(lists.IsProtected("word0"));
      Assert.False(lists.IsProtected("word1"));
      Assert.Equal(WordLists.Capacity, lists.Snapshot().Protected.Count);
    }

    [Fact]
    public void FullListNeverDropsUserEntries() {
      WordLists lists = FullOfProtected(WordSource.User);
      lists.Unprotect("word5");
      lists.Protect("blorp", WordSource.Auto, At(WordLists.Capacity + 5));

      Assert.True(lists.Protect("zorbit", WordSource.User, At(WordLists.Capacity + 6)));

      Assert.False(lists.IsProtected("blorp"));
      Assert.True(lists.IsProtected("word0"));
    }

    [Fact]
    public void FullListOfUserEntriesRefusesANewOne() {
      WordLists lists = FullOfProtected(WordSource.User);

      Assert.False(lists.Protect("zorbit", WordSource.User, At(WordLists.Capacity)));

      Assert.False(lists.IsProtected("zorbit"));
      Assert.Equal(WordLists.Capacity, lists.Snapshot().Protected.Count);
    }

    [Fact]
    public void AddingAnExistingWordRefreshesItEvenWhenFull() {
      WordLists lists = FullOfProtected(WordSource.Auto);

      Assert.True(lists.Protect("word0", WordSource.Auto, At(WordLists.Capacity)));
      lists.Protect("zorbit", WordSource.Auto, At(WordLists.Capacity + 1));

      Assert.True(lists.IsProtected("word0"));
      Assert.False(lists.IsProtected("word1"));
    }

    [Fact]
    public void UserAddingAnAutomaticEntryMakesItTheUsers() {
      var lists = new WordLists();
      lists.Protect("ntcn", WordSource.Auto, _start);

      lists.Protect("ntcn", WordSource.User, _start);
      lists.Protect("ntcn", WordSource.Auto, _start);

      Assert.Equal(WordSource.User, Assert.Single(lists.Snapshot().Protected).Source);
    }

    [Fact]
    public void LearnedListIsBoundedPerAllLanguagesTogether() {
      var lists = new WordLists();
      for (int i = 0; i < WordLists.Capacity; i++) {
        Language language = i % 2 == 0 ? Language.Russian : Language.English;
        lists.Learn($"word{i}", language, WordSource.Auto, At(i));
      }

      Assert.True(lists.Learn("кринж", Language.Russian, WordSource.Auto, At(WordLists.Capacity)));

      Assert.Contains("кринж", lists.LearnedIn(Language.Russian));
      Assert.DoesNotContain("word0", lists.LearnedIn(Language.Russian));
      Assert.Contains("word1", lists.LearnedIn(Language.English));
      Assert.Equal(WordLists.Capacity, lists.Snapshot().Learned.Count);
    }

    [Fact]
    public void LearnedWordBelongsToItsLanguageOnly() {
      var lists = new WordLists();

      lists.Learn("Кринж", Language.Russian, WordSource.User, _start);

      Assert.Contains("кринж", lists.LearnedIn(Language.Russian));
      Assert.Empty(lists.LearnedIn(Language.Ukrainian));
      Assert.True(lists.Unlearn("КРИНЖ", Language.Russian));
      Assert.Empty(lists.LearnedIn(Language.Russian));
    }

    [Fact]
    public void SnapshotRestoresTheSameLists() {
      var lists = new WordLists();
      lists.Protect("ntcn", WordSource.Auto, _start);
      lists.Learn("кринж", Language.Russian, WordSource.User, _start);

      var restored = new WordLists(lists.Snapshot());

      Assert.Equal(lists.Snapshot().Protected, restored.Snapshot().Protected);
      Assert.Equal(lists.Snapshot().Learned, restored.Snapshot().Learned);
      Assert.True(restored.IsProtected("ntcn"));
      Assert.Contains("кринж", restored.LearnedIn(Language.Russian));
    }

    private static DateTimeOffset At(int minutes) {
      return _start.AddMinutes(minutes);
    }

    private static WordLists FullOfProtected(WordSource source) {
      var lists = new WordLists();
      for (int i = 0; i < WordLists.Capacity; i++) {
        lists.Protect($"word{i}", source, At(i));
      }

      return lists;
    }
  }
}
