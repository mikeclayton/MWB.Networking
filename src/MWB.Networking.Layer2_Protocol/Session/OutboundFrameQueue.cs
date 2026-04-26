using MWB.Networking.Layer2_Protocol.Frames;

namespace MWB.Networking.Layer2_Protocol.Session;

/// <summary>
/// Manages the ordered delivery of outbound <see cref="ProtocolFrame"/> instances
/// from a protocol session to the outbound processing loop.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="OutboundFrameQueue"/> exists to solve a subtle but critical concurrency
/// problem in the protocol runtime: outbound frames may be produced on arbitrary
/// threads and may be enqueued both before and after the outbound processor starts,
/// while still requiring strict ordering and correct wake-up semantics.
/// </para>
/// <para>
/// The class encapsulates a lock-protected FIFO queue together with a semaphore
/// that represents <em>frame availability as a quantity</em>, not as a one-shot event.
/// Each successful enqueue releases exactly one semaphore permit, ensuring that
/// frames enqueued before the processor starts are not missed and that wake-ups
/// always correspond to real, dequeuable frames.
/// </para>
/// <para>
/// By hiding the underlying data structures and exposing only enqueue, wait, and
/// dequeue operations, this type enforces the correct interaction pattern and
/// prevents unsafe access patterns (such as inspecting queue state without proper
/// synchronization). This makes the outbound processing logic robust against
/// lost wake-ups, spurious wake-ups, and race conditions.
/// </para>
/// <para>
/// This type is deliberately <em>not</em> a general-purpose queue; it encodes
/// protocol-specific concurrency and lifecycle invariants and should be used as
/// the sole access point for outbound frame scheduling.
/// </para>
/// </remarks>
internal sealed class OutboundFrameQueue
{
    private readonly object _queueGate = new();
    private readonly Queue<ProtocolFrame> _queue = new();
    private readonly SemaphoreSlim _availableSignal = new(0);

    /// <summary>
    /// Enqueues a frame and signals availability.
    /// Must be called only after protocol validation has succeeded.
    /// </summary>
    public void Enqueue(ProtocolFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        lock (_queueGate)
        {
            _queue.Enqueue(frame);
        }

        // Signal *after* enqueue to ensure the frame is observable
        _availableSignal.Release();
    }

    /// <summary>
    /// Waits until at least one outbound frame is available.
    /// Guarantees that a subsequent TryDequeue will succeed
    /// unless another consumer races and dequeues first.
    /// </summary>
    public async Task WaitForFrameAsync(CancellationToken cancellationToken)
    {
        await _availableSignal
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Attempts to dequeue an outbound frame.
    /// Returns false if no frame is available.
    /// </summary>
    public bool TryDequeue(out ProtocolFrame frame)
    {
        lock (_queueGate)
        {
            if (_queue.Count == 0)
            {
                frame = default!;
                return false;
            }

            frame = _queue.Dequeue();
            return true;
        }
    }

    /// <summary>
    /// Clears all queued frames.
    /// Intended for session shutdown or fatal protocol termination.
    /// Does not attempt to rebalance the semaphore.
    /// </summary>
    public void Clear()
    {
        lock (_queueGate)
        {
            _queue.Clear();
        }
    }
}
