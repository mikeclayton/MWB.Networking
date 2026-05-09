using MWB.Networking.Layer0_Transport.Stack.Core.Abstractions;
using System.Collections.Concurrent;

namespace MWB.Networking.Layer1_Framing.Driver.UnitTests.Helpers;

/// <summary>
/// A concrete, in-process implementation of <see cref="ITransportStack"/> for use in
/// unit tests. Byte reads are served from an in-memory queue; writes are captured for
/// later inspection. Both EOF and fault conditions can be injected.
///
/// No mocking framework is used — this is a real implementation of the interface.
/// </summary>
internal sealed class FakeTransportStack : ITransportStack
{
    // Each queued item is one of:
    //   byte[]    – data to copy into the caller's Read buffer
    //   null      – signals a clean EOF (Read returns 0)
    //   Exception – thrown from Read to simulate a transport fault
    private readonly BlockingCollection<object?> _readQueue = new();

    private readonly List<byte[]> _writtenSegments = new();

    // ------------------------------------------------------------------
    // ITransportEvents
    // ------------------------------------------------------------------

    public event Action? TransportClosed;
    public event Action<Exception>? TransportFaulted;

    // ------------------------------------------------------------------
    // ITransportByteSource
    // ------------------------------------------------------------------

    /// <summary>
    /// Blocks until a queued item is available, then returns data, 0 (EOF),
    /// or throws, depending on the item type.
    /// </summary>
    public int Read(Span<byte> buffer)
    {
        var item = _readQueue.Take();

        return item switch
        {
            null => 0,
            Exception ex => throw ex,
            byte[] data => CopyData(data, buffer),
            _ => throw new InvalidOperationException($"Unexpected queue item type: {item.GetType().Name}")
        };
    }

    private static int CopyData(byte[] data, Span<byte> buffer)
    {
        data.CopyTo(buffer);
        return data.Length;
    }

    // ------------------------------------------------------------------
    // ITransportByteSink
    // ------------------------------------------------------------------

    public void Write(ReadOnlySpan<byte> bytes) =>
        _writtenSegments.Add(bytes.ToArray());

    // ------------------------------------------------------------------
    // Inspection
    // ------------------------------------------------------------------

    /// <summary>
    /// All byte arrays passed to <see cref="Write"/>, in call order.
    /// Each element corresponds to one <see cref="Write"/> call.
    /// </summary>
    public IReadOnlyList<byte[]> WrittenSegments => _writtenSegments.AsReadOnly();

    /// <summary>
    /// Concatenates all <see cref="WrittenSegments"/> into a single byte array.
    /// </summary>
    public byte[] AllWrittenBytes()
    {
        var total = _writtenSegments.Sum(s => s.Length);
        var result = new byte[total];
        var offset = 0;
        foreach (var seg in _writtenSegments)
        {
            seg.CopyTo(result, offset);
            offset += seg.Length;
        }
        return result;
    }

    // ------------------------------------------------------------------
    // Injection helpers
    // ------------------------------------------------------------------

    /// <summary>Queues a chunk of data to be returned by the next <see cref="Read"/>.</summary>
    public void EnqueueBytes(byte[] data) => _readQueue.Add(data);

    /// <summary>Queues a clean EOF — <see cref="Read"/> will return 0.</summary>
    public void EnqueueEof() => _readQueue.Add(null);

    /// <summary>Queues a fault — <see cref="Read"/> will throw <paramref name="ex"/>.</summary>
    public void EnqueueException(Exception ex) => _readQueue.Add(ex);

    // ------------------------------------------------------------------
    // Event-raising helpers
    // ------------------------------------------------------------------

    /// <summary>Raises <see cref="TransportClosed"/> directly, as if the transport signalled it.</summary>
    public void RaiseTransportClosed() => TransportClosed?.Invoke();

    /// <summary>Raises <see cref="TransportFaulted"/> directly, as if the transport signalled it.</summary>
    public void RaiseTransportFaulted(Exception ex) => TransportFaulted?.Invoke(ex);
}
