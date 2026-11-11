using Rekey.Core;
using Rekey.Core.Languages;

namespace Rekey.Win32.Layouts {
  /// <summary>
  /// Holds how a Layout Switch ended.
  /// </summary>
  /// <param name="Adopted">
  /// The Layout now active in the window, or <see langword="null" /> when it cannot be read.
  /// </param>
  /// <param name="Incident">The Incident of a switch that did not land; otherwise, none.</param>
  internal sealed record SwitchResult(LayoutId? Adopted, Incident? Incident);
}
