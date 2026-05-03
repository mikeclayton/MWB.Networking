using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer1_Framing.Codec.Exceptions;
using MWB.Networking.Layer1_Framing.Codecs.LengthPrefixed.Transport;
using MWB.Networking.Layer1_Framing.Codecs.LengthPrefixed.UnitTests.Helpers;
using System.Buffers;
using System.Buffers.Binary;

namespace MWB.Networking.Layer1_Framing.Codecs.LengthPrefixed.UnitTests;

/// <summary>
/// Isolated unit tests for <see cref="LengthPrefixedTransportDecoder"/>.
///
/// All inputs are constructed directly as byte arrays and
/// <see cref="ReadOnlySequence{T}"/> values — no pipeline, no transport.
/// </summary>
[TestClass]
public sealed class LengthPrefixedTransportDecoderTests
{
    public TestContext TestContext { get; set; } = null!;

    private static LengthPrefixedTransportDecoder CreateDecoder(
        int maxFrameSize = 16 * 1024 * 1024)
        => new(NullLogger.Instance, maxFrameSize);

    // -------------------------------------------------------------------------
    // Constructor validation
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(
            () => new LengthPrefixedTransportDecoder(null!));
    }

    [TestMethod]
    public void Constructor_WithZeroMaxFrameSize_ThrowsArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => new LengthPrefixedTransportDecoder(NullLogger.Instance, 0));
    }

    [TestMethod]
    public void Constructor_WithNegativeMaxFrameSize_ThrowsArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => new LengthPrefixedTransportDecoder(NullLogger.Instance, -1));
    }

    [TestMethod]
    public void Constructor_ValidArguments_DoesNotThrow()
    {
        _ = new LengthPrefixedTransportDecoder(NullLogger.Instance, 1024);
    }

    // -------------------------------------------------------------------------
    // Incomplete input → returns false (needs more bytes)
    // -------------------------------------------------------------------------

    [TestMethod]
    public void TryDecode_EmptySequence_ReturnsFalse()
    {
        var decoder = CreateDecoder();
        var input = ReadOnlySequence<byte>.Empty;

        var result = decoder.TryDecode(ref input, out _);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void TryDecode_OneByte_ReturnsFalse()
    {
        var decoder = CreateDecoder();
        var input = CodecTestHelpers.CreateSequence(new byte[] { 0x00 });

        var result = decoder.TryDecode(ref input, out _);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void TryDecode_TwoBytes_ReturnsFalse()
    {
        var decoder = CreateDecoder();
        var input = CodecTestHelpers.CreateSequence(new byte[] { 0x00, 0x00 });

        var result = decoder.TryDecode(ref input, out _);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void TryDecode_ThreeBytes_ReturnsFalse()
    {
        var decoder = CreateDecoder();
        var input = CodecTestHelpers.CreateSequence(new byte[] { 0x00, 0x00, 0x00 });

        var result = decoder.TryDecode(ref input, out _);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void TryDecode_HeaderIndicatesOneBytePayload_ButNoPayloadPresent_ReturnsFalse()
    {
        // Header says payload length = 1, but there are no payload bytes
        var decoder = CreateDecoder();
        var input = CodecTestHelpers.CreateSequence(CodecTestHelpers.BuildHeader(1));

        var result = decoder.TryDecode(ref input, out _);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void TryDecode_HeaderIndicatesThreeBytePayload_ButOnlyOnePayloadByte_ReturnsFalse()
    {
        var decoder = CreateDecoder();
        var input = CodecTestHelpers.CreateSequence(
            [.. CodecTestHelpers.BuildHeader(3), 0x01]);

        var result = decoder.TryDecode(ref input, out _);

        Assert.IsFalse(result);
    }

    // -------------------------------------------------------------------------
    // Incomplete input → sequence not advanced
    // -------------------------------------------------------------------------

    [TestMethod]
    public void TryDecode_ReturnsFalse_SequenceIsUnchanged()
    {
        // When TryDecode returns false, the sequence position must be unchanged
        // so the caller can accumulate more bytes and try again.
        var decoder = CreateDecoder();
        var original = CodecTestHelpers.CreateSequence(new byte[] { 0x00, 0x00, 0x00 });
        var input = original;

        decoder.TryDecode(ref input, out _);

        Assert.AreEqual(original.Length, input.Length,
            "Sequence must not be advanced when TryDecode returns false.");
    }

    // -------------------------------------------------------------------------
    // Complete frame → returns true
    // -------------------------------------------------------------------------

    [TestMethod]
    public void TryDecode_ZeroLengthPayload_ReturnsTrueAndEmptyPayload()
    {
        // A 4-byte header of [0,0,0,0] is a valid zero-length frame.
        var decoder = CreateDecoder();
        var input = CodecTestHelpers.CreateSequence(CodecTestHelpers.BuildHeader(0));

        var result = decoder.TryDecode(ref input, out var payload);

        Assert.IsTrue(result);
        Assert.AreEqual(0, payload.Length);
    }

    [TestMethod]
    public void TryDecode_SmallPayload_ReturnsTrueAndCorrectPayload()
    {
        var decoder = CreateDecoder();
        var data = new byte[] { 0x01, 0x02, 0x03 };
        var input = CodecTestHelpers.CreateSequence(
            CodecTestHelpers.BuildExpectedFrame(data));

        var result = decoder.TryDecode(ref input, out var payload);

        Assert.IsTrue(result);
        CollectionAssert.AreEqual(data, payload.ToArray());
    }

    [TestMethod]
    public void TryDecode_LargePayload_ReturnsTrueAndCorrectPayload()
    {
        var decoder = CreateDecoder();
        var data = new byte[64 * 1024]; // 64 KB
        new Random(12345).NextBytes(data);
        var input = CodecTestHelpers.CreateSequence(
            CodecTestHelpers.BuildExpectedFrame(data));

        var result = decoder.TryDecode(ref input, out var payload);

        Assert.IsTrue(result);
        CollectionAssert.AreEqual(data, payload.ToArray());
    }

    // -------------------------------------------------------------------------
    // Sequence advancement on success
    // -------------------------------------------------------------------------

    [TestMethod]
    public void TryDecode_OnSuccess_AdvancesSequencePastFrame()
    {
        // After a successful decode the sequence must point past the frame.
        var decoder = CreateDecoder();
        var payload = new byte[] { 0xAA, 0xBB };
        var input = CodecTestHelpers.CreateSequence(
            CodecTestHelpers.BuildExpectedFrame(payload));

        var originalLength = input.Length; // 4 + 2 = 6
        decoder.TryDecode(ref input, out _);

        Assert.AreEqual(0, input.Length,
            "Sequence must be fully consumed when a single frame exactly fills it.");
    }

    [TestMethod]
    public void TryDecode_OnSuccess_AdvancesSequencePastFrameOnly()
    {
        // Extra trailing bytes after the frame must remain in the sequence.
        var decoder = CreateDecoder();
        var payload = new byte[] { 0x01, 0x02, 0x03 };
        var trailing = new byte[] { 0xFF, 0xFE };
        var frame = CodecTestHelpers.BuildExpectedFrame(payload);
        var input = CodecTestHelpers.CreateSequence(
            frame.Concat(trailing).ToArray());

        decoder.TryDecode(ref input, out _);

        // Only the trailing bytes should remain
        Assert.AreEqual(trailing.Length, input.Length);
        CollectionAssert.AreEqual(trailing, input.ToArray());
    }

    // -------------------------------------------------------------------------
    // Big-endian byte order
    // -------------------------------------------------------------------------

    [TestMethod]
    public void TryDecode_LengthPrefix_IsInterpretedAsBigEndian()
    {
        // Payload length 256 (0x0100) must be read as big-endian:
        //   [0x00, 0x00, 0x01, 0x00] → 256, not [0x00, 0x01, 0x00, 0x00] → 65536.
        var decoder = CreateDecoder();
        var payload = new byte[256];
        new Random(99).NextBytes(payload);
        var frame = new byte[4 + 256];
        frame[0] = 0x00;
        frame[1] = 0x00;
        frame[2] = 0x01;
        frame[3] = 0x00; // big-endian 256
        payload.CopyTo(frame, 4);
        var input = CodecTestHelpers.CreateSequence(frame);

        var result = decoder.TryDecode(ref input, out var decoded);

        Assert.IsTrue(result, "Must decode a frame with 256-byte payload.");
        Assert.AreEqual(256, decoded.Length);
        CollectionAssert.AreEqual(payload, decoded.ToArray());
    }

    // -------------------------------------------------------------------------
    // Fatal errors → TransportDecodeException
    // -------------------------------------------------------------------------

    [TestMethod]
    public void TryDecode_NegativeLengthPrefix_ThrowsTransportDecodeException()
    {
        // A leading byte of 0x80 or higher sets the sign bit, giving a negative
        // signed int32. This is always invalid framing.
        var decoder = CreateDecoder();
        var input = CodecTestHelpers.CreateSequence(
            new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }); // -1 in big-endian int32

        Assert.ThrowsExactly<TransportDecodeException>(
            () => decoder.TryDecode(ref input, out _));
    }

    [TestMethod]
    public void TryDecode_NegativePrefix_AllSignBits_ThrowsTransportDecodeException()
    {
        // int.MinValue — 0x80 0x00 0x00 0x00
        var decoder = CreateDecoder();
        var input = CodecTestHelpers.CreateSequence(
            new byte[] { 0x80, 0x00, 0x00, 0x00 });

        Assert.ThrowsExactly<TransportDecodeException>(
            () => decoder.TryDecode(ref input, out _));
    }

    [TestMethod]
    public void TryDecode_LengthExceedsDefaultMaxFrameSize_ThrowsTransportDecodeException()
    {
        // Default max is 16 MB. 16 MB + 1 must throw.
        const int defaultMax = 16 * 1024 * 1024;
        var decoder = CreateDecoder(defaultMax);
        var tooLong = CodecTestHelpers.BuildHeader(defaultMax + 1);
        var input = CodecTestHelpers.CreateSequence(tooLong);

        Assert.ThrowsExactly<TransportDecodeException>(
            () => decoder.TryDecode(ref input, out _));
    }

    [TestMethod]
    public void TryDecode_LengthExceedsCustomMaxFrameSize_ThrowsTransportDecodeException()
    {
        const int maxFrameSize = 1024;
        var decoder = CreateDecoder(maxFrameSize);
        var input = CodecTestHelpers.CreateSequence(
            CodecTestHelpers.BuildHeader(maxFrameSize + 1));

        Assert.ThrowsExactly<TransportDecodeException>(
            () => decoder.TryDecode(ref input, out _));
    }

    // -------------------------------------------------------------------------
    // maxFrameSize boundary: equals is accepted, exceeds is rejected
    // -------------------------------------------------------------------------

    [TestMethod]
    public void TryDecode_LengthEqualsMaxFrameSize_ReturnsFalseUntilPayloadArrives()
    {
        // A frame whose length exactly equals maxFrameSize is valid.
        // With only the header present, TryDecode must return false (needs payload).
        const int maxFrameSize = 128;
        var decoder = CreateDecoder(maxFrameSize);
        var input = CodecTestHelpers.CreateSequence(
            CodecTestHelpers.BuildHeader(maxFrameSize));

        var result = decoder.TryDecode(ref input, out _);

        // No exception, and false because payload hasn't arrived yet
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void TryDecode_LengthEqualsMaxFrameSize_DecodesSuccessfullyWhenPayloadPresent()
    {
        const int maxFrameSize = 16;
        var decoder = CreateDecoder(maxFrameSize);
        var payload = new byte[maxFrameSize];
        new Random(7).NextBytes(payload);
        var input = CodecTestHelpers.CreateSequence(
            CodecTestHelpers.BuildExpectedFrame(payload));

        var result = decoder.TryDecode(ref input, out var decoded);

        Assert.IsTrue(result);
        CollectionAssert.AreEqual(payload, decoded.ToArray());
    }

    // -------------------------------------------------------------------------
    // Multiple frames in one buffer
    // -------------------------------------------------------------------------

    [TestMethod]
    public void TryDecode_TwoFramesBackToBack_DecodesFirstOnly()
    {
        var decoder = CreateDecoder();
        var payload1 = new byte[] { 0x01, 0x02 };
        var payload2 = new byte[] { 0x03, 0x04, 0x05 };
        var combined = CodecTestHelpers.BuildExpectedFrame(payload1)
            .Concat(CodecTestHelpers.BuildExpectedFrame(payload2))
            .ToArray();
        var input = CodecTestHelpers.CreateSequence(combined);

        var result = decoder.TryDecode(ref input, out var decoded);

        Assert.IsTrue(result);
        CollectionAssert.AreEqual(payload1, decoded.ToArray(),
            "First call must decode the first frame only.");
        Assert.AreEqual(
            4 + payload2.Length, input.Length,
            "Sequence must have advanced exactly past the first frame.");
    }

    [TestMethod]
    public void TryDecode_TwoFramesBackToBack_SecondCallDecodesSecondFrame()
    {
        var decoder = CreateDecoder();
        var payload1 = new byte[] { 0xAA };
        var payload2 = new byte[] { 0xBB, 0xCC };
        var combined = CodecTestHelpers.BuildExpectedFrame(payload1)
            .Concat(CodecTestHelpers.BuildExpectedFrame(payload2))
            .ToArray();
        var input = CodecTestHelpers.CreateSequence(combined);

        decoder.TryDecode(ref input, out _);           // decode first frame
        var result = decoder.TryDecode(ref input, out var decoded); // decode second

        Assert.IsTrue(result);
        CollectionAssert.AreEqual(payload2, decoded.ToArray());
        Assert.AreEqual(0, input.Length,
            "Sequence must be empty after both frames are consumed.");
    }

    [TestMethod]
    public void TryDecode_ManyFrames_AllDecodeInOrder()
    {
        var decoder = CreateDecoder();
        const int frameCount = 20;
        var payloads = Enumerable.Range(0, frameCount)
            .Select(i => Enumerable.Range(0, i + 1).Select(b => (byte)(b % 256)).ToArray())
            .ToList();

        var allBytes = payloads
            .SelectMany(p => CodecTestHelpers.BuildExpectedFrame(p))
            .ToArray();

        var input = CodecTestHelpers.CreateSequence(allBytes);

        for (var i = 0; i < frameCount; i++)
        {
            var result = decoder.TryDecode(ref input, out var decoded);
            Assert.IsTrue(result, $"Frame {i} must decode successfully.");
            CollectionAssert.AreEqual(payloads[i], decoded.ToArray(),
                $"Frame {i} payload mismatch.");
        }

        Assert.AreEqual(0, input.Length,
            "Sequence must be empty after all frames are decoded.");
    }

    // -------------------------------------------------------------------------
    // Multi-segment input (fragmented receive buffer)
    // -------------------------------------------------------------------------

    [TestMethod]
    public void TryDecode_HeaderSplitAcrossSegments_DecodesCorrectly()
    {
        // Simulates the header arriving in two separate network reads.
        var decoder = CreateDecoder();
        var payload = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        var frame = CodecTestHelpers.BuildExpectedFrame(payload);

        // Split the 4-byte header across two segments
        var input = CodecTestHelpers.CreateSequence(
            frame[..2],                    // first 2 header bytes
            frame[2..]);                   // remaining header + payload

        var result = decoder.TryDecode(ref input, out var decoded);

        Assert.IsTrue(result);
        CollectionAssert.AreEqual(payload, decoded.ToArray());
    }

    [TestMethod]
    public void TryDecode_PayloadSplitAcrossSegments_DecodesCorrectly()
    {
        // Simulates the payload arriving in two separate network reads.
        var decoder = CreateDecoder();
        var payload = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06 };
        var frame = CodecTestHelpers.BuildExpectedFrame(payload);

        // Header in first segment, payload split across two more
        var input = CodecTestHelpers.CreateSequence(
            frame[..4],                    // header only
            frame[4..7],                   // first half of payload
            frame[7..]);                   // second half of payload

        var result = decoder.TryDecode(ref input, out var decoded);

        Assert.IsTrue(result);
        CollectionAssert.AreEqual(payload, decoded.ToArray());
    }

    [TestMethod]
    public void TryDecode_HeaderAndPayloadBothFragmented_DecodesCorrectly()
    {
        // Extreme fragmentation: each byte in its own segment.
        var decoder = CreateDecoder();
        var payload = new byte[] { 0x10, 0x20, 0x30 };
        var frame = CodecTestHelpers.BuildExpectedFrame(payload);

        var segments = frame.Select(b => new byte[] { b }).ToArray();
        var input = CodecTestHelpers.CreateSequence(segments);

        var result = decoder.TryDecode(ref input, out var decoded);

        Assert.IsTrue(result);
        CollectionAssert.AreEqual(payload, decoded.ToArray());
    }

    [TestMethod]
    public void TryDecode_FrameSpansThreeSegments_DecodesCorrectly()
    {
        var decoder = CreateDecoder();
        var payload = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD, 0xEE };
        var frame = CodecTestHelpers.BuildExpectedFrame(payload);

        // [header] | [first 2 payload bytes] | [last 3 payload bytes]
        var input = CodecTestHelpers.CreateSequence(
            frame[..4],
            frame[4..6],
            frame[6..]);

        var result = decoder.TryDecode(ref input, out var decoded);

        Assert.IsTrue(result);
        CollectionAssert.AreEqual(payload, decoded.ToArray());
    }
}
