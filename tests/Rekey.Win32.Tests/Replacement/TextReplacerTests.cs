using Rekey.Core;
using Rekey.Win32.Replacement;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Xunit;
using static Rekey.Win32.Tests.Replacement.VerifiedReplacementTests;

namespace Rekey.Win32.Tests.Replacement {
  // Design D3: how a ReplaceText reaches the application.
  public sealed class TextReplacerTests {
    private const string _editor = @"C:\Apps\Editor.exe";
    private readonly List<INPUT[]> _sent = [];

    [Fact]
    public void VerifiedMatchTypesOnlyTheReplacement() {
      TextReplacer replacer = Replacer(new FakeControl("ghbdtn"), verify: true);

      ReplacementResult result = replacer.Replace(new ReplaceText("ghbdtn", "привет"), _editor);

      Assert.Equal(ReplacementResult.Verified, result);
      Assert.Equal(12, Assert.Single(_sent).Length);
    }

    [Fact]
    public void VerifiedReplacementOffRetypesWithoutReading() {
      var control = new FakeControl("ghbdtn");
      TextReplacer replacer = Replacer(control, verify: false);

      ReplacementResult result = replacer.Replace(new ReplaceText("ghbdtn", "привет"), _editor);

      Assert.Equal(ReplacementResult.Retyped, result);
      Assert.Equal(0, control.AskedLength);
      Assert.Equal(24, Assert.Single(_sent).Length);
    }

    [Fact]
    public void MismatchSendsNothing() {
      TextReplacer replacer = Replacer(new FakeControl("dtnxyz"), verify: true);

      ReplacementResult result = replacer.Replace(new ReplaceText("ghbdtn", "привет"), _editor);

      Assert.Equal(ReplacementResult.Mismatch, result);
      Assert.Empty(_sent);
    }

    [Fact]
    public void AppThatRanOutOfTimeTwiceIsRetypedWithoutReading() {
      var control = new FakeControl(null) { TimedOut = true };
      TextReplacer replacer = Replacer(control, verify: true);
      replacer.Replace(new ReplaceText("ghbdtn", "привет"), _editor);
      replacer.Replace(new ReplaceText("ghbdtn", "привет"), _editor);
      control.AskedLength = 0;

      replacer.Replace(new ReplaceText("ghbdtn", "привет"), _editor);

      Assert.Equal(0, control.AskedLength);
    }

    private TextReplacer Replacer(ITextBeforeCaret control, bool verify) {
      return new TextReplacer(control, verify, () => [], _sent.Add);
    }
  }
}
