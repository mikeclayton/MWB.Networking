using MWB.Networking.Layer1_Framing.Codec;
using MWB.Networking.Layer1_Framing.Codec.Buffer;
using MWB.Networking.Layer1_Framing.Codecs.Null.Frame;

namespace MWB.Networking.Layer1_Framing.Codecs.Null.UnitTests;

/// <summary>
/// Smoke tests for <see cref="NullFrameCodec"/>.
/// Confirms that both directions pass bytes through unchanged and always
/// report <see cref="FrameDecodeResult.Success"/>.
/// </summary>
[TestClass]
public sealed class NullFrameCodecTests
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

    // -------------------------------------------------------------------------
    // Encode
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Encode_NonEmptyInput_OutputMatchesInput()
    {
        var payload = new byte[] { 0x01, 0x02, 0x03 };
        var input = CreateBuffer(payload);
        var output = new CodecBuffer();

        new NullFrameCodec().Encode(input.Reader, output.Writer);
        output.Writer.Complete();

        CollectionAssert.AreEqual(payload, ReadAll(output));
    }

    [TestMethod]
    public void Encode_EmptyInput_ProducesNoOutput()
    {
        var input = CreateBuffer();
        var output = new CodecBuffer();

        new NullFrameCodec().Encode(input.Reader, output.Writer);
        output.Writer.Complete();

        Assert.IsEmpty(ReadAll(output));
    }

    [TestMethod]
    public void Encode_MultiSegmentInput_ConcatenatesAllSegments()
    {
        var input = CreateBuffer([0xAA, 0xBB], [0xCC, 0xDD]);
        var output = new CodecBuffer();

        new NullFrameCodec().Encode(input.Reader, output.Writer);
        output.Writer.Complete();

        CollectionAssert.AreEqual(
            new byte[] { 0xAA, 0xBB, 0xCC, 0xDD },
            ReadAll(output));
    }

    // -------------------------------------------------------------------------
    // Decode
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Decode_NonEmptyInput_ReturnsSuccess()
    {
        var input = CreateBuffer([0x01]);
        var output = new CodecBuffer();

        var result = new NullFrameCodec().Decode(input.Reader, output.Writer);

        Assert.AreEqual(FrameDecodeResult.Success, result);
    }

    [TestMethod]
    public void Decode_NonEmptyInput_OutputMatchesInput()
    {
        var payload = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        var input = CreateBuffer(payload);
        var output = new CodecBuffer();

        new NullFrameCodec().Decode(input.Reader, output.Writer);
        output.Writer.Complete();

        CollectionAssert.AreEqual(payload, ReadAll(output));
    }

    [TestMethod]
    public void Decode_EmptyInput_ReturnsSuccess()
    {
        var input = CreateBuffer();
        var output = new CodecBuffer();

        var result = new NullFrameCodec().Decode(input.Reader, output.Writer);

        Assert.AreEqual(FrameDecodeResult.Success, result);
    }

    [TestMethod]
    public void Decode_EmptyInput_ProducesNoOutput()
    {
        var input = CreateBuffer();
        var output = new CodecBuffer();

        new NullFrameCodec().Decode(input.Reader, output.Writer);
        output.Writer.Complete();

        Assert.IsEmpty(ReadAll(output));
    }
}
