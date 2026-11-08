namespace Rekey.Core.Languages {
  /// <summary>
  /// Reports that a Language Profile file cannot be read or fails validation.
  /// </summary>
  public sealed class ProfileLoadException : Exception {
    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileLoadException" /> class.
    /// </summary>
    public ProfileLoadException() {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileLoadException" /> class with a message.
    /// </summary>
    /// <param name="message">The message that describes the problem.</param>
    public ProfileLoadException(string message)
        : base(message) {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileLoadException" /> class with a message
    /// and the exception that caused it.
    /// </summary>
    /// <param name="message">The message that describes the problem.</param>
    /// <param name="innerException">The exception that caused the problem.</param>
    public ProfileLoadException(string message, Exception innerException)
        : base(message, innerException) {
    }
  }
}
