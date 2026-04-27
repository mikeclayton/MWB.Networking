using MWB.Networking.Layer0_Transport.Encoding;
using MWB.Networking.Layer1_Framing.Frames;
using MWB.Networking.Layer1_Framing.Internal;
using System.Buffers.Binary;

namespace MWB.Networking.Layer1_Framing.Serialization;

internal static class NetworkFrameSerializer
{
    public static ByteSegments SerializeFrame(NetworkFrame frame)
    {
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
        if (frame.RequestType.HasValue)
        {
            flags |= NetworkFrameFlags.HasRequestType;
        }
        if (frame.ResponseType.HasValue)
        {
            flags |= NetworkFrameFlags.HasResponseType;
        }
        if (frame.StreamId.HasValue)
        {
            flags |= NetworkFrameFlags.HasStreamId;
        }

        // ---- 2. Compute frame header size ---------------------------------

        var headerLength = 2; // FrameKind + Flags

        if (frame.EventType.HasValue) headerLength += 4;
        if (frame.RequestId.HasValue) headerLength += 4;
        if (frame.RequestType.HasValue) headerLength += 4;
        if (frame.ResponseType.HasValue) headerLength += 4;
        if (frame.StreamId.HasValue) headerLength += 4;

        // ---- 4. Write header ------------------------------------------

        var header = new byte[headerLength];
        var span = header.AsSpan(0, headerLength);
        var offset = 0;

        span[offset++] = (byte)frame.Kind;
        span[offset++] = (byte)flags;

        if (frame.EventType.HasValue)
        {
            BinaryPrimitives.WriteUInt32BigEndian(
                span.Slice(offset, 4), frame.EventType.Value);
            offset += 4;
        }

        if (frame.RequestId.HasValue)
        {
            BinaryPrimitives.WriteUInt32BigEndian(
                span.Slice(offset, 4), frame.RequestId.Value);
            offset += 4;
        }

        if (frame.RequestType.HasValue)
        {
            BinaryPrimitives.WriteUInt32BigEndian(
                span.Slice(offset, 4), frame.RequestType.Value);
            offset += 4;
        }

        if (frame.ResponseType.HasValue)
        {
            BinaryPrimitives.WriteUInt32BigEndian(
                span.Slice(offset, 4), frame.ResponseType.Value);
            offset += 4;
        }

        if (frame.StreamId.HasValue)
        {
            BinaryPrimitives.WriteUInt32BigEndian(
                span.Slice(offset, 4), frame.StreamId.Value);
            offset += 4;
        }

        // build segments (header + payload)
        return frame.Payload.IsEmpty
            ? new ByteSegments(header.AsMemory(0, headerLength))
            : new ByteSegments(
                header.AsMemory(0, headerLength),
                frame.Payload);
    }

    /// <summary>
    /// Deserializes a single NetworkFrame from its serialized byte representation.
    /// </summary>
    /// <remarks>
    /// The input is assumed to represent exactly one complete logical NetworkFrame.
    /// No framing, buffering, or transport concerns are handled here.
    /// </remarks>
    public static NetworkFrame DeserializeFrame(ByteSegments frame)
    {
        // For now, assume a single contiguous segment.
        // (Can be extended later if needed.)
        if (frame.Segments.Length != 1)
            throw new InvalidOperationException(
                "NetworkFrame deserialization expects a single contiguous segment.");

        var memory = frame.Segments[0];
        var span = memory.Span;
        var offset = 0;

        // ---- 1. Read fixed header -----------------------------------------

        var kind = (NetworkFrameKind)span[offset++];
        var flags = (NetworkFrameFlags)span[offset++];

        uint? eventType = null;
        uint? requestId = null;
        uint? requestType = null;
        uint? responseType = null;
        uint? streamId = null;
        uint? streamType = null;

        // ---- 2. Read optional fields --------------------------------------

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

        if (flags.HasFlag(NetworkFrameFlags.HasRequestType))
        {
            requestType = BinaryPrimitives.ReadUInt32BigEndian(
                span.Slice(offset, 4));
            offset += 4;
        }

        if (flags.HasFlag(NetworkFrameFlags.HasResponseType))
        {
            responseType = BinaryPrimitives.ReadUInt32BigEndian(
                span.Slice(offset, 4));
            offset += 4;
        }

        if (flags.HasFlag(NetworkFrameFlags.HasStreamId))
        {
            streamId = BinaryPrimitives.ReadUInt32BigEndian(
                span.Slice(offset, 4));
            offset += 4;
        }

        if (flags.HasFlag(NetworkFrameFlags.HasStreamType))
        {
            streamType = BinaryPrimitives.ReadUInt32BigEndian(
                span.Slice(offset, 4));
            offset += 4;
        }

        // ---- 3. Remaining bytes are payload --------------------------------

        var payload = (offset < span.Length)
            ? memory.Slice(offset)
            : ReadOnlyMemory<byte>.Empty;

        return new NetworkFrame(
            kind, eventType, requestId, requestType, responseType, streamId, streamType, payload);
    }
}
