using MWB.Networking.Layer1_Framing.Codec;
using MWB.Networking.Layer1_Framing.Codec.Buffer;
using MWB.Networking.Layer1_Framing.Codec.Frames;
using MWB.Networking.Layer1_Framing.Codecs.Default.Network;
using MWB.Networking.Layer1_Framing.Codecs.Default.UnitTests.Helpers;

namespace MWB.Networking.Layer1_Framing.Codecs.Default.UnitTests;

/// <summary>
/// Isolated unit tests for the <em>decode</em> direction of
/// <see cref="DefaultNetworkFrameCodec"/>.
///
/// Tests verify:
/// <list type="bullet">
///   <item>Structural failures return <see cref="FrameDecodeResult.InvalidFrameEncoding"/>.</item>
///   <item>Each optional field is decoded correctly from its bit position and byte offset.</item>
///   <item>Field values are interpreted as big-endian uint32.</item>
///   <item>The payload is correctly captured from the remaining bytes.</item>
///   <item>Multi-segment payload input returns <see cref="FrameDecodeResult.InvalidFrameEncoding"/>
///         (documented codec limitation).</item>
///   <item>Fake-reader edge cases (negative or overflowing Length) are rejected.</item>
/// </list>
/// </summary>
[TestClass]
public sealed class DefaultNetworkFrameCodecDecodeTests
{
    public TestContext TestContext { get; set; } = null!;

    private static DefaultNetworkFrameCodec CreateCodec() => new();

    // -------------------------------------------------------------------------
    // Structural failure — too few bytes
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that an empty input buffer returns
    /// <see cref="FrameDecodeResult.InvalidFrameEncoding"/> rather than
    /// <see cref="FrameDecodeResult.NeedsMoreData"/>.
    /// </summary>
    /// <remarks>
    /// Returning <see cref="FrameDecodeResult.NeedsMoreData"/> might appear more
    /// natural for empty or truncated input, but it would be wrong here.
    /// <see cref="DefaultNetworkFrameCodec.Decode"/> sits above the transport layer
    /// and is only ever called <em>after</em> the transport codec (e.g.
    /// <c>LengthPrefixedTransportDecoder</c>) has confirmed that a complete frame is
    /// present and stripped the length prefix.  By the time bytes reach this codec
    /// there is nothing further to wait for, so an empty or truncated buffer means the
    /// assembled frame content is structurally corrupt — not that more data is pending.
    /// Returning <see cref="FrameDecodeResult.NeedsMoreData"/> would cause the caller
    /// to stall indefinitely waiting for data that will never arrive.
    /// </remarks>
    [TestMethod]
    public void Decode_EmptyBuffer_ReturnsInvalidFrameEncoding()
    {
        // TryRead returns false immediately — no kind or flags byte available.
        var (result, _) = CodecTestHelpers.Decode();

        Assert.AreEqual(FrameDecodeResult.InvalidFrameEncoding, result);
    }

    [TestMethod]
    public void Decode_OnlyOneByte_ReturnsInvalidFrameEncoding()
    {
        // A single byte cannot satisfy the minimum header of kind + flags.
        var (result, _) = CodecTestHelpers.Decode([0x01]);

        Assert.AreEqual(FrameDecodeResult.InvalidFrameEncoding, result);
    }

    // -------------------------------------------------------------------------
    // Structural failure — declared optional field data missing or truncated
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Decode_EventTypeFlagSet_ButNoFieldDataFollows_ReturnsInvalidFrameEncoding()
    {
        // kind=Event(0x01), flags=HasEventType(0x01) — but no 4-byte field data.
        var (result, _) = CodecTestHelpers.Decode([0x01, 0x01]);

        Assert.AreEqual(FrameDecodeResult.InvalidFrameEncoding, result);
    }

    [TestMethod]
    public void Decode_RequestIdFlagSet_ButOnlyThreeFieldBytes_ReturnsInvalidFrameEncoding()
    {
        // kind=Request(0x02), flags=HasRequestId(0x02), then only 3 bytes (need 4).
        var (result, _) = CodecTestHelpers.Decode([0x02, 0x02, 0x00, 0x00, 0x00]);

        Assert.AreEqual(FrameDecodeResult.InvalidFrameEncoding, result);
    }

    [TestMethod]
    public void Decode_RequestIdFlagSet_ButFieldDataInPartialSegment_ReturnsInvalidFrameEncoding()
    {
        // Header in segment 1 (2 bytes), only 3 of the required 4 requestId
        // bytes in segment 2.  The codec must detect the short read.
        var (result, _) = CodecTestHelpers.Decode(
            [0x02, 0x02],       // kind=Request, flags=HasRequestId
            [0x00, 0x00, 0x00]  // 3 bytes — one short of a complete uint32
        );

        Assert.AreEqual(FrameDecodeResult.InvalidFrameEncoding, result);
    }

    /// <summary>
    /// Verifies that an optional field whose four bytes straddle a buffer-segment
    /// boundary causes the decoder to return
    /// <see cref="FrameDecodeResult.InvalidFrameEncoding"/>.
    /// </summary>
    /// <remarks>
    /// This test documents an <b>implementation limitation</b>, not a protocol rule.
    /// The input bytes form a structurally valid frame (kind=Event,
    /// EventType=0x12345678); the decode fails solely because the first two bytes of
    /// the EventType field are at the tail of segment 1 and the remaining two bytes
    /// are at the head of segment 2.  <c>TryReadUInt32</c> calls
    /// <see cref="ICodecBufferReader.TryRead"/>, receives only the two remaining bytes
    /// of the current segment (fewer than the required four), and returns false.
    /// <br/><br/>
    /// In practice this situation cannot arise: the encoder writes the entire header
    /// — kind, flags, and all optional fields — as a single <c>stackalloc</c> span
    /// in one <c>writer.Write</c> call, so the first segment always contains the
    /// complete header.
    /// <br/><br/>
    /// <b>Future breakage risk:</b> if the encoder is ever changed to write optional
    /// fields incrementally (one <c>Write</c> call per field), the segment layout will
    /// change and this test may fail for frames the pipeline now decodes correctly.
    /// In that case, remove or update this test rather than treating it as a
    /// regression.
    /// </remarks>    [TestMethod]
    public void Decode_OptionalFieldSplitAcrossSegments_ReturnsInvalidFrameEncoding()
    {
        // This documents a known codec limitation: if a 4-byte optional field
        // straddles a buffer-segment boundary the codec cannot recover it, because
        // TryRead returns only the remaining bytes of the current segment.
        //
        // Segment 1: kind + flags + first 2 bytes of EventType → 4 bytes
        // Segment 2: remaining 2 bytes of EventType
        var (result, _) = CodecTestHelpers.Decode(
            [0x01, 0x01, 0x12, 0x34],  // kind=Event, flags=HasEventType, EventType[0..1]
            [0x56, 0x78]               // EventType[2..3] — in a separate segment
        );

        Assert.AreEqual(FrameDecodeResult.InvalidFrameEncoding, result);
    }

    // -------------------------------------------------------------------------
    // Structural failure — payload split across multiple segments
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Decode_PayloadSplitAcrossMultipleSegments_ReturnsInvalidFrameEncoding()
    {
        // The codec requires the entire payload to appear in a single contiguous
        // buffer segment.  Split payloads cannot be assembled.
        //
        // Segment 1: header (kind=Event, flags=None)
        // Segment 2: first payload byte
        // Segment 3: second payload byte
        var (result, _) = CodecTestHelpers.Decode(
            [0x01, 0x00],  // header
            [0xAA],        // payload byte 1
            [0xBB]         // payload byte 2 — separate segment
        );

        Assert.AreEqual(FrameDecodeResult.InvalidFrameEncoding, result);
    }

    // -------------------------------------------------------------------------
    // Fake-reader edge cases — Length out of valid range
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Decode_RemainingLengthIsNegative_ReturnsInvalidFrameEncoding()
    {
        // A real reader's Length can never be negative, but the guard must still
        // fire if a misbehaving implementation reports one.
        var reader = new FakeCodecBufferReader(
            claimedLength: -1,
            [0x01, 0x00]  // kind=Event, flags=None — enough to get past the header
        );

        var result = CreateCodec().Decode(reader, out _);

        Assert.AreEqual(FrameDecodeResult.InvalidFrameEncoding, result);
    }

    [TestMethod]
    public void Decode_RemainingLengthExceedsInt32Max_ReturnsInvalidFrameEncoding()
    {
        // A payload length exceeding int.MaxValue cannot be materialised and
        // must be rejected before any allocation is attempted.
        var reader = new FakeCodecBufferReader(
            claimedLength: (long)int.MaxValue + 1,
            [0x01, 0x00]  // kind=Event, flags=None
        );

        var result = CreateCodec().Decode(reader, out _);

        Assert.AreEqual(FrameDecodeResult.InvalidFrameEncoding, result);
    }

    // -------------------------------------------------------------------------
    // Success — minimal frame (kind + zero flags, no optional fields, no payload)
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Decode_MinimalFrame_ReturnsSuccess()
    {
        var (result, _) = CodecTestHelpers.Decode([0x01, 0x00]);

        Assert.AreEqual(FrameDecodeResult.Success, result);
    }

    [TestMethod]
    public void Decode_MinimalFrame_KindDecodedCorrectly()
    {
        var (_, frame) = CodecTestHelpers.Decode([0x01, 0x00]);

        Assert.AreEqual(NetworkFrameKind.Event, frame!.Kind);
    }

    [TestMethod]
    public void Decode_MinimalFrame_AllOptionalFieldsAreNull()
    {
        var (_, frame) = CodecTestHelpers.Decode([0x01, 0x00]);

        Assert.IsNull(frame!.EventType);
        Assert.IsNull(frame.RequestId);
        Assert.IsNull(frame.RequestType);
        Assert.IsNull(frame.ResponseType);
        Assert.IsNull(frame.StreamId);
        Assert.IsNull(frame.StreamType);
    }

    [TestMethod]
    public void Decode_MinimalFrame_PayloadIsEmpty()
    {
        var (_, frame) = CodecTestHelpers.Decode([0x01, 0x00]);

        Assert.AreEqual(0, frame!.Payload.Length,
            "A frame with no payload bytes must decode to an empty payload.");
    }

    // -------------------------------------------------------------------------
    // Success — each optional field decoded independently
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Decode_WithEventType_DecodesEventTypeCorrectly()
    {
        // kind=Event(0x01), flags=HasEventType(0x01), EventType=42
        var rawBytes = new byte[] { 0x01, 0x01, 0x00, 0x00, 0x00, 0x2A };
        var (result, frame) = CodecTestHelpers.Decode(rawBytes);

        Assert.AreEqual(FrameDecodeResult.Success, result);
        Assert.AreEqual(42u, frame!.EventType);
        Assert.IsNull(frame.RequestId);
    }

    [TestMethod]
    public void Decode_WithRequestId_DecodesRequestIdCorrectly()
    {
        // kind=Request(0x02), flags=HasRequestId(0x02), RequestId=1000
        var rawBytes = new byte[] { 0x02, 0x02, 0x00, 0x00, 0x03, 0xE8 };
        var (result, frame) = CodecTestHelpers.Decode(rawBytes);

        Assert.AreEqual(FrameDecodeResult.Success, result);
        Assert.AreEqual(1000u, frame!.RequestId);
        Assert.IsNull(frame.RequestType);
    }

    [TestMethod]
    public void Decode_WithRequestType_DecodesRequestTypeCorrectly()
    {
        // kind=Request(0x02), flags=HasRequestId|HasRequestType(0x06)
        var rawBytes = new byte[]
        {
            0x02, 0x06,                    // kind, flags
            0x00, 0x00, 0x00, 0x01,        // requestId = 1
            0x00, 0x00, 0x00, 0x07,        // requestType = 7
        };
        var (result, frame) = CodecTestHelpers.Decode(rawBytes);

        Assert.AreEqual(FrameDecodeResult.Success, result);
        Assert.AreEqual(1u, frame!.RequestId);
        Assert.AreEqual(7u, frame.RequestType);
    }

    [TestMethod]
    public void Decode_WithResponseType_DecodesResponseTypeCorrectly()
    {
        // kind=Response(0x03), flags=HasRequestId|HasResponseType(0x0A)
        var rawBytes = new byte[]
        {
            0x03, 0x0A,                    // kind, flags
            0x00, 0x00, 0x00, 0x05,        // requestId = 5
            0x00, 0x00, 0x00, 0x03,        // responseType = 3
        };
        var (result, frame) = CodecTestHelpers.Decode(rawBytes);

        Assert.AreEqual(FrameDecodeResult.Success, result);
        Assert.AreEqual(5u, frame!.RequestId);
        Assert.AreEqual(3u, frame.ResponseType);
        Assert.IsNull(frame.RequestType);
    }

    [TestMethod]
    public void Decode_WithStreamId_DecodesStreamIdCorrectly()
    {
        // kind=StreamData(0x11), flags=HasStreamId(0x10), StreamId=99
        var rawBytes = new byte[] { 0x11, 0x10, 0x00, 0x00, 0x00, 0x63 };
        var (result, frame) = CodecTestHelpers.Decode(rawBytes);

        Assert.AreEqual(FrameDecodeResult.Success, result);
        Assert.AreEqual(99u, frame!.StreamId);
        Assert.IsNull(frame.StreamType);
    }

    [TestMethod]
    public void Decode_WithStreamType_DecodesStreamTypeCorrectly()
    {
        // kind=StreamOpen(0x10), flags=HasStreamId|HasStreamType(0x30)
        var rawBytes = new byte[]
        {
            0x10, 0x30,                    // kind, flags
            0x00, 0x00, 0x00, 0x0A,        // streamId = 10
            0x00, 0x00, 0x00, 0x02,        // streamType = 2
        };
        var (result, frame) = CodecTestHelpers.Decode(rawBytes);

        Assert.AreEqual(FrameDecodeResult.Success, result);
        Assert.AreEqual(10u, frame!.StreamId);
        Assert.AreEqual(2u, frame.StreamType);
    }

    [TestMethod]
    public void Decode_AllOptionalFieldsPresent_DecodesAllFields()
    {
        // All six optional fields set, each with a distinct value.
        var rawBytes = new byte[]
        {
            0x01, 0x3F,                    // kind=Event, flags=all six bits
            0x00, 0x00, 0x00, 0x01,        // eventType    = 1
            0x00, 0x00, 0x00, 0x02,        // requestId    = 2
            0x00, 0x00, 0x00, 0x03,        // requestType  = 3
            0x00, 0x00, 0x00, 0x04,        // responseType = 4
            0x00, 0x00, 0x00, 0x05,        // streamId     = 5
            0x00, 0x00, 0x00, 0x06,        // streamType   = 6
        };
        var (result, frame) = CodecTestHelpers.Decode(rawBytes);

        Assert.AreEqual(FrameDecodeResult.Success, result);
        Assert.AreEqual(1u, frame!.EventType);
        Assert.AreEqual(2u, frame.RequestId);
        Assert.AreEqual(3u, frame.RequestType);
        Assert.AreEqual(4u, frame.ResponseType);
        Assert.AreEqual(5u, frame.StreamId);
        Assert.AreEqual(6u, frame.StreamType);
    }

    // -------------------------------------------------------------------------
    // Success — payload
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Decode_WithPayload_DecodesPayloadCorrectly()
    {
        // Header only (kind=Event, no flags), then 4 payload bytes.
        var rawBytes = new byte[] { 0x01, 0x00, 0xDE, 0xAD, 0xBE, 0xEF };
        var (result, frame) = CodecTestHelpers.Decode(rawBytes);

        Assert.AreEqual(FrameDecodeResult.Success, result);
        CollectionAssert.AreEqual(
            new byte[] { 0xDE, 0xAD, 0xBE, 0xEF },
            frame!.Payload.ToArray());
    }

    [TestMethod]
    public void Decode_HeaderAndPayloadInSeparateSegments_Succeeds()
    {
        // The complete payload in a single (second) segment is acceptable even
        // when the header is a separate (first) segment.
        var (result, frame) = CodecTestHelpers.Decode(
            [0x01, 0x00],          // header: kind=Event, flags=None
            [0xAA, 0xBB]           // complete 2-byte payload in one segment
        );

        Assert.AreEqual(FrameDecodeResult.Success, result);
        CollectionAssert.AreEqual(new byte[] { 0xAA, 0xBB }, frame!.Payload.ToArray());
    }

    // -------------------------------------------------------------------------
    // Success — big-endian field decoding
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Decode_EventType_InterpretedAsBigEndian()
    {
        // Bytes [0x12, 0x34, 0x56, 0x78] must decode to 0x12345678,
        // not 0x78563412 (little-endian) or any other interpretation.
        var rawBytes = new byte[] { 0x01, 0x01, 0x12, 0x34, 0x56, 0x78 };
        var (result, frame) = CodecTestHelpers.Decode(rawBytes);

        Assert.AreEqual(FrameDecodeResult.Success, result);
        Assert.AreEqual(0x12345678u, frame!.EventType,
            "Field bytes must be read as big-endian (MSB first).");
    }

    [TestMethod]
    public void Decode_FieldValue_MaxUInt32_DecodedCorrectly()
    {
        // StreamId = uint.MaxValue = 0xFFFFFFFF
        var rawBytes = new byte[] { 0x11, 0x10, 0xFF, 0xFF, 0xFF, 0xFF };
        var (result, frame) = CodecTestHelpers.Decode(rawBytes);

        Assert.AreEqual(FrameDecodeResult.Success, result);
        Assert.AreEqual(uint.MaxValue, frame!.StreamId);
    }

    // -------------------------------------------------------------------------
    // Success — all NetworkFrameKind values decoded correctly
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Decode_AllNetworkFrameKinds_KindDecodedCorrectly()
    {
        var cases = new (byte kindByte, NetworkFrameKind expectedKind)[]
        {
            (0x01, NetworkFrameKind.Event),
            (0x02, NetworkFrameKind.Request),
            (0x03, NetworkFrameKind.Response),
            (0x04, NetworkFrameKind.Error),
            (0x10, NetworkFrameKind.StreamOpen),
            (0x11, NetworkFrameKind.StreamData),
            (0x12, NetworkFrameKind.StreamClose),
            (0x13, NetworkFrameKind.StreamAbort),
        };

        foreach (var (kindByte, expectedKind) in cases)
        {
            var rawBytes = new byte[] { kindByte, 0x00 };
            var (result, frame) = CodecTestHelpers.Decode(rawBytes);

            Assert.AreEqual(FrameDecodeResult.Success, result,
                $"Decode of kind byte 0x{kindByte:X2} must succeed.");
            Assert.AreEqual(expectedKind, frame!.Kind,
                $"Kind byte 0x{kindByte:X2} must decode to {expectedKind}.");
        }
    }
}
