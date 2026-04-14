using MWB.Networking.Layer0_Transport;
using MWB.Networking.Layer1_Framing.Internal;
using System.Buffers;
using System.Buffers.Binary;

namespace MWB.Networking.Layer1_Framing;

public sealed class NetworkFrameWriter : INetworkFrameWriter
{
    private static readonly ArrayPool<byte> HeaderPool = ArrayPool<byte>.Shared;

    public async Task WriteFrameAsync(
        INetworkConnection connection,
        NetworkFrame frame,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(frame);

        // ---- 1. Compute flags ---------------------------------------------

        var flags = NetworkFrameFlags.None;
        if (frame.EventType.HasValue)
        {
            flags |= NetworkFrameFlags.HasEventType;
        }
        if (frame.RequestId.HasValue)
        {
            flags |= NetworkFrameFlags.HasRequestId;
        }
        if (frame.StreamId.HasValue)
        {
            flags |= NetworkFrameFlags.HasStreamId;
        }

        // ---- 2. Compute frame header size ---------------------------------

        var headerLength = 2; // FrameKind + Flags

        if (frame.EventType.HasValue) headerLength += 4;
        if (frame.RequestId.HasValue) headerLength += 4;
        if (frame.StreamId.HasValue) headerLength += 4;

        // ---- 3. Rent header buffer ----------------------------------------

        var header = NetworkFrameWriter.HeaderPool.Rent(headerLength);

        try
        {
            var span = header.AsSpan(0, headerLength);
            var offset = 0;

            // ---- 4. Write header ------------------------------------------

            span[offset++] = (byte)frame.Kind;
            span[offset++] = (byte)flags;

            if (frame.EventType.HasValue)
            {
                BinaryPrimitives.WriteUInt32BigEndian(
                    span.Slice(offset, 4),
                    frame.EventType.Value);
                offset += 4;
            }

            if (frame.RequestId.HasValue)
            {
                BinaryPrimitives.WriteUInt32BigEndian(
                    span.Slice(offset, 4),
                    frame.RequestId.Value);
                offset += 4;
            }

            if (frame.StreamId.HasValue)
            {
                BinaryPrimitives.WriteUInt32BigEndian(
                    span.Slice(offset, 4),
                    frame.StreamId.Value);
                offset += 4;
            }

            // sanity check
            if (offset != headerLength)
            {
                throw new InvalidOperationException("Header length mismatch.");
            }

            // ---- 5. Send block (header + payload) -------------------------
            await connection.WriteBlockAsync(
                [header.AsMemory(0, headerLength), frame.Payload],
                ct);
        }
        catch (Exception ex)
        {
            throw;
        }
        finally
        {
            NetworkFrameWriter.HeaderPool.Return(header);
        }
    }
}
