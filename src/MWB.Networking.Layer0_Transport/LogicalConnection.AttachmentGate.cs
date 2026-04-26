namespace MWB.Networking.Layer0_Transport;

public sealed partial class LogicalConnection
{
    /// <summary>
    /// A small lifecycle gate that allows callers to await the moment a backing
    /// network connection is attached.
    ///
    /// This exists to make connection attachment an explicit, atomic concept and
    /// to prevent accidental use of the connection before attachment has occurred.
    /// </summary>
    private sealed class AttachmentGate
    {
        private volatile TaskCompletionSource _attachedTcs =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>
        /// An awaitable task that completes when a backing connection is attached.
        /// Completion signals that an Attach operation has occurred, and does not
        /// imply current or future connectivity.
        /// </summary>
        public Task WhenAttachedAsync(CancellationToken ct) =>
            _attachedTcs.Task.WaitAsync(ct);

        /// <summary>
        /// Signals that an attachment has completed.
        /// Safe to call multiple times; only the first call completes the gate.
        /// </summary>
        public void SignalAttached()
        {
            _attachedTcs.TrySetResult();
        }

        /// <summary>
        /// Resets the gate, cancelling any current awaiters.
        /// Used to establish a new attachment epoch.
        /// </summary>
        public void Reset()
        {
            var oldAttachedTcs = Interlocked.Exchange(
                ref _attachedTcs,
                new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)
            );
            oldAttachedTcs.TrySetCanceled();
        }
    }
}
