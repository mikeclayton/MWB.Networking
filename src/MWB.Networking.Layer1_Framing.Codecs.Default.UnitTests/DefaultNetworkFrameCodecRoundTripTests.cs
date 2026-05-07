using MWB.Networking.Layer1_Framing.Codec;
using MWB.Networking.Layer1_Framing.Codec.Buffer;
using MWB.Networking.Layer1_Framing.Codec.Frames;
using MWB.Networking.Layer1_Framing.Codecs.Default.Network;
using MWB.Networking.Layer1_Framing.Codecs.Default.UnitTests.Helpers;

namespace MWB.Networking.Layer1_Framing.Codecs.Default.UnitTests;

/// <summary>
/// End-to-end round-trip tests for <see cref="DefaultNetworkFrameCodec"/>.
///
/// Each test encodes a <see cref="NetworkFrame"/>, pipes the resulting bytes
/// straight back into the decoder, and asserts that every field of the
/// recovered frame is identical to the original.  This gives high confidence
/// that the encode and decode paths are consistent without depending on
/// knowledge of the internal wire format.
/// </summary>
[TestClass]
public sealed class DefaultNetworkFrameCodecRoundTripTests
{
    public TestContext TestContext { get; set; } = null!;

    // -------------------------------------------------------------------------
    // Round-trip helper
    // -------------------------------------------------------------------------

    /// <summary>
    /// Encodes <paramref name="original"/>, feeds the bytes into a fresh
    /// decoder, and returns the recovered frame.  Asserts that the decode
    /// succeeds.
    /// </summary>
    private static NetworkFrame RoundTrip(NetworkFrame original)
    {
        // Encode
        var encodedBytes = CodecTestHelpers.Encode(original);

        // Decode — feed the encoded bytes as a single buffer segment, which is
        // what the pipeline does after the transport layer strips the length
        // prefix.
        var inputBuffer = CodecTestHelpers.CreateInputBuffer(encodedBytes);
        var codec = new DefaultNetworkFrameCodec();
        var result = codec.Decode(inputBuffer.Reader, out var recovered);

        Assert.AreEqual(FrameDecodeResult.Success, result,
            "Round-trip decode must return Success for a freshly encoded frame.");

        return recovered;
    }

    // -------------------------------------------------------------------------
    // Event frames
    // -------------------------------------------------------------------------

    [TestMethod]
    public void RoundTrip_EventFrame_NoOptionalFields_DecodesIdentically()
    {
        var frame = NetworkFrames.Event(eventType: null);
        CodecTestHelpers.AssertFramesEqual(frame, RoundTrip(frame));
    }

    [TestMethod]
    public void RoundTrip_EventFrame_WithEventType_DecodesIdentically()
    {
        var frame = NetworkFrames.Event(eventType: 0xCAFEBABE);
        CodecTestHelpers.AssertFramesEqual(frame, RoundTrip(frame));
    }

    [TestMethod]
    public void RoundTrip_EventFrame_WithEventTypeAndPayload_DecodesIdentically()
    {
        var frame = NetworkFrames.Event(
            eventType: 7,
            payload: new byte[] { 0x01, 0x02, 0x03, 0x04 });

        CodecTestHelpers.AssertFramesEqual(frame, RoundTrip(frame));
    }

    // -------------------------------------------------------------------------
    // Request frames
    // -------------------------------------------------------------------------

    [TestMethod]
    public void RoundTrip_RequestFrame_WithRequestId_DecodesIdentically()
    {
        var frame = NetworkFrames.Request(requestId: 42);
        CodecTestHelpers.AssertFramesEqual(frame, RoundTrip(frame));
    }

    [TestMethod]
    public void RoundTrip_RequestFrame_WithRequestIdAndRequestType_DecodesIdentically()
    {
        var frame = NetworkFrames.Request(requestId: 100, requestType: 7);
        CodecTestHelpers.AssertFramesEqual(frame, RoundTrip(frame));
    }

    [TestMethod]
    public void RoundTrip_RequestFrame_WithRequestIdAndPayload_DecodesIdentically()
    {
        var frame = NetworkFrames.Request(
            requestId: 1,
            payload: new byte[] { 0xAA, 0xBB, 0xCC });

        CodecTestHelpers.AssertFramesEqual(frame, RoundTrip(frame));
    }

    // -------------------------------------------------------------------------
    // Response frames
    // -------------------------------------------------------------------------

    [TestMethod]
    public void RoundTrip_ResponseFrame_WithRequestIdAndResponseType_DecodesIdentically()
    {
        var frame = NetworkFrames.Response(requestId: 5, responseType: 3);
        CodecTestHelpers.AssertFramesEqual(frame, RoundTrip(frame));
    }

    [TestMethod]
    public void RoundTrip_ResponseFrame_NoResponseType_DecodesIdentically()
    {
        var frame = NetworkFrames.Response(requestId: 5, responseType: null);
        CodecTestHelpers.AssertFramesEqual(frame, RoundTrip(frame));
    }

    // -------------------------------------------------------------------------
    // Stream frames
    // -------------------------------------------------------------------------

    [TestMethod]
    public void RoundTrip_StreamOpenFrame_WithStreamId_DecodesIdentically()
    {
        var frame = NetworkFrames.StreamOpen(streamId: 10);
        CodecTestHelpers.AssertFramesEqual(frame, RoundTrip(frame));
    }

    [TestMethod]
    public void RoundTrip_StreamOpenFrame_WithAllFields_DecodesIdentically()
    {
        var frame = NetworkFrames.StreamOpen(
            streamId: 10,
            streamType: 2,
            requestId: 99,
            metadata: new byte[] { 0xFF, 0xFE });

        CodecTestHelpers.AssertFramesEqual(frame, RoundTrip(frame));
    }

    [TestMethod]
    public void RoundTrip_StreamDataFrame_WithPayload_DecodesIdentically()
    {
        var frame = NetworkFrames.StreamData(
            streamId: 7,
            payload: new byte[] { 0x10, 0x20, 0x30 });

        CodecTestHelpers.AssertFramesEqual(frame, RoundTrip(frame));
    }

    [TestMethod]
    public void RoundTrip_StreamCloseFrame_DecodesIdentically()
    {
        var frame = NetworkFrames.StreamClose(streamId: 3);
        CodecTestHelpers.AssertFramesEqual(frame, RoundTrip(frame));
    }

    [TestMethod]
    public void RoundTrip_StreamAbortFrame_DecodesIdentically()
    {
        // NetworkFrames has no StreamAbort factory; construct directly.
        var frame = NetworkFrame.CreateRaw(
            NetworkFrameKind.StreamAbort,
            eventType: null, requestId: null, requestType: null,
            responseType: null, streamId: 99, streamType: null,
            payload: default);

        CodecTestHelpers.AssertFramesEqual(frame, RoundTrip(frame));
    }

    [TestMethod]
    public void RoundTrip_ErrorFrame_WithPayload_DecodesIdentically()
    {
        // NetworkFrames has no Error factory; construct directly.
        var frame = NetworkFrame.CreateRaw(
            NetworkFrameKind.Error,
            eventType: null, requestId: null, requestType: null,
            responseType: null, streamId: null, streamType: null,
            payload: new byte[] { 0xFF, 0xFE, 0xFD });

        CodecTestHelpers.AssertFramesEqual(frame, RoundTrip(frame));
    }

    // -------------------------------------------------------------------------
    // All optional fields + payload
    // -------------------------------------------------------------------------

    [TestMethod]
    public void RoundTrip_AllOptionalFieldsPresent_NoPayload_DecodesIdentically()
    {
        var frame = NetworkFrame.CreateRaw(
            NetworkFrameKind.Event,
            eventType: 1, requestId: 2, requestType: 3,
            responseType: 4, streamId: 5, streamType: 6,
            payload: default);

        CodecTestHelpers.AssertFramesEqual(frame, RoundTrip(frame));
    }

    [TestMethod]
    public void RoundTrip_AllOptionalFieldsPresent_WithPayload_DecodesIdentically()
    {
        var frame = NetworkFrame.CreateRaw(
            NetworkFrameKind.Event,
            eventType: 0x11111111u, requestId: 0x22222222u, requestType: 0x33333333u,
            responseType: 0x44444444u, streamId: 0x55555555u, streamType: 0x66666666u,
            payload: new byte[] { 0xAB, 0xCD, 0xEF });

        CodecTestHelpers.AssertFramesEqual(frame, RoundTrip(frame));
    }

    // -------------------------------------------------------------------------
    // Payload edge cases
    // -------------------------------------------------------------------------

    [TestMethod]
    public void RoundTrip_LargePayload_DecodesIdentically()
    {
        var payload = new byte[512 * 1024]; // 512 KB
        new Random(99).NextBytes(payload);
        var frame = NetworkFrames.StreamData(streamId: 1, payload: payload);

        CodecTestHelpers.AssertFramesEqual(frame, RoundTrip(frame));
    }

    [TestMethod]
    public void RoundTrip_AllByteValuesInPayload_DecodesIdentically()
    {
        // All 256 possible byte values in a single payload — no byte value
        // should be treated specially by the codec.
        var payload = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
        var frame = NetworkFrames.Event(eventType: null, payload: payload);

        CodecTestHelpers.AssertFramesEqual(frame, RoundTrip(frame));
    }

    // -------------------------------------------------------------------------
    // Field value edge cases
    // -------------------------------------------------------------------------

    [TestMethod]
    public void RoundTrip_MaxUInt32FieldValues_DecodesIdentically()
    {
        var frame = NetworkFrame.CreateRaw(
            NetworkFrameKind.Event,
            eventType:    uint.MaxValue,
            requestId:    uint.MaxValue,
            requestType:  uint.MaxValue,
            responseType: uint.MaxValue,
            streamId:     uint.MaxValue,
            streamType:   uint.MaxValue,
            payload: default);

        CodecTestHelpers.AssertFramesEqual(frame, RoundTrip(frame));
    }

    [TestMethod]
    public void RoundTrip_ZeroUInt32FieldValues_DecodesIdentically()
    {
        var frame = NetworkFrame.CreateRaw(
            NetworkFrameKind.Event,
            eventType:    0u,
            requestId:    0u,
            requestType:  0u,
            responseType: 0u,
            streamId:     0u,
            streamType:   0u,
            payload: default);

        CodecTestHelpers.AssertFramesEqual(frame, RoundTrip(frame));
    }

    // -------------------------------------------------------------------------
    // Multiple frames encoded and decoded sequentially
    // -------------------------------------------------------------------------

    [TestMethod]
    public void RoundTrip_MultipleFramesEncoded_AllDecodeInOrder()
    {
        var codec = new DefaultNetworkFrameCodec();

        var frames = new[]
        {
            NetworkFrames.Event(eventType: 1),
            NetworkFrames.Request(requestId: 42, requestType: 7),
            NetworkFrames.Response(requestId: 42, responseType: 3,
                payload: new byte[] { 0xAA, 0xBB }),
            NetworkFrames.StreamOpen(streamId: 10, streamType: 2, requestId: 42),
            NetworkFrames.StreamData(streamId: 10,
                payload: new byte[] { 0x01, 0x02, 0x03 }),
            NetworkFrames.StreamClose(streamId: 10),
        };

        foreach (var (original, index) in frames.Select((f, i) => (f, i)))
        {
            var encoded = CodecTestHelpers.Encode(original);
            var inputBuffer = CodecTestHelpers.CreateInputBuffer(encoded);
            var result = codec.Decode(inputBuffer.Reader, out var recovered);

            Assert.AreEqual(FrameDecodeResult.Success, result,
                $"Frame {index} must decode successfully.");
            CodecTestHelpers.AssertFramesEqual(original, recovered,
                $"Frame {index}");
        }
    }
}
