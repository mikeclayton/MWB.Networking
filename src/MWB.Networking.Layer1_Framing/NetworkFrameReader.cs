using MWB.Networking.Layer0_Transport;
using MWB.Networking.Layer1_Framing.Internal;
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

        // 3) Remaining bytes are payload (zero-copy slice)
        var payload = (offset < buffer.Length)
            ? buffer.AsMemory(offset)
            : ReadOnlyMemory<byte>.Empty;

        return new NetworkFrame(
            kind: kind,
            eventType: eventType,
            requestId: requestId,
            streamId: streamId,
            payload: payload
        );
    }
}
