using MWB.Networking.Layer0_Transport.Driver.Abstractions;
using MWB.Networking.Layer0_Transport.Encoding;
using MWB.Networking.Layer0_Transport.Stack;

namespace MWB.Networking.Layer3_Endpoint.Hosting;

/// <summary>
/// Adapts the async <see cref="TransportStack"/> to the synchronous
/// <see cref="ITransportStack"/> interface required by <see cref="TransportDriver"/>.
///
/// <see cref="Read"/> and <see cref="Write"/> block the calling thread using
/// <c>GetAwaiter().GetResult()</c>. This is safe because <see cref="TransportDriver"/>
/// drives these methods exclusively from a dedicated <see cref="Task.Run"/> thread-pool
/// thread — never from a synchronization context or the thread pool's shared queue.
/// </summary>
internal sealed class NetworkConnectionTransportAdapter : ITransportStack
{
    private readonly TransportStack _stack;

    internal NetworkConnectionTransportAdapter(TransportStack stack)
    {
        _stack = stack ?? throw new ArgumentNullException(nameof(stack));
    }

    // ------------------------------------------------------------------
    // ITransportByteSource
    // ------------------------------------------------------------------

    /// <summary>
    /// Blocks the calling thread until bytes are available, then copies
    /// them into <paramref name="buffer"/> and returns the byte count.
    /// Returns 0 on clean EOF.
    /// </summary>
    public int Read(Span<byte> buffer)
    {
        // Span<byte> cannot cross an async boundary, so we rent a matching
        // byte[] for the async call and copy the result back.
        var temp = new byte[buffer.Length];
        var bytesRead = _stack.ReadAsync(temp, CancellationToken.None)
            .GetAwaiter()
            .GetResult();
        temp.AsSpan(0, bytesRead).CopyTo(buffer);
        return bytesRead;
    }

    // ------------------------------------------------------------------
    // ITransportByteSink
    // ------------------------------------------------------------------

    /// <summary>
    /// Blocks the calling thread until the bytes have been written.
    /// </summary>
    public void Write(ReadOnlySpan<byte> bytes)
    {
        // ReadOnlySpan<byte> cannot cross an async boundary; copy to a byte[]
        // before handing off to the async write path.
        var copy = bytes.ToArray();
        _stack.WriteAsync(new ByteSegments(copy), CancellationToken.None)
            .GetAwaiter()
            .GetResult();
    }

    // ------------------------------------------------------------------
    // ITransportEvents
    // ------------------------------------------------------------------

    // TransportDriver detects clean EOF (Read returns 0) and faults
    // (Read throws) directly from the Read() return path, so these
    // events are never raised by this adapter. The stubs satisfy the
    // interface contract without adding dead wiring.

#pragma warning disable CS0067 // Event is never used
    public event Action? TransportClosed;
    public event Action<Exception>? TransportFaulted;
#pragma warning restore CS0067
}
