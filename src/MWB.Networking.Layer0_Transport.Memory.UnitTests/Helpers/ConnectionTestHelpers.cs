using MWB.Networking.Layer0_Transport.Encoding;
using MWB.Networking.Layer0_Transport.Lifecycle.Abstractions;
using MWB.Networking.Layer0_Transport.Lifecycle.Stack;
using MWB.Networking.Layer0_Transport.Memory.Buffer;

namespace MWB.Networking.Layer0_Transport.Memory.UnitTests.Helpers;

internal static class ConnectionTestHelpers
{
    // -------------------------------------------------------------------------
    // Connection factories
    // -------------------------------------------------------------------------

    /// <summary>
    /// Creates a unidirectional channel so that bytes written to the returned
    /// <c>writeEnd</c> can be read from the returned <c>readEnd</c>.
    /// </summary>
    /// <remarks>
    /// ConnectionA writes to the A→B buffer; ConnectionB reads from that same buffer.
    /// </remarks>
    internal static (InMemoryNetworkConnection writeEnd, InMemoryNetworkConnection readEnd)
        CreateUnidirectionalPair()
    {
        var buffer = new SegmentedDuplexBuffer();
        return (buffer.ConnectionA, buffer.ConnectionB);
    }

    /// <summary>
    /// Creates a cross-wired duplex pair.
    /// <c>connectionA.WriteAsync</c> is readable via <c>connectionB.ReadAsync</c>,
    /// and vice-versa.
    /// </summary>
    internal static (InMemoryNetworkConnection connectionA, InMemoryNetworkConnection connectionB)
        CreateDuplexInMemoryConnectionPair()
    {
        var buffer = new SegmentedDuplexBuffer();
        return (buffer.ConnectionA, buffer.ConnectionB);
    }

    /// <summary>
    /// Creates two providers that share an in-memory duplex buffer.
    /// </summary>
    internal static (InMemoryNetworkConnectionProvider providerA, InMemoryNetworkConnectionProvider providerB)
        CreateDuplexInMemoryProviders()
    {
        var buffer = new SegmentedDuplexBuffer();
        return (
            new InMemoryNetworkConnectionProvider(buffer, SegmentedDuplexBufferSide.SideA),
            new InMemoryNetworkConnectionProvider(buffer, SegmentedDuplexBufferSide.SideB)
        );
    }

    /// <summary>
    /// Opens both sides of a duplex provider pair, each with its own
    /// <see cref="ObservableConnectionStatus"/>.
    /// </summary>
    internal static async Task<(INetworkConnection connA, INetworkConnection connB)>
        OpenBothAsync(
            InMemoryNetworkConnectionProvider providerA,
            InMemoryNetworkConnectionProvider providerB,
            CancellationToken ct)
    {
        var connA = await providerA.OpenConnectionAsync(new ObservableConnectionStatus(), ct);
        var connB = await providerB.OpenConnectionAsync(new ObservableConnectionStatus(), ct);
        return (connA, connB);
    }

    // -------------------------------------------------------------------------
    // ByteSegments helpers
    // -------------------------------------------------------------------------

    /// <summary>Wraps a byte array in a single-segment <see cref="ByteSegments"/>.</summary>
    internal static ByteSegments Segment(params byte[] data) =>
        new((ReadOnlyMemory<byte>)data);

    // -------------------------------------------------------------------------
    // Read helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Reads until the connection signals EOF (ReadAsync returns 0)
    /// and returns all received bytes concatenated.
    /// </summary>
    internal static async Task<byte[]> ReadToEndAsync(
        INetworkConnection connection,
        CancellationToken ct)
    {
        var chunks = new List<byte[]>();
        var buffer = new byte[4096];

        int n;
        while ((n = await connection.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
        {
            var chunk = new byte[n];
            Array.Copy(buffer, chunk, n);
            chunks.Add(chunk);
        }

        return chunks.SelectMany(c => c).ToArray();
    }

    /// <summary>
    /// Reads exactly <paramref name="count"/> bytes from the connection,
    /// issuing multiple reads if a single read returns fewer bytes.
    /// Throws <see cref="EndOfStreamException"/> if the stream ends early.
    /// </summary>
    internal static async Task<byte[]> ReadExactAsync(
        INetworkConnection connection,
        int count,
        CancellationToken ct)
    {
        var result = new byte[count];
        var totalRead = 0;

        while (totalRead < count)
        {
            var n = await connection
                .ReadAsync(result.AsMemory(totalRead, count - totalRead), ct)
                .ConfigureAwait(false);

            if (n == 0)
                throw new EndOfStreamException(
                    $"Stream ended after {totalRead} byte(s); expected {count}.");

            totalRead += n;
        }

        return result;
    }
}
