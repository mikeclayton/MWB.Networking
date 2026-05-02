using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer1_Framing.Codec.Buffer;
using MWB.Networking.Layer1_Framing.Codecs.LengthPrefixed.Transport;
using MWB.Networking.Layer1_Framing.Codecs.LengthPrefixed.UnitTests.Helpers;

namespace MWB.Networking.Layer1_Framing.Codecs.LengthPrefixed.UnitTests;

/// <summary>
/// Isolated unit tests for <see cref="LengthPrefixedTransportEncoder"/>.
///
/// Each test constructs a <see cref="CodecBuffer"/> directly to supply input
/// and reads the output from another buffer — no pipeline, no transport.
/// </summary>
[TestClass]
public sealed class LengthPrefixedTransportEncoderTests
{
    public TestContext TestContext { get; set; } = null!;

    private static LengthPrefixedTransportEncoder CreateEncoder()
        => new(NullLogger.Instance);

    /// <summary>
    /// Runs the encoder with the given input buffer and returns all output bytes.
    /// </summary>
    private static byte[] Encode(CodecBuffer inputBuffer)
    {
        var outputBuffer = new CodecBuffer();
        var encoder = CreateEncoder();
        encoder.Encode(inputBuffer.Reader, outputBuffer.Writer);
        return CodecTestHelpers.ReadAllOutput(outputBuffer);
    }

    // -------------------------------------------------------------------------
    // Constructor validation
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(
            () => new LengthPrefixedTransportEncoder(null!));
    }

    [TestMethod]
    public void Constructor_ValidArguments_DoesNotThrow()
    {
        _ = new LengthPrefixedTransportEncoder(NullLogger.Instance);
    }

    // -------------------------------------------------------------------------
    // Header — length prefix
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Encode_EmptyPayload_WritesFourZeroByteHeader()
    {
        var input = CodecTestHelpers.CreateInputBuffer([]);
        var output = Encode(input);

        Assert.AreEqual(4, output.Length,
            "Encoding an empty payload must produce exactly the 4-byte header.");
        CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x00, 0x00 }, output);
    }

    [TestMethod]
    public void Encode_SingleBytePayload_WritesHeaderValueOne()
    {
        var input = CodecTestHelpers.CreateInputBuffer(new byte[] { 0x42 });
        var output = Encode(input);

        Assert.AreEqual(5, output.Length);
        // Header must be big-endian 1
        CollectionAssert.AreEqual(
            new byte[] { 0x00, 0x00, 0x00, 0x01 },
            output[..4]);
    }

    [TestMethod]
    public void Encode_SmallPayload_WritesHeaderWithCorrectBigEndianLength()
    {
        var payload = new byte[] { 0x10, 0x20, 0x30 };
        var input = CodecTestHelpers.CreateInputBuffer(payload);
        var output = Encode(input);

        Assert.AreEqual(7, output.Length, "Output must be header(4) + payload(3).");
        // Big-endian 3
        CollectionAssert.AreEqual(
            new byte[] { 0x00, 0x00, 0x00, 0x03 },
            output[..4]);
    }

    [TestMethod]
    public void Encode_255BytePayload_HeaderIsBigEndian255()
    {
        var payload = new byte[255];
        new Random(1).NextBytes(payload);
        var input = CodecTestHelpers.CreateInputBuffer(payload);
        var output = Encode(input);

        // [0x00, 0x00, 0x00, 0xFF]
        CollectionAssert.AreEqual(
            new byte[] { 0x00, 0x00, 0x00, 0xFF },
            output[..4]);
    }

    [TestMethod]
    public void Encode_256BytePayload_HeaderShowsBigEndianByteOrder()
    {
        // 256 in big-endian is [0x00, 0x00, 0x01, 0x00].
        // If it were little-endian it would be [0x00, 0x01, 0x00, 0x00].
        var payload = new byte[256];
        new Random(2).NextBytes(payload);
        var input = CodecTestHelpers.CreateInputBuffer(payload);
        var output = Encode(input);

        CollectionAssert.AreEqual(
            new byte[] { 0x00, 0x00, 0x01, 0x00 },
            output[..4],
            "Header must be big-endian (MSB first).");
    }

    [TestMethod]
    public void Encode_65536BytePayload_HeaderIsBigEndian()
    {
        // 65536 = 0x00_01_00_00 big-endian
        var payload = new byte[65536];
        new Random(3).NextBytes(payload);
        var input = CodecTestHelpers.CreateInputBuffer(payload);
        var output = Encode(input);

        CollectionAssert.AreEqual(
            new byte[] { 0x00, 0x01, 0x00, 0x00 },
            output[..4]);
    }

    // -------------------------------------------------------------------------
    // Payload — correct content
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Encode_SmallPayload_PayloadFollowsHeaderUnchanged()
    {
        var payload = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
        var input = CodecTestHelpers.CreateInputBuffer(payload);
        var output = Encode(input);

        CollectionAssert.AreEqual(payload, output[4..],
            "The payload bytes must follow the header unchanged.");
    }

    [TestMethod]
    public void Encode_LargePayload_PayloadFollowsHeaderUnchanged()
    {
        var payload = new byte[128 * 1024]; // 128 KB
        new Random(99).NextBytes(payload);
        var input = CodecTestHelpers.CreateInputBuffer(payload);
        var output = Encode(input);

        Assert.AreEqual(4 + payload.Length, output.Length);
        CollectionAssert.AreEqual(payload, output[4..]);
    }

    // -------------------------------------------------------------------------
    // Multi-segment input
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Encode_TwoSegmentInput_ConcatenatesPayloadWithSingleHeader()
    {
        // Two separate writes to the input buffer must appear as one contiguous
        // encoded payload — the header length covers both segments combined.
        var seg1 = new byte[] { 0x01, 0x02, 0x03 };
        var seg2 = new byte[] { 0x04, 0x05, 0x06 };
        var input = CodecTestHelpers.CreateInputBuffer(seg1, seg2);
        var output = Encode(input);

        // Header
        CollectionAssert.AreEqual(
            new byte[] { 0x00, 0x00, 0x00, 0x06 },
            output[..4],
            "Header must reflect the total of both segments.");

        // Payload
        CollectionAssert.AreEqual(
            new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06 },
            output[4..]);
    }

    [TestMethod]
    public void Encode_ManySegmentInput_HeaderReflectsTotalLength()
    {
        const int segmentCount = 10;
        const int segmentSize = 7;
        var expected = Enumerable.Range(0, segmentCount)
            .SelectMany(i => Enumerable.Repeat((byte)i, segmentSize))
            .ToArray();

        var segments = Enumerable.Range(0, segmentCount)
            .Select(i => Enumerable.Repeat((byte)i, segmentSize).ToArray())
            .ToArray();

        var input = CodecTestHelpers.CreateInputBuffer(segments);
        var output = Encode(input);

        Assert.AreEqual(4 + expected.Length, output.Length);
        CollectionAssert.AreEqual(expected, output[4..]);
    }

    // -------------------------------------------------------------------------
    // Complete frame structure
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Encode_Output_MatchesExpectedFrameFormat()
    {
        // Verify the complete frame format independently of other tests.
        var payload = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        var input = CodecTestHelpers.CreateInputBuffer(payload);
        var output = Encode(input);

        var expected = CodecTestHelpers.BuildExpectedFrame(payload);
        CollectionAssert.AreEqual(expected, output);
    }

    // -------------------------------------------------------------------------
    // Encoder invariant violations (misbehaving ICodecBufferReader)
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Encode_ReaderDeliverMoreBytesThanClaimed_ThrowsInvalidOperationException()
    {
        // FakeCodecBufferReader claims 3 bytes but returns a 4-byte segment.
        // The encoder must detect this and throw its overrun guard.
        var outputBuffer = new CodecBuffer();
        var encoder = CreateEncoder();
        var fakeReader = new FakeCodecBufferReader(
            claimedLength: 3,
            new byte[] { 0x01, 0x02, 0x03, 0x04 }); // 4 bytes — more than claimed

        Assert.ThrowsExactly<InvalidOperationException>(
            () => encoder.Encode(fakeReader, outputBuffer.Writer));
    }

    [TestMethod]
    public void Encode_ReaderDeliversFewerBytesThanClaimed_ThrowsInvalidOperationException()
    {
        // FakeCodecBufferReader claims 5 bytes but only provides 3.
        // The encoder must detect the underrun at the end and throw.
        var outputBuffer = new CodecBuffer();
        var encoder = CreateEncoder();
        var fakeReader = new FakeCodecBufferReader(
            claimedLength: 5,
            new byte[] { 0x01, 0x02, 0x03 }); // 3 bytes — fewer than claimed

        Assert.ThrowsExactly<InvalidOperationException>(
            () => encoder.Encode(fakeReader, outputBuffer.Writer));
    }
}
