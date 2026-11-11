namespace Rekey.DesktopChecks {
  /// <summary>
  /// Reports a restore the desktop checks could not finish, with steps to recover by hand.
  /// </summary>
  /// <remarks>
  /// The message is written by the tool itself, so it is the only exception message the report
  /// shows; other exceptions show only their type.
  /// </remarks>
  public sealed class RecoveryException : Exception {
    /// <summary>
    /// Initializes a new instance of the <see cref="RecoveryException" /> class.
    /// </summary>
    public RecoveryException() {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RecoveryException" /> class.
    /// </summary>
    /// <param name="message">The steps to recover by hand.</param>
    public RecoveryException(string message)
        : base(message) {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RecoveryException" /> class.
    /// </summary>
    /// <param name="message">The steps to recover by hand.</param>
    /// <param name="innerException">The failure that stopped the restore.</param>
    public RecoveryException(string message, Exception innerException)
        : base(message, innerException) {
    }
  }
}
