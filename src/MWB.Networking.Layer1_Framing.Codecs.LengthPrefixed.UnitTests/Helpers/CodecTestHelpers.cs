using MWB.Networking.Layer1_Framing.Codec.Buffer;
using System.Buffers;
using System.Buffers.Binary;

namespace MWB.Networking.Layer1_Framing.Codecs.LengthPrefixed.UnitTests.Helpers;

/// <summary>
/// Shared utility methods for length-prefixed codec unit tests.
/// </summary>
internal static class CodecTestHelpers
{
    // -------------------------------------------------------------------------
    // CodecBuffer factory helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Creates a fully-written <see cref="CodecBuffer"/> whose segments are
    /// each of the supplied arrays written in order.
    /// </summary>
    internal static CodecBuffer CreateInputBuffer(params byte[][] segments)
    {
        var buffer = new CodecBuffer();
        foreach (var segment in segments)
        {
            if (segment.Length > 0)
                buffer.Writer.Write(segment);
        }
        buffer.Writer.Complete();
        return buffer;
    }

    /// <summary>
    /// Drains all available segments from a <see cref="CodecBuffer"/>'s reader
    /// and returns them concatenated as a single byte array.
    /// </summary>
    internal static byte[] ReadAllOutput(CodecBuffer buffer)
    {
        var chunks = new List<byte[]>();

        while (buffer.Reader.TryRead(out var memory))
        {
            chunks.Add(memory.ToArray());
            buffer.Reader.Advance(memory.Length);
        }

        return chunks.SelectMany(c => c).ToArray();
    }

    // -------------------------------------------------------------------------
    // ReadOnlySequence<byte> helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Creates a single-segment <see cref="ReadOnlySequence{T}"/> from an array.
    /// </summary>
    internal static ReadOnlySequence<byte> CreateSequence(byte[] data)
        => new(data);

    /// <summary>
    /// Creates a multi-segment <see cref="ReadOnlySequence{T}"/> from two or
    /// more byte arrays. Useful for testing decoders against fragmented input.
    /// </summary>
    internal static ReadOnlySequence<byte> CreateSequence(params byte[][] segments)
    {
        if (segments.Length == 0)
            return ReadOnlySequence<byte>.Empty;

        if (segments.Length == 1)
            return new ReadOnlySequence<byte>(segments[0]);

        var first = new ByteArraySegment(segments[0]);
        var last = first;
        for (var i = 1; i < segments.Length; i++)
            last = last.Append(segments[i]);

        return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }

    // -------------------------------------------------------------------------
    // Frame encoding helpers (independent of the codec under test)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Builds a length-prefixed frame byte-by-byte so tests can construct
    /// reference inputs without depending on the encoder under test.
    /// </summary>
    internal static byte[] BuildExpectedFrame(byte[] payload)
    {
        var frame = new byte[4 + payload.Length];
        BinaryPrimitives.WriteInt32BigEndian(frame, payload.Length);
        payload.CopyTo(frame, 4);
        return frame;
    }

    /// <summary>
    /// Builds a 4-byte big-endian header with the given payload length followed
    /// by the supplied payload data.
    /// </summary>
    internal static byte[] BuildFrame(int length, byte[] payload)
    {
        var frame = new byte[4 + payload.Length];
        BinaryPrimitives.WriteInt32BigEndian(frame, length);
        payload.CopyTo(frame, 4);
        return frame;
    }

    /// <summary>
    /// Creates a 4-byte big-endian length prefix only, with no payload bytes.
    /// </summary>
    internal static byte[] BuildHeader(int value)
    {
        var header = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(header, value);
        return header;
    }

    // -------------------------------------------------------------------------
    // Internal multi-segment helper
    // -------------------------------------------------------------------------

    private sealed class ByteArraySegment : ReadOnlySequenceSegment<byte>
    {
        public ByteArraySegment(ReadOnlyMemory<byte> memory)
        {
            Memory = memory;
        }

        public ByteArraySegment Append(ReadOnlyMemory<byte> memory)
        {
            var next = new ByteArraySegment(memory)
            {
                RunningIndex = RunningIndex + Memory.Length
            };
            Next = next;
            return next;
        }
    }
}
