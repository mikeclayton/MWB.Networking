using MWB.Networking.Layer1_Framing.Codec;
using MWB.Networking.Layer1_Framing.Codec.Abstractions;
using MWB.Networking.Layer1_Framing.Codec.Buffer;
using MWB.Networking.Layer1_Framing.Codec.Frames;
using System.Buffers.Binary;

namespace MWB.Networking.Layer1_Framing.Codecs.Default.Network;

public sealed partial class DefaultNetworkFrameCodec : INetworkFrameCodec
{
    public void Encode(NetworkFrame frame, ICodecBufferWriter writer)
    {
        // ---- 1. Compute flags ---------------------------------------------

        var flags = NetworkFrameFlags.None;

        if (frame.EventType.HasValue) flags |= NetworkFrameFlags.HasEventType;
        if (frame.RequestId.HasValue) flags |= NetworkFrameFlags.HasRequestId;
        if (frame.RequestType.HasValue) flags |= NetworkFrameFlags.HasRequestType;
        if (frame.ResponseType.HasValue) flags |= NetworkFrameFlags.HasResponseType;
        if (frame.StreamId.HasValue) flags |= NetworkFrameFlags.HasStreamId;
        if (frame.StreamType.HasValue) flags |= NetworkFrameFlags.HasStreamType;

        // ---- 2. Compute frame header size ---------------------------------

        var headerLength = 2; // FrameKind + Flags

        if (frame.EventType.HasValue) headerLength += 4;
        if (frame.RequestId.HasValue) headerLength += 4;
        if (frame.RequestType.HasValue) headerLength += 4;
        if (frame.ResponseType.HasValue) headerLength += 4;
        if (frame.StreamId.HasValue) headerLength += 4;
        if (frame.StreamType.HasValue) headerLength += 4;

        // ---- 3. Write header ------------------------------------------

        Span<byte> header = stackalloc byte[headerLength];
        var offset = 0;

        header[offset++] = (byte)frame.Kind;
        header[offset++] = (byte)flags;

        if (frame.EventType.HasValue)
        {
            BinaryPrimitives.WriteUInt32BigEndian(
                header.Slice(offset, 4), frame.EventType.Value);
            offset += 4;
        }

        if (frame.RequestId.HasValue)
        {
            BinaryPrimitives.WriteUInt32BigEndian(
                header.Slice(offset, 4), frame.RequestId.Value);
            offset += 4;
        }

        if (frame.RequestType.HasValue)
        {
            BinaryPrimitives.WriteUInt32BigEndian(
                header.Slice(offset, 4), frame.RequestType.Value);
            offset += 4;
        }

        if (frame.ResponseType.HasValue)
        {
            BinaryPrimitives.WriteUInt32BigEndian(
                header.Slice(offset, 4), frame.ResponseType.Value);
            offset += 4;
        }

        if (frame.StreamId.HasValue)
        {
            BinaryPrimitives.WriteUInt32BigEndian(
                header.Slice(offset, 4), frame.StreamId.Value);
            offset += 4;
        }

        if (frame.StreamType.HasValue)
        {
            BinaryPrimitives.WriteUInt32BigEndian(
                header.Slice(offset, 4), frame.StreamType.Value);
            offset += 4;
        }

        // build segments (header + payload)

        writer.Write(header);
        if (!frame.Payload.IsEmpty)
        {
            writer.Write(frame.Payload);
        }
    }

    /// <summary>
    /// Deserializes a single NetworkFrame from its serialized byte representation.
    /// </summary>
    /// <remarks>
    /// The input is assumed to represent exactly one complete logical NetworkFrame.
    /// No framing, buffering, or transport concerns are handled here.
    /// </remarks>
    public FrameDecodeResult Decode(
        ICodecBufferReader inputReader,
        out NetworkFrame outputFrame)
    {
        outputFrame = default!;

        // ---- Need at least Kind + Flags -----------------------------------

        if (!inputReader.TryRead(out var header) || header.Length < 2)
        {
            return FrameDecodeResult.InvalidFrameEncoding;
        }

        var kind = (NetworkFrameKind)header.Span[0];
        var flags = (NetworkFrameFlags)header.Span[1];

        inputReader.Advance(2);

        // ---- Optional fields -----------------------------------

        uint? eventType = null;
        uint? requestId = null;
        uint? requestType = null;
        uint? responseType = null;
        uint? streamId = null;
        uint? streamType = null;

        // local helper function
        bool TryReadUInt32(out uint? value)
        {
            if (!inputReader.TryRead(out var mem) || mem.Length < 4)
            {
                value = default;
                return false;
            }   

            value = BinaryPrimitives.ReadUInt32BigEndian(mem.Span);
            inputReader.Advance(4);
            return true;
        }

        if (((flags & NetworkFrameFlags.HasEventType) != 0) && !TryReadUInt32(out eventType))
        {
            return FrameDecodeResult.InvalidFrameEncoding;
        }

        if (((flags & NetworkFrameFlags.HasRequestId) != 0) && !TryReadUInt32(out requestId))
        {
            return FrameDecodeResult.InvalidFrameEncoding;
        }

        if (((flags & NetworkFrameFlags.HasRequestType) != 0) && !TryReadUInt32(out requestType))
        {
            return FrameDecodeResult.InvalidFrameEncoding;
        }

        if (((flags & NetworkFrameFlags.HasResponseType) != 0) && !TryReadUInt32(out responseType))
        {
            return FrameDecodeResult.InvalidFrameEncoding;
        }

        if (((flags & NetworkFrameFlags.HasStreamId) != 0) && !TryReadUInt32(out streamId))
        {
            return FrameDecodeResult.InvalidFrameEncoding;
        }

        if (((flags & NetworkFrameFlags.HasStreamType) != 0) && !TryReadUInt32(out streamType))
        {
            return FrameDecodeResult.InvalidFrameEncoding;
        }

        // ---- Remaining bytes are payload ----------------------------------

        var remaining = inputReader.Length;
        if (remaining < 0 || remaining > int.MaxValue)
        {
            return FrameDecodeResult.InvalidFrameEncoding;
        }

        ReadOnlyMemory<byte> payload;
        if (remaining == 0)
        {
            payload = ReadOnlyMemory<byte>.Empty;
        }
        else
        {
            // Invariant / Limitation:
            // Transport decoding must provide the entire frame payload as a single
            // contiguous buffer segment. DefaultNetworkFrameCodec does not support
            // multi-segment payloads.
            if (!inputReader.TryRead(out var mem) || (mem.Length != remaining))
            {
                return FrameDecodeResult.InvalidFrameEncoding;
            }

            payload = mem[..(int)remaining].ToArray();
            inputReader.Advance((int)remaining);
        }

        // ---- Construct the return value ----------------------------------

        outputFrame = NetworkFrame.CreateRaw(
            kind,
            eventType,
            requestId,
            requestType,
            responseType,
            streamId,
            streamType,
            payload);

        return FrameDecodeResult.Success;
    }
}
