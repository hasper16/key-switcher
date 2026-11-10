namespace Rekey.Win32.Keyboard {
  /// <summary>
  /// Shows whether the worker thread keeps up with input.
  /// </summary>
  internal interface IWorkerPulse {
    /// <summary>
    /// Gets the number of loops the worker finished; it moves at least every 250 ms while the
    /// worker runs.
    /// </summary>
    long Heartbeat { get; }

    /// <summary>
    /// Gets the number of hook inputs waiting for the worker.
    /// </summary>
    int QueuedInput { get; }
  }
}
