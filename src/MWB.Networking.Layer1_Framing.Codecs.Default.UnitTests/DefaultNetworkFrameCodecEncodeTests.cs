using MWB.Networking.Layer1_Framing.Codec.Buffer;
using MWB.Networking.Layer1_Framing.Codec.Frames;
using MWB.Networking.Layer1_Framing.Codecs.Default.Network;
using MWB.Networking.Layer1_Framing.Codecs.Default.UnitTests.Helpers;

namespace MWB.Networking.Layer1_Framing.Codecs.Default.UnitTests;

/// <summary>
/// Isolated unit tests for the <em>encode</em> direction of
/// <see cref="DefaultNetworkFrameCodec"/>.
///
/// Tests verify:
/// <list type="bullet">
///   <item>The kind byte and flags byte are written at the correct offsets.</item>
///   <item>Each optional field sets the correct bit in the flags byte.</item>
///   <item>Optional field values are written in the correct order, big-endian.</item>
///   <item>The payload follows the header unchanged.</item>
///   <item>The complete encoded output matches independently hand-built bytes.</item>
/// </list>
/// </summary>
[TestClass]
public sealed class DefaultNetworkFrameCodecEncodeTests
{
    public TestContext TestContext { get; set; } = null!;

    private static DefaultNetworkFrameCodec CreateCodec() => new();

    /// <summary>
    /// Encodes <paramref name="frame"/> and returns the raw output bytes.
    /// </summary>
    private static byte[] Encode(NetworkFrame frame)
    {
        var outputBuffer = new CodecBuffer();
        CreateCodec().Encode(frame, outputBuffer.Writer);
        outputBuffer.Writer.Complete();
        return CodecTestHelpers.ReadAllOutput(outputBuffer);
    }

    // -------------------------------------------------------------------------
    // Construction
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Constructor_DefaultParameterlessCtor_DoesNotThrow()
    {
        _ = new DefaultNetworkFrameCodec();
    }

    // -------------------------------------------------------------------------
    // Minimal frame — kind + flags, no optional fields, no payload
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Encode_MinimalFrame_OutputIsTwoBytes()
    {
        // A frame with no optional fields and an empty payload must produce
        // exactly 2 bytes: one for Kind and one for the (zero) Flags.
        var frame = NetworkFrames.Event(eventType: null);
        var output = Encode(frame);

        Assert.AreEqual(2, output.Length,
            "Minimal frame must produce exactly 2 bytes (kind + flags).");
    }

    [TestMethod]
    public void Encode_MinimalFrame_KindByteIsAtOffsetZero()
    {
        var frame = NetworkFrames.Event(eventType: null);
        var output = Encode(frame);

        Assert.AreEqual((byte)NetworkFrameKind.Event, output[0],
            "Kind byte must be the first byte of the encoded output.");
    }

    [TestMethod]
    public void Encode_MinimalFrame_FlagsByteIsZero()
    {
        var frame = NetworkFrames.Event(eventType: null);
        var output = Encode(frame);

        Assert.AreEqual(0x00, output[1],
            "Flags byte must be 0x00 when no optional fields are present.");
    }

    // -------------------------------------------------------------------------
    // Kind byte — all NetworkFrameKind values
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Encode_AllNetworkFrameKinds_KindByteMatchesExpected()
    {
        // Every NetworkFrameKind value must appear verbatim at offset 0.
        var cases = new (NetworkFrameKind kind, byte expected)[]
        {
            (NetworkFrameKind.Event,       0x01),
            (NetworkFrameKind.Request,     0x02),
            (NetworkFrameKind.Response,    0x03),
            (NetworkFrameKind.Error,       0x04),
            (NetworkFrameKind.StreamOpen,  0x10),
            (NetworkFrameKind.StreamData,  0x11),
            (NetworkFrameKind.StreamClose, 0x12),
            (NetworkFrameKind.StreamAbort, 0x13),
        };

        foreach (var (kind, expected) in cases)
        {
            var frame = new NetworkFrame(kind,
                eventType: null, requestId: null, requestType: null,
                responseType: null, streamId: null, streamType: null,
                payload: default);

            var output = Encode(frame);

            Assert.AreEqual(expected, output[0],
                $"Kind {kind} should encode to byte 0x{expected:X2} at offset 0.");
        }
    }

    // -------------------------------------------------------------------------
    // Flags byte — each optional field sets the correct bit
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Encode_WithEventTypePresent_EventTypeFlagIsSet()
    {
        var frame = NetworkFrames.Event(eventType: 1);
        var output = Encode(frame);

        Assert.AreEqual(CodecTestHelpers.FlagEventType, output[1],
            "HasEventType flag (bit 0 = 0x01) must be set.");
    }

    [TestMethod]
    public void Encode_WithRequestIdPresent_RequestIdFlagIsSet()
    {
        var frame = NetworkFrames.Request(requestId: 1);
        var output = Encode(frame);

        Assert.AreEqual(CodecTestHelpers.FlagRequestId, output[1],
            "HasRequestId flag (bit 1 = 0x02) must be set.");
    }

    [TestMethod]
    public void Encode_WithRequestTypePresent_RequestTypeFlagIsSet()
    {
        var frame = NetworkFrames.Request(requestId: 1, requestType: 7);
        var output = Encode(frame);

        // Both RequestId (0x02) and RequestType (0x04) are set.
        const byte expected = CodecTestHelpers.FlagRequestId | CodecTestHelpers.FlagRequestType;
        Assert.AreEqual(expected, output[1],
            "HasRequestId and HasRequestType flags must both be set.");
    }

    [TestMethod]
    public void Encode_WithResponseTypePresent_ResponseTypeFlagIsSet()
    {
        var frame = NetworkFrames.Response(requestId: 1, responseType: 5);
        var output = Encode(frame);

        // Both RequestId (0x02) and ResponseType (0x08) are set.
        const byte expected = CodecTestHelpers.FlagRequestId | CodecTestHelpers.FlagResponseType;
        Assert.AreEqual(expected, output[1],
            "HasRequestId and HasResponseType flags must both be set.");
    }

    [TestMethod]
    public void Encode_WithStreamIdPresent_StreamIdFlagIsSet()
    {
        var frame = NetworkFrames.StreamOpen(streamId: 10);
        var output = Encode(frame);

        Assert.AreEqual(CodecTestHelpers.FlagStreamId, output[1],
            "HasStreamId flag (bit 4 = 0x10) must be set.");
    }

    [TestMethod]
    public void Encode_WithStreamTypePresent_StreamTypeFlagIsSet()
    {
        var frame = NetworkFrames.StreamOpen(streamId: 10, streamType: 3);
        var output = Encode(frame);

        const byte expected = CodecTestHelpers.FlagStreamId | CodecTestHelpers.FlagStreamType;
        Assert.AreEqual(expected, output[1],
            "HasStreamId and HasStreamType flags must both be set.");
    }

    [TestMethod]
    public void Encode_AllOptionalFieldsPresent_AllFlagsSetInFlagsByte()
    {
        var frame = new NetworkFrame(
            NetworkFrameKind.Event,
            eventType: 1, requestId: 2, requestType: 3,
            responseType: 4, streamId: 5, streamType: 6,
            payload: default);

        var output = Encode(frame);

        const byte allFlags =
            CodecTestHelpers.FlagEventType    |   // 0x01
            CodecTestHelpers.FlagRequestId    |   // 0x02
            CodecTestHelpers.FlagRequestType  |   // 0x04
            CodecTestHelpers.FlagResponseType |   // 0x08
            CodecTestHelpers.FlagStreamId     |   // 0x10
            CodecTestHelpers.FlagStreamType;      // 0x20
        // = 0x3F

        Assert.AreEqual(allFlags, output[1],
            "All six flag bits must be set (0x3F) when all optional fields are present.");
    }

    // -------------------------------------------------------------------------
    // Optional fields — big-endian encoding and position
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Encode_EventType_WrittenBigEndianAtOffset2()
    {
        // 0x12345678 in big-endian is [0x12, 0x34, 0x56, 0x78].
        // If it were little-endian it would be [0x78, 0x56, 0x34, 0x12].
        var frame = NetworkFrames.Event(eventType: 0x12345678);
        var output = Encode(frame);

        Assert.AreEqual(6, output.Length, "kind(1) + flags(1) + eventType(4) = 6 bytes.");
        CollectionAssert.AreEqual(
            new byte[] { 0x12, 0x34, 0x56, 0x78 },
            output[2..6],
            "EventType must be written big-endian (MSB first) starting at offset 2.");
    }

    [TestMethod]
    public void Encode_RequestId_WrittenBigEndianAfterFlagsAtOffset2()
    {
        var frame = NetworkFrames.Request(requestId: 0xDEADBEEF);
        var output = Encode(frame);

        // kind(1) + flags(1) + requestId(4) = 6 bytes
        Assert.AreEqual(6, output.Length);
        CollectionAssert.AreEqual(
            new byte[] { 0xDE, 0xAD, 0xBE, 0xEF },
            output[2..6],
            "RequestId must be written big-endian starting at offset 2.");
    }

    [TestMethod]
    public void Encode_AllOptionalFields_WrittenInExpectedOrder()
    {
        // Assign each field a distinct small value so its 4-byte encoding is
        // unambiguous: field N has value N, encoded as [0x00, 0x00, 0x00, N].
        var frame = new NetworkFrame(
            NetworkFrameKind.Event,
            eventType:    0x00000001u,
            requestId:    0x00000002u,
            requestType:  0x00000003u,
            responseType: 0x00000004u,
            streamId:     0x00000005u,
            streamType:   0x00000006u,
            payload: default);

        var output = Encode(frame);

        // Layout: kind(1) + flags(1) + 6 fields × 4 bytes = 26 bytes total.
        Assert.AreEqual(26, output.Length,
            "2 header bytes + 6 optional fields × 4 bytes = 26 bytes.");

        CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x00, 0x01 }, output[2..6],
            "EventType must be first optional field (offset 2–5).");
        CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x00, 0x02 }, output[6..10],
            "RequestId must follow EventType (offset 6–9).");
        CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x00, 0x03 }, output[10..14],
            "RequestType must follow RequestId (offset 10–13).");
        CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x00, 0x04 }, output[14..18],
            "ResponseType must follow RequestType (offset 14–17).");
        CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x00, 0x05 }, output[18..22],
            "StreamId must follow ResponseType (offset 18–21).");
        CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x00, 0x06 }, output[22..26],
            "StreamType must be last optional field (offset 22–25).");
    }

    // -------------------------------------------------------------------------
    // Payload
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Encode_EmptyPayload_NoPayloadBytesWritten()
    {
        // An empty payload must not add any bytes beyond the header.
        var frame = NetworkFrames.Event(eventType: null, payload: default);
        var output = Encode(frame);

        Assert.AreEqual(2, output.Length,
            "No payload bytes should appear when the payload is empty.");
    }

    [TestMethod]
    public void Encode_NonEmptyPayload_PayloadFollowsHeaderUnchanged()
    {
        var payload = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
        var frame = NetworkFrames.Event(eventType: null, payload: payload);
        var output = Encode(frame);

        // 2-byte header + 5-byte payload = 7 bytes
        Assert.AreEqual(7, output.Length);
        CollectionAssert.AreEqual(payload, output[2..],
            "Payload must follow the header byte-for-byte.");
    }

    [TestMethod]
    public void Encode_LargePayload_AllBytesPreserved()
    {
        var payload = new byte[64 * 1024];
        new Random(42).NextBytes(payload);
        var frame = NetworkFrames.StreamData(streamId: 1, payload: payload);
        var output = Encode(frame);

        // 2-byte header + 4-byte streamId + 64KB payload
        Assert.AreEqual(2 + 4 + payload.Length, output.Length);
        CollectionAssert.AreEqual(payload, output[6..],
            "All payload bytes must be preserved exactly for large payloads.");
    }

    // -------------------------------------------------------------------------
    // Complete frame structure — verified against hand-built expected bytes
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Encode_RequestFrameWithFieldsAndPayload_MatchesManuallyBuiltBytes()
    {
        // Request frame: requestId=0x00001234, requestType=0x00000007,
        // payload=[0xDE, 0xAD, 0xBE, 0xEF].
        //
        // kind    = Request = 0x02
        // flags   = HasRequestId | HasRequestType = 0x02 | 0x04 = 0x06
        // requestId   = 0x00001234 → [0x00, 0x00, 0x12, 0x34]
        // requestType = 0x00000007 → [0x00, 0x00, 0x00, 0x07]
        // payload     = [0xDE, 0xAD, 0xBE, 0xEF]
        var frame = NetworkFrames.Request(
            requestId: 0x00001234,
            requestType: 0x00000007,
            payload: new byte[] { 0xDE, 0xAD, 0xBE, 0xEF });

        var output = Encode(frame);

        var expected = new byte[]
        {
            0x02, 0x06,                    // kind, flags
            0x00, 0x00, 0x12, 0x34,        // requestId
            0x00, 0x00, 0x00, 0x07,        // requestType
            0xDE, 0xAD, 0xBE, 0xEF,        // payload
        };

        CollectionAssert.AreEqual(expected, output,
            "Complete encoded output must match the manually assembled expected bytes.");
    }

    [TestMethod]
    public void Encode_StreamOpenFrame_MatchesManuallyBuiltBytes()
    {
        // StreamOpen: streamId=0x00000064 (100), requestId=0x00000001,
        // no payload.
        //
        // kind  = StreamOpen = 0x10
        // flags = HasRequestId | HasStreamId = 0x02 | 0x10 = 0x12
        // requestId = 1 → [0x00, 0x00, 0x00, 0x01]
        // streamId  = 100 → [0x00, 0x00, 0x00, 0x64]
        var frame = NetworkFrames.StreamOpen(streamId: 100, requestId: 1);

        var output = Encode(frame);

        var expected = new byte[]
        {
            0x10, 0x12,                    // kind, flags
            0x00, 0x00, 0x00, 0x01,        // requestId (written before streamId — bit 1 < bit 4)
            0x00, 0x00, 0x00, 0x64,        // streamId
        };

        CollectionAssert.AreEqual(expected, output);
    }
}
