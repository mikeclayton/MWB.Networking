using MWB.Networking.Layer1_Framing.Codec.Buffer;
using MWB.Networking.Layer1_Framing.Codecs.NullCodecs.Transport;
using System.Buffers;

namespace MWB.Networking.Layer1_Framing.Codecs.Null.UnitTests;

/// <summary>
/// Smoke tests for <see cref="NullTransportCodec"/>.
/// Confirms that encode passes bytes through unchanged, that decode treats
/// the entire available sequence as a single frame, and that an empty
/// sequence returns false.
/// </summary>
[TestClass]
public sealed class NullTransportCodecTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static CodecBuffer CreateBuffer(params byte[][] segments)
    {
        var buffer = new CodecBuffer();
        foreach (var segment in segments)
            if (segment.Length > 0)
                buffer.Writer.Write(segment);
        buffer.Writer.Complete();
        return buffer;
    }

    private static byte[] ReadAll(CodecBuffer buffer)
    {
        var chunks = new List<byte[]>();
        while (buffer.Reader.TryRead(out var memory))
        {
            chunks.Add(memory.ToArray());
            buffer.Reader.Advance(memory.Length);
        }
        return chunks.SelectMany(c => c).ToArray();
    }

    private static ReadOnlySequence<byte> SingleSegment(byte[] data)
        => new(data);

    /// <summary>
    /// Builds a multi-segment <see cref="ReadOnlySequence{T}"/> so that
    /// <c>TryDecode</c> is exercised against a non-contiguous input.
    /// </summary>
    private static ReadOnlySequence<byte> MultiSegment(params byte[][] segments)
    {
        if (segments.Length == 0) return ReadOnlySequence<byte>.Empty;
        if (segments.Length == 1) return new ReadOnlySequence<byte>(segments[0]);

        var first = new Segment(segments[0]);
        var last = first;
        for (var i = 1; i < segments.Length; i++)
            last = last.Append(segments[i]);

        return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }

    private sealed class Segment : ReadOnlySequenceSegment<byte>
    {
        internal Segment(ReadOnlyMemory<byte> memory) => Memory = memory;

        internal Segment Append(ReadOnlyMemory<byte> memory)
        {
            var next = new Segment(memory) { RunningIndex = RunningIndex + Memory.Length };
            Next = next;
            return next;
        }
    }

    // -------------------------------------------------------------------------
    // Encode
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Encode_NonEmptyInput_OutputMatchesInput()
    {
        var payload = new byte[] { 0x01, 0x02, 0x03 };
        var input = CreateBuffer(payload);
        var output = new CodecBuffer();

        new NullTransportCodec().Encode(input.Reader, output.Writer);
        output.Writer.Complete();

        CollectionAssert.AreEqual(payload, ReadAll(output));
    }

    [TestMethod]
    public void Encode_EmptyInput_ProducesNoOutput()
    {
        var input = CreateBuffer();
        var output = new CodecBuffer();

        new NullTransportCodec().Encode(input.Reader, output.Writer);
        output.Writer.Complete();

        Assert.AreEqual(0, ReadAll(output).Length);
    }

    // -------------------------------------------------------------------------
    // TryDecode
    // -------------------------------------------------------------------------

    [TestMethod]
    public void TryDecode_EmptySequence_ReturnsFalse()
    {
        var sequence = ReadOnlySequence<byte>.Empty;

        var result = new NullTransportCodec().TryDecode(ref sequence, out _);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void TryDecode_EmptySequence_SequenceUnchanged()
    {
        var sequence = ReadOnlySequence<byte>.Empty;

        new NullTransportCodec().TryDecode(ref sequence, out _);

        Assert.AreEqual(0, sequence.Length);
    }

    [TestMethod]
    public void TryDecode_NonEmptySequence_ReturnsTrue()
    {
        var sequence = SingleSegment([0xAA, 0xBB]);

        var result = new NullTransportCodec().TryDecode(ref sequence, out _);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void TryDecode_NonEmptySequence_FrameBytesContainAllInput()
    {
        var payload = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var sequence = SingleSegment(payload);

        new NullTransportCodec().TryDecode(ref sequence, out var frame);

        CollectionAssert.AreEqual(payload, frame.ToArray());
    }

    [TestMethod]
    public void TryDecode_NonEmptySequence_ConsumesEntireSequence()
    {
        var sequence = SingleSegment([0xAA, 0xBB, 0xCC]);

        new NullTransportCodec().TryDecode(ref sequence, out _);

        Assert.AreEqual(0, sequence.Length,
            "TryDecode must advance the sequence to its end.");
    }

    [TestMethod]
    public void TryDecode_MultiSegmentSequence_ReturnsAllBytesFlattened()
    {
        // Verifies that ToArray() is called on the full sequence, not just
        // the first segment.
        var sequence = MultiSegment([0xAA, 0xBB], [0xCC, 0xDD]);

        new NullTransportCodec().TryDecode(ref sequence, out var frame);

        CollectionAssert.AreEqual(
            new byte[] { 0xAA, 0xBB, 0xCC, 0xDD },
            frame.ToArray());
    }
}
