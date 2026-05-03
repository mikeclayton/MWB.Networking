using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer1_Framing.Codec.Buffer;
using MWB.Networking.Layer1_Framing.Codecs.LengthPrefixed.Transport;
using MWB.Networking.Layer1_Framing.Codecs.LengthPrefixed.UnitTests.Helpers;

namespace MWB.Networking.Layer1_Framing.Codecs.LengthPrefixed.UnitTests;

/// <summary>
/// Tests for <see cref="LengthPrefixedTransportCodec"/> as a whole, focusing on:
/// <list type="bullet">
///   <item>Construction and composition (Encoder / Decoder wired correctly).</item>
///   <item>End-to-end round-trips: encode then decode produces the original payload.</item>
///   <item>Multiple frames: sequential encode/decode cycles all succeed.</item>
/// </list>
///
/// Detailed behavioural edge cases are covered by the dedicated encoder
/// and decoder test files.
/// </summary>
[TestClass]
public sealed class LengthPrefixedTransportCodecTests
{
    public TestContext TestContext { get; set; } = null!;

    private static LengthPrefixedTransportCodec CreateCodec(
        int maxFrameSize = 16 * 1024 * 1024)
        => new(NullLogger.Instance, maxFrameSize);

    // -------------------------------------------------------------------------
    // Construction
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(
            () => new LengthPrefixedTransportCodec(null!));
    }

    [TestMethod]
    public void Constructor_ValidArguments_ExposesEncoderAndDecoder()
    {
        var codec = CreateCodec();

        Assert.IsNotNull(codec.Encoder);
        Assert.IsNotNull(codec.Decoder);
    }

    [TestMethod]
    public void Constructor_EncoderAndDecoderAreCorrectTypes()
    {
        var codec = CreateCodec();

        Assert.IsInstanceOfType<LengthPrefixedTransportEncoder>(codec.Encoder);
        Assert.IsInstanceOfType<LengthPrefixedTransportDecoder>(codec.Decoder);
    }

    // -------------------------------------------------------------------------
    // Round-trip helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Encodes <paramref name="payload"/> using the codec, then decodes the
    /// resulting bytes and returns the recovered payload.
    /// </summary>
    private static byte[] RoundTrip(LengthPrefixedTransportCodec codec, byte[] payload)
    {
        // ── encode ──────────────────────────────────────────────────────────
        var inputBuffer = CodecTestHelpers.CreateInputBuffer(payload);
        var encodedBuffer = new CodecBuffer();
        codec.Encoder.Encode(inputBuffer.Reader, encodedBuffer.Writer);
        var encodedBytes = CodecTestHelpers.ReadAllOutput(encodedBuffer);

        // ── decode ───────────────────────────────────────────────────────────
        var sequence = CodecTestHelpers.CreateSequence(encodedBytes);
        var decoded = codec.TryDecode(ref sequence, out var output);

        Assert.IsTrue(decoded, "TryDecode must succeed on a complete, freshly encoded frame.");
        return output.ToArray();
    }

    // -------------------------------------------------------------------------
    // Round-trip tests
    // -------------------------------------------------------------------------

    [TestMethod]
    public void RoundTrip_EmptyPayload_DecodesIdentically()
    {
        var codec = CreateCodec();
        var payload = Array.Empty<byte>();

        var recovered = RoundTrip(codec, payload);

        CollectionAssert.AreEqual(payload, recovered);
    }

    [TestMethod]
    public void RoundTrip_SingleBytePayload_DecodesIdentically()
    {
        var codec = CreateCodec();
        var payload = new byte[] { 0x42 };

        var recovered = RoundTrip(codec, payload);

        CollectionAssert.AreEqual(payload, recovered);
    }

    [TestMethod]
    public void RoundTrip_SmallPayload_DecodesIdentically()
    {
        var codec = CreateCodec();
        var payload = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };

        var recovered = RoundTrip(codec, payload);

        CollectionAssert.AreEqual(payload, recovered);
    }

    [TestMethod]
    public void RoundTrip_LargePayload_DecodesIdentically()
    {
        var codec = CreateCodec();
        var payload = new byte[1024 * 1024]; // 1 MB
        new Random(42).NextBytes(payload);

        var recovered = RoundTrip(codec, payload);

        CollectionAssert.AreEqual(payload, recovered);
    }

    [TestMethod]
    public void RoundTrip_BinaryPayload_AllByteValues_DecodesIdentically()
    {
        // All 256 possible byte values in a single payload — no byte value
        // should be treated specially by the codec.
        var codec = CreateCodec();
        var payload = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();

        var recovered = RoundTrip(codec, payload);

        CollectionAssert.AreEqual(payload, recovered);
    }

    // -------------------------------------------------------------------------
    // Multiple sequential frames
    // -------------------------------------------------------------------------

    [TestMethod]
    public void RoundTrip_MultipleFramesEncoded_AllDecodeInOrder()
    {
        // Encode several frames, concatenate them, then decode sequentially.
        var codec = CreateCodec();
        var payloads = new[]
        {
            new byte[] { 0xAA },
            new byte[] { 0x01, 0x02, 0x03 },
            Array.Empty<byte>(),
            new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }
        };

        // Encode all frames in order
        var encodedAll = new List<byte>();
        foreach (var payload in payloads)
        {
            var inputBuffer = CodecTestHelpers.CreateInputBuffer(payload);
            var encodedBuffer = new CodecBuffer();
            codec.Encoder.Encode(inputBuffer.Reader, encodedBuffer.Writer);
            encodedAll.AddRange(CodecTestHelpers.ReadAllOutput(encodedBuffer));
        }

        // Decode sequentially from the concatenated buffer
        var sequence = CodecTestHelpers.CreateSequence(encodedAll.ToArray());
        for (var i = 0; i < payloads.Length; i++)
        {
            var result = codec.TryDecode(ref sequence, out var decoded);
            Assert.IsTrue(result, $"Frame {i} must decode successfully.");
            CollectionAssert.AreEqual(payloads[i], decoded.ToArray(),
                $"Frame {i} payload mismatch.");
        }

        Assert.AreEqual(0, sequence.Length,
            "All bytes must be consumed after decoding all frames.");
    }

    [TestMethod]
    public void RoundTrip_ManySmallFrames_AllDecodeInOrder()
    {
        var codec = CreateCodec();
        const int frameCount = 100;

        var payloads = Enumerable.Range(0, frameCount)
            .Select(i => new byte[] { (byte)(i % 256) })
            .ToList();

        var encodedAll = new List<byte>();
        foreach (var payload in payloads)
        {
            var inputBuffer = CodecTestHelpers.CreateInputBuffer(payload);
            var encodedBuffer = new CodecBuffer();
            codec.Encoder.Encode(inputBuffer.Reader, encodedBuffer.Writer);
            encodedAll.AddRange(CodecTestHelpers.ReadAllOutput(encodedBuffer));
        }

        var sequence = CodecTestHelpers.CreateSequence(encodedAll.ToArray());
        for (var i = 0; i < frameCount; i++)
        {
            var result = codec.TryDecode(ref sequence, out var decoded);
            Assert.IsTrue(result, $"Frame {i} must decode.");
            CollectionAssert.AreEqual(payloads[i], decoded.ToArray(),
                $"Frame {i} payload mismatch.");
        }

        Assert.AreEqual(0, sequence.Length);
    }

    // -------------------------------------------------------------------------
    // ITransportCodec delegation
    // -------------------------------------------------------------------------

    [TestMethod]
    public void ITransportCodec_Encode_DelegatesToEncoder()
    {
        // When called through the interface, the codec must produce the same
        // output as calling Encoder.Encode() directly.
        var codec = CreateCodec();
        var payload = new byte[] { 0x01, 0x02, 0x03 };

        // Via interface
        var inputBuffer1 = CodecTestHelpers.CreateInputBuffer(payload);
        var outputBuffer1 = new CodecBuffer();
        ((MWB.Networking.Layer1_Framing.Codec.Abstractions.ITransportCodec)codec)
            .Encode(inputBuffer1.Reader, outputBuffer1.Writer);
        var interfaceOutput = CodecTestHelpers.ReadAllOutput(outputBuffer1);

        // Via encoder directly
        var inputBuffer2 = CodecTestHelpers.CreateInputBuffer(payload);
        var outputBuffer2 = new CodecBuffer();
        codec.Encoder.Encode(inputBuffer2.Reader, outputBuffer2.Writer);
        var encoderOutput = CodecTestHelpers.ReadAllOutput(outputBuffer2);

        CollectionAssert.AreEqual(encoderOutput, interfaceOutput,
            "ITransportCodec.Encode must produce the same output as Encoder.Encode.");
    }

    [TestMethod]
    public void ITransportCodec_TryDecode_DelegatesToDecoder()
    {
        // TryDecode on the codec must produce the same result as calling
        // Decoder.TryDecode directly.
        var codec = CreateCodec();
        var payload = new byte[] { 0xAA, 0xBB };
        var frame = CodecTestHelpers.BuildExpectedFrame(payload);

        var sequence1 = CodecTestHelpers.CreateSequence(frame);
        var sequence2 = CodecTestHelpers.CreateSequence(frame);

        var codecResult = codec.TryDecode(ref sequence1, out var codecPayload);
        var decoderResult = codec.Decoder.TryDecode(ref sequence2, out var decoderPayload);

        Assert.AreEqual(decoderResult, codecResult);
        CollectionAssert.AreEqual(decoderPayload.ToArray(), codecPayload.ToArray());
    }

    // -------------------------------------------------------------------------
    // Custom maxFrameSize is respected end-to-end
    // -------------------------------------------------------------------------

    [TestMethod]
    public void RoundTrip_PayloadAtCustomMaxFrameSize_Succeeds()
    {
        const int maxFrameSize = 64;
        var codec = CreateCodec(maxFrameSize);
        var payload = new byte[maxFrameSize];
        new Random(5).NextBytes(payload);

        var recovered = RoundTrip(codec, payload);

        CollectionAssert.AreEqual(payload, recovered);
    }
}
