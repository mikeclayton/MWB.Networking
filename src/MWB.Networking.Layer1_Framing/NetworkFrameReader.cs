using MouseWithoutBorders.Networking.PeerTransport.Layer2_Protocol;
using MWB.Networking.Layer0_Transport;
using System.Buffers.Binary;

namespace MWB.Networking.Layer1_Framing;

public sealed class NetworkFrameReader : INetworkFrameReader
{
    public async Task<NetworkFrame> ReadFrameAsync(
        INetworkConnection connection,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(connection);

        // TcpNetworkConnection.ReceiveAsync() already:
        // - read the 4-byte length prefix
        // - validated max size
        // - returned exactly one frame buffer
        var buffer = await connection.ReadBlockAsync(ct);

        var span = (ReadOnlySpan<byte>)buffer;
        var offset = 0;

        // 1) Read fixed header
        var kind = (NetworkFrameKind)span[offset++];
        var flags = (NetworkFrameFlags)span[offset++];

        var eventType = (uint?)null;
        var requestId = (uint?)null;
        var streamId = (uint?)null;
        var chunkIndex = (uint?)null;
        var isFinalChunk = flags.HasFlag(NetworkFrameFlags.IsFinalChunk);

        // 2) Read optional fields
        if (flags.HasFlag(NetworkFrameFlags.HasEventType))
        {
            eventType = BinaryPrimitives.ReadUInt32BigEndian(
                span.Slice(offset, 4));
            offset += 4;
        }

        if (flags.HasFlag(NetworkFrameFlags.HasRequestId))
        {
            requestId = BinaryPrimitives.ReadUInt32BigEndian(
                span.Slice(offset, 4));
            offset += 4;
        }

        if (flags.HasFlag(NetworkFrameFlags.HasStreamId))
        {
            streamId = BinaryPrimitives.ReadUInt32BigEndian(
                span.Slice(offset, 4));
            offset += 4;
        }

        if (flags.HasFlag(NetworkFrameFlags.HasChunkIndex))
        {
            chunkIndex = BinaryPrimitives.ReadUInt32BigEndian(
                span.Slice(offset, 4));
            offset += 4;
        }

        if (flags.HasFlag(NetworkFrameFlags.IsFinalChunk))
        {
            // isFinalChunk is encoded as a flag, so we don't need to read any bytes
            isFinalChunk = true;
        }

        // 3) Remaining bytes are payload (zero-copy slice)
        var payload = (offset < buffer.Length)
            ? buffer.AsMemory(offset)
            : ReadOnlyMemory<byte>.Empty;

        return new NetworkFrame(
            kind: kind,
            eventType: eventType,
            requestId: requestId,
            streamId: streamId,
            chunkIndex: chunkIndex,
            isFinalChunk: isFinalChunk,
            payload: payload
        );
    }
}
