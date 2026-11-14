using Rekey.Core.Detection;
using Rekey.Core.Languages;
using Rekey.Core.Tests.Detection;
using Rekey.Core.Words;
using Xunit;

namespace Rekey.Core.Tests {
  // Spec word-learning, engine side, with the bundled Language Profiles; the Words are the spec's
  // own examples.
  public sealed class WordLearningTests {
    private static readonly DateTimeOffset _monday = new(2026, 11, 16, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void UnprotectedWordIsCorrected() {
      Typist typist = With(new WordLists()).Type("ntcn").Hold(KeyKind.Space);

      Assert.Equal([new ReplaceText("ntcn", "тест")], typist.Actions);
    }

    [Fact]
    public void ProtectedWordIsKept() {
      Typist typist = With(Protecting("ntcn")).Type("ntcn").Hold(KeyKind.Space);

      // The Protected Word is marked used, so the list changed.
      Assert.Equal([new WordListsChanged()], typist.Actions);
    }

    [Fact]
    public void ProtectedWordIsMatchedIgnoringCase() {
      Typist typist = With(Protecting("NTCN")).Type("Ntcn").Hold(KeyKind.Space);

      Assert.Equal([new WordListsChanged()], typist.Actions);
    }

    [Fact]
    public void UnprotectedRunIsCorrected() {
      Typist typist = With(new WordLists())
          .Type("yt").Hold(KeyKind.Space).Type("ntcn").Hold(KeyKind.Space);

      Assert.Equal([new ReplaceText("yt ntcn", "не тест")], typist.Actions);
    }

    [Fact]
    public void RunWithAProtectedWordIsKept() {
      Typist typist = With(Protecting("ntcn"))
          .Type("yt").Hold(KeyKind.Space).Type("ntcn").Hold(KeyKind.Space);

      Assert.DoesNotContain(typist.Actions, action => action is ReplaceText);
    }

    [Fact]
    public void HotkeyCorrectsAProtectedWord() {
      Typist typist = With(Protecting("ntcn")).Type("ntcn").Hold(KeyKind.Space).Hotkey();

      Assert.Equal([new WordListsChanged(), new ReplaceText("ntcn ", "тест ")], typist.Actions);
    }

    [Fact]
    public void UnknownSlangWordIsNotCorrected() {
      Typist typist = With(new WordLists()).Type("rhby;").Hold(KeyKind.Space);

      Assert.Empty(typist.Actions);
    }

    [Fact]
    public void LearnedWordCountsAsACommonWord() {
      var lists = new WordLists();
      lists.Learn("кринж", Language.Russian, WordSource.User, _monday);

      Typist typist = With(lists).Type("rhby;").Hold(KeyKind.Space);

      Assert.Equal([new ReplaceText("rhby;", "кринж")], typist.Actions);
    }

    [Fact]
    public void LearnedWordOfTheTypedLanguageIsNotCorrected() {
      // Without it, "ghbdtn" becomes "привет" (spec auto-correction).
      var lists = new WordLists();
      lists.Learn("ghbdtn", Language.English, WordSource.User, _monday);

      Typist typist = With(lists).Type("ghbdtn").Hold(KeyKind.Space);

      Assert.Empty(typist.Actions);
    }

    [Fact]
    public void RemovingProtectionTakesEffectForTheNextWord() {
      Typist typist = With(Protecting("ntcn"))
          .Send(WordListEdited.Unprotect(TimeSpan.Zero, "ntcn"))
          .Type("ntcn").Hold(KeyKind.Space);

      Assert.Equal([new WordListsChanged(), new ReplaceText("ntcn", "тест")], typist.Actions);
    }

    [Fact]
    public void AddingProtectionTakesEffectForTheNextWord() {
      Typist typist = With(new WordLists())
          .Send(WordListEdited.Protect(TimeSpan.Zero, "ntcn"))
          .Type("ntcn").Hold(KeyKind.Space);

      Assert.Equal([new WordListsChanged(), new WordListsChanged()], typist.Actions);
      Assert.Equal(WordSource.User, Assert.Single(typist.Engine.Words.Snapshot().Protected).Source);
    }

    [Fact]
    public void LearningAndUnlearningTakeEffectForTheNextWord() {
      Typist typist = With(new WordLists())
          .Send(WordListEdited.Learn(TimeSpan.Zero, "кринж", Language.Russian))
          .Type("rhby;").Hold(KeyKind.Space).Press(KeyKind.Enter)
          .Send(WordListEdited.Unlearn(TimeSpan.Zero, "кринж", Language.Russian))
          .Type("rhby;").Hold(KeyKind.Space);

      Assert.Equal(
          [new WordListsChanged(), new ReplaceText("rhby;", "кринж"), new WordListsChanged()],
          typist.Actions);
    }

    [Fact]
    public void RemovingAnAbsentWordChangesNothing() {
      Typist typist = With(new WordLists()).Send(WordListEdited.Unprotect(TimeSpan.Zero, "ntcn"));

      Assert.Empty(typist.Actions);
    }

    [Fact]
    public void AddingToAListFullOfUserWordsIsRefused() {
      var lists = new WordLists();
      for (int i = 0; i < WordLists.Capacity; i++) {
        lists.Protect($"word{i}", WordSource.User, _monday);
      }

      Typist typist = With(lists).Send(WordListEdited.Protect(TimeSpan.Zero, "ntcn"));

      Assert.Equal([new WordListFull()], typist.Actions);
    }

    [Fact]
    public void UndoProtectsTheWord() {
      Typist typist = With(new WordLists())
          .Type("ntcn").Hold(KeyKind.Space).Finish().RequestUndo().Finish();

      Assert.Equal(
          [
            new ReplaceText("ntcn", "тест"),
            new SwitchLayout(KeyMapFixtures.Russian.Layout),
            new ReplaceText("тест ", "ntcn "),
            new SwitchLayout(KeyMapFixtures.EnglishUs.Layout),
            new UndoPerformed("ntcn", "тест"),
            new WordListsChanged(),
          ],
          typist.Actions);
      ProtectedWord word = Assert.Single(typist.Engine.Words.Snapshot().Protected);
      Assert.Equal(("ntcn", WordSource.Auto), (word.Text, word.Source));
    }

    [Fact]
    public void WordProtectedByUndoIsKeptNextTime() {
      Typist typist = With(new WordLists())
          .Type("ntcn").Hold(KeyKind.Space).Finish().RequestUndo().Finish();
      typist.Actions.Clear();

      typist.Type("ntcn").Hold(KeyKind.Space);

      Assert.DoesNotContain(typist.Actions, action => action is ReplaceText);
    }

    [Fact]
    public void UndoOfAShortWordDoesNotProtectIt() {
      var aggressive = new Settings(Sensitivity: Sensitivity.Aggressive);
      Typist typist = new Typist(aggressive, new WordLists())
          .Type("yt").Hold(KeyKind.Space).Finish().RequestUndo().Finish();

      Assert.Contains(new UndoPerformed("yt", "не"), typist.Actions);
      Assert.DoesNotContain(new WordListsChanged(), typist.Actions);
      Assert.Empty(typist.Engine.Words.Snapshot().Protected);
    }

    [Fact]
    public void UndoCountsLettersNotDigitsForTheLengthRule() {
      Typist typist = new Typist(new Settings(AutoCorrectionEnabled: false), new WordLists())
          .Type("yt1").Hotkey().Finish().RequestUndo().Finish();

      Assert.Contains(typist.Actions, action => action is UndoPerformed);
      Assert.Empty(typist.Engine.Words.Snapshot().Protected);
    }

    [Fact]
    public void UndoForgetsALearnedWordTypedWithADigit() {
      var lists = new WordLists();
      lists.Learn("кринж", Language.Russian, WordSource.User, _monday);

      Typist typist = new Typist(new Settings(AutoCorrectionEnabled: false), lists)
          .Type("rhby;1").Hotkey().Finish().RequestUndo().Finish();

      Assert.Contains(new UndoPerformed("rhby;1", "кринж1"), typist.Actions);
      Assert.Empty(typist.Engine.Words.LearnedIn(Language.Russian));
    }

    [Fact]
    public void UndoOfARunProtectsItsLongWordsOnly() {
      Typist typist = With(new WordLists())
          .Type("yt").Hold(KeyKind.Space).Type("ntcn").Hold(KeyKind.Space)
          .Finish().RequestUndo().Finish();

      Assert.Equal(["ntcn"], typist.Engine.Words.Snapshot().Protected.Select(word => word.Text));
    }

    [Fact]
    public void ManualCorrectionToAnUnknownWordLearnsIt() {
      Typist typist = With(new WordLists()).Type("rhby;").Hotkey().Finish();

      Assert.Equal(
          [
            new ReplaceText("rhby;", "кринж"),
            new SwitchLayout(KeyMapFixtures.Russian.Layout),
            new WordListsChanged(),
          ],
          typist.Actions);
      LearnedWord word = Assert.Single(typist.Engine.Words.Snapshot().Learned);
      Assert.Equal(
          ("кринж", Language.Russian, WordSource.Auto), (word.Text, word.Language, word.Source));
    }

    [Fact]
    public void LearnedWordIsCorrectedAutomaticallyNextTime() {
      Typist typist = With(new WordLists()).Type("rhby;").Hotkey().Finish();
      typist.Actions.Clear();

      typist.Type("rhby;").Hold(KeyKind.Space);

      Assert.Equal([new ReplaceText("rhby;", "кринж")], typist.Actions);
    }

    [Fact]
    public void ManualCorrectionToAKnownWordLearnsNothing() {
      Typist typist = With(new WordLists()).Type("ghbdtn").Hotkey().Finish();

      Assert.DoesNotContain(new WordListsChanged(), typist.Actions);
      Assert.Empty(typist.Engine.Words.Snapshot().Learned);
    }

    [Fact]
    public void ConvertedSelectionLearnsNothing() {
      Typist typist = With(new WordLists()).Hotkey("rhby;").Finish().RequestUndo().Finish();

      Assert.Contains(new UndoPerformed("rhby;", "кринж"), typist.Actions);
      Assert.DoesNotContain(new WordListsChanged(), typist.Actions);
    }

    [Fact]
    public void UndoOfACorrectionToALearnedWordForgetsIt() {
      var lists = new WordLists();
      lists.Learn("кринж", Language.Russian, WordSource.User, _monday);

      Typist typist = With(lists)
          .Type("rhby;").Hold(KeyKind.Space).Finish().RequestUndo().Finish();

      Assert.Empty(typist.Engine.Words.LearnedIn(Language.Russian));
      Assert.Contains(new WordListsChanged(), typist.Actions);
    }

    private static WordLists Protecting(string word) {
      var lists = new WordLists();
      lists.Protect(word, WordSource.User, _monday);
      return lists;
    }

    private static Typist With(WordLists lists) {
      return new Typist(new Settings(), lists);
    }
  }
}
