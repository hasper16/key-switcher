using Rekey.Core.Detection;
using Rekey.Core.Languages;

namespace Rekey.Core {
  /// <summary>
  /// Evaluates a Word that a Trigger Key ended.
  /// </summary>
  /// <param name="word">The Keystrokes of the Word.</param>
  /// <param name="typedIn">The Layout the Word was typed in.</param>
  /// <returns>The Decision for the Word.</returns>
  public delegate Decision WordEvaluator(IReadOnlyList<Keystroke> word, LayoutId typedIn);
}
