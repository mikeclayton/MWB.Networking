using MWB.Networking.Layer1_Framing.Codec;
using MWB.Networking.Layer1_Framing.Codec.Buffer;
using MWB.Networking.Layer1_Framing.Codecs.Reverse.Frame;

namespace MWB.Networking.Layer1_Framing.Codecs.Reverse.UnitTests;

/// <summary>
/// Smoke tests for <see cref="ReverseFrameCodec"/>.
///
/// The key properties under test are:
/// <list type="bullet">
///   <item>Each segment's bytes are reversed.</item>
///   <item>Segments are emitted in reverse order.</item>
///   <item>The concatenated output equals the concatenated input reversed.</item>
///   <item>Applying the transform twice restores the original bytes (it is its
///         own inverse).</item>
/// </list>
/// </summary>
[TestClass]
public sealed class ReverseFrameCodecTests
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

    /// <summary>
    /// Reads the output buffer one segment at a time, returning each segment
    /// as a separate array.  This lets tests assert both the count and the
    /// per-segment content, not just the concatenated bytes.
    /// </summary>
    private static List<byte[]> ReadSegments(CodecBuffer buffer)
    {
        var segments = new List<byte[]>();
        while (buffer.Reader.TryRead(out var memory))
        {
            segments.Add(memory.ToArray());
            buffer.Reader.Advance(memory.Length);
        }
        return segments;
    }

    private static byte[] ReadAll(CodecBuffer buffer)
        => ReadSegments(buffer).SelectMany(s => s).ToArray();

    private static byte[] ApplyTransform(byte[][] inputSegments, bool encode)
    {
        var input = CreateBuffer(inputSegments);
        var output = new CodecBuffer();
        var codec = new ReverseFrameCodec();
        if (encode)
            codec.Encode(input.Reader, output.Writer);
        else
            codec.Decode(input.Reader, output.Writer);
        output.Writer.Complete();
        return ReadAll(output);
    }

    // -------------------------------------------------------------------------
    // Encode — byte content
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Encode_SingleSegment_BytesAreReversed()
    {
        var input = CreateBuffer([0x01, 0x02, 0x03]);
        var output = new CodecBuffer();

        new ReverseFrameCodec().Encode(input.Reader, output.Writer);
        output.Writer.Complete();

        CollectionAssert.AreEqual(
            new byte[] { 0x03, 0x02, 0x01 },
            ReadAll(output));
    }

    [TestMethod]
    public void Encode_EmptyInput_ProducesNoOutput()
    {
        var input = CreateBuffer();
        var output = new CodecBuffer();

        new ReverseFrameCodec().Encode(input.Reader, output.Writer);
        output.Writer.Complete();

        Assert.IsEmpty(ReadAll(output));
    }

    [TestMethod]
    public void Encode_SingleByteSegment_OutputIsUnchanged()
    {
        var input = CreateBuffer([0xAB]);
        var output = new CodecBuffer();

        new ReverseFrameCodec().Encode(input.Reader, output.Writer);
        output.Writer.Complete();

        CollectionAssert.AreEqual(new byte[] { 0xAB }, ReadAll(output));
    }

    // -------------------------------------------------------------------------
    // Encode — segment structure
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Encode_TwoSegments_EachSegmentReversedAndOrderReversed()
    {
        // Input:  [0xAA, 0xBB] | [0xCC, 0xDD]
        // Output: [0xDD, 0xCC] | [0xBB, 0xAA]   (segment 2 reversed, then segment 1 reversed)
        var input = CreateBuffer([0xAA, 0xBB], [0xCC, 0xDD]);
        var output = new CodecBuffer();

        new ReverseFrameCodec().Encode(input.Reader, output.Writer);
        output.Writer.Complete();

        var segments = ReadSegments(output);

        Assert.HasCount(2, segments, "Output must contain the same number of segments as input.");
        CollectionAssert.AreEqual(new byte[] { 0xDD, 0xCC }, segments[0], "First output segment must be the reversed last input segment.");
        CollectionAssert.AreEqual(new byte[] { 0xBB, 0xAA }, segments[1], "Second output segment must be the reversed first input segment.");
    }

    [TestMethod]
    public void Encode_ThreeUnequalSegments_ConcatenatedOutputIsFullReversal()
    {
        // Input segments of different sizes — verifies the general case.
        // Input:  [0xA1] | [0xB1, 0xB2] | [0xC1, 0xC2, 0xC3]
        // Concatenated input:  A1 B1 B2 C1 C2 C3
        // Concatenated output: C3 C2 C1 B2 B1 A1   (exact reversal)
        var input = CreateBuffer(
            [0xA1],
            [0xB1, 0xB2],
            [0xC1, 0xC2, 0xC3]);
        var output = new CodecBuffer();

        new ReverseFrameCodec().Encode(input.Reader, output.Writer);
        output.Writer.Complete();

        var segments = ReadSegments(output);

        Assert.HasCount(3, segments);
        CollectionAssert.AreEqual(new byte[] { 0xC3, 0xC2, 0xC1 }, segments[0]);
        CollectionAssert.AreEqual(new byte[] { 0xB2, 0xB1 },       segments[1]);
        CollectionAssert.AreEqual(new byte[] { 0xA1 },             segments[2]);
    }

    // -------------------------------------------------------------------------
    // Decode
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Decode_NonEmptyInput_ReturnsSuccess()
    {
        var input = CreateBuffer([0x01, 0x02]);
        var output = new CodecBuffer();

        var result = new ReverseFrameCodec().Decode(input.Reader, output.Writer);

        Assert.AreEqual(FrameDecodeResult.Success, result);
    }

    [TestMethod]
    public void Decode_EmptyInput_ReturnsSuccess()
    {
        var input = CreateBuffer();
        var output = new CodecBuffer();

        var result = new ReverseFrameCodec().Decode(input.Reader, output.Writer);

        Assert.AreEqual(FrameDecodeResult.Success, result);
    }

    [TestMethod]
    public void Decode_SingleSegment_BytesAreReversed()
    {
        var input = CreateBuffer([0xDE, 0xAD, 0xBE, 0xEF]);
        var output = new CodecBuffer();

        new ReverseFrameCodec().Decode(input.Reader, output.Writer);
        output.Writer.Complete();

        CollectionAssert.AreEqual(
            new byte[] { 0xEF, 0xBE, 0xAD, 0xDE },
            ReadAll(output));
    }

    // -------------------------------------------------------------------------
    // Round-trip — applying the transform twice restores the original
    // -------------------------------------------------------------------------

    [TestMethod]
    public void RoundTrip_EncodeThenDecode_SingleSegment_RestoresOriginalBytes()
    {
        // Reversing is its own inverse: applying it twice must give back the
        // original bytes regardless of content.
        var original = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
        var codec = new ReverseFrameCodec();

        // Encode pass
        var after1 = new CodecBuffer();
        codec.Encode(CreateBuffer(original).Reader, after1.Writer);
        after1.Writer.Complete();

        // Decode pass (re-use the encoded output as input)
        var after2 = new CodecBuffer();
        codec.Decode(after1.Reader, after2.Writer);
        after2.Writer.Complete();

        CollectionAssert.AreEqual(original, ReadAll(after2),
            "Two applications of the reverse transform must restore the original bytes.");
    }

    [TestMethod]
    public void RoundTrip_EncodeThenDecode_MultiSegment_RestoresOriginalStructure()
    {
        // Each segment should be individually restored after a second pass.
        // Input:  [0x01, 0x02] | [0x03, 0x04, 0x05]
        // After 1st pass: [0x05, 0x04, 0x03] | [0x02, 0x01]
        // After 2nd pass: [0x01, 0x02] | [0x03, 0x04, 0x05]   (restored)
        var codec = new ReverseFrameCodec();

        var after1 = new CodecBuffer();
        codec.Encode(CreateBuffer([0x01, 0x02], [0x03, 0x04, 0x05]).Reader, after1.Writer);
        after1.Writer.Complete();

        var after2 = new CodecBuffer();
        codec.Decode(after1.Reader, after2.Writer);
        after2.Writer.Complete();

        var segments = ReadSegments(after2);

        Assert.HasCount(2, segments);
        CollectionAssert.AreEqual(new byte[] { 0x01, 0x02 },       segments[0]);
        CollectionAssert.AreEqual(new byte[] { 0x03, 0x04, 0x05 }, segments[1]);
    }
}
