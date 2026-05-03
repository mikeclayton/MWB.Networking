using MWB.Networking.Layer1_Framing.Codec;
using MWB.Networking.Layer1_Framing.Codec.Frames;
using MWB.Networking.Layer1_Framing.Pipeline.UnitTests.Helpers;

namespace MWB.Networking.Layer1_Framing.Pipeline.UnitTests;

/// <summary>
/// End-to-end round-trip tests for <see cref="NetworkPipeline"/>.
///
/// Each test encodes a frame through the standard pipeline, feeds the resulting
/// wire bytes straight into <c>Decode</c>, and asserts that the decoded frame
/// is structurally identical to the original.  This verifies the complete
/// encode → decode path without depending on hand-crafted wire-byte constants.
/// </summary>
[TestClass]
public sealed class NetworkPipelineRoundTripTests
{
    // -------------------------------------------------------------------------
    // Shared helper
    // -------------------------------------------------------------------------

    /// <summary>
    /// Encodes <paramref name="frame"/> and immediately decodes the result,
    /// returning the decoded frame.  Asserts that the decode reports Success.
    /// </summary>
    private static NetworkFrame RoundTrip(NetworkFrame frame)
    {
        var pipeline = PipelineTestHelpers.CreateStandardPipeline();
        var wireBytes = PipelineTestHelpers.ToBytes(pipeline.Encode(frame));
        var seq = PipelineTestHelpers.ToSequence(wireBytes);

        var result = pipeline.Decode(ref seq, out var decoded);

        Assert.AreEqual(FrameDecodeResult.Success, result,
            "Round-trip decode must return Success.");
        Assert.IsNotNull(decoded,
            "Round-trip decode must produce a non-null frame.");

        return decoded;
    }

    // -------------------------------------------------------------------------
    // Frame-kind coverage
    // -------------------------------------------------------------------------

    [TestMethod]
    public void RoundTrip_EventFrame_RestoresAllFields()
    {
        var original = NetworkFrames.Event(eventType: 42u, payload: new byte[] { 0x01, 0x02 });
        PipelineTestHelpers.AssertFramesEqual(original, RoundTrip(original));
    }

    [TestMethod]
    public void RoundTrip_EventFrame_WithNoEventType_RestoresAllFields()
    {
        var original = NetworkFrames.Event(null);
        PipelineTestHelpers.AssertFramesEqual(original, RoundTrip(original));
    }

    [TestMethod]
    public void RoundTrip_RequestFrame_RestoresAllFields()
    {
        var original = NetworkFrames.Request(
            requestId: 7u,
            requestType: 3u,
            payload: new byte[] { 0xAA, 0xBB, 0xCC });
        PipelineTestHelpers.AssertFramesEqual(original, RoundTrip(original));
    }

    [TestMethod]
    public void RoundTrip_ResponseFrame_RestoresAllFields()
    {
        var original = NetworkFrames.Response(
            requestId: 99u,
            responseType: 2u,
            payload: new byte[] { 0xDE, 0xAD });
        PipelineTestHelpers.AssertFramesEqual(original, RoundTrip(original));
    }

    [TestMethod]
    public void RoundTrip_StreamOpenFrame_RestoresAllFields()
    {
        var original = NetworkFrames.StreamOpen(
            streamId: 5u,
            streamType: 1u,
            requestId: 10u,
            metadata: new byte[] { 0x10, 0x20 });
        PipelineTestHelpers.AssertFramesEqual(original, RoundTrip(original));
    }

    [TestMethod]
    public void RoundTrip_StreamDataFrame_RestoresAllFields()
    {
        var original = NetworkFrames.StreamData(streamId: 3u, payload: new byte[] { 0xFF, 0xFE, 0xFD });
        PipelineTestHelpers.AssertFramesEqual(original, RoundTrip(original));
    }

    [TestMethod]
    public void RoundTrip_StreamCloseFrame_RestoresAllFields()
    {
        var original = NetworkFrames.StreamClose(streamId: 8u);
        PipelineTestHelpers.AssertFramesEqual(original, RoundTrip(original));
    }

    // -------------------------------------------------------------------------
    // Payload edge cases
    // -------------------------------------------------------------------------

    [TestMethod]
    public void RoundTrip_EmptyPayload_RestoresFrame()
    {
        var original = NetworkFrames.Event(eventType: 1u);
        PipelineTestHelpers.AssertFramesEqual(original, RoundTrip(original));
    }

    [TestMethod]
    public void RoundTrip_LargePayload_RestoresAllBytes()
    {
        // 4 096-byte payload exercises multi-segment transport handling.
        var payload = Enumerable.Range(0, 4096).Select(i => (byte)(i % 256)).ToArray();
        var original = NetworkFrames.Event(eventType: null, payload: payload);
        PipelineTestHelpers.AssertFramesEqual(original, RoundTrip(original));
    }

    // -------------------------------------------------------------------------
    // Multi-frame sequential decode
    // -------------------------------------------------------------------------

    [TestMethod]
    public void RoundTrip_MultipleFrames_DecodesInOrder()
    {
        // Encode three distinct frames and concatenate their wire bytes.
        // A sequential series of Decode calls must reconstruct each frame in
        // the original order and leave the sequence empty afterwards.
        var pipeline = PipelineTestHelpers.CreateStandardPipeline();

        var frames = new[]
        {
            NetworkFrames.Event(eventType: 1u),
            NetworkFrames.Request(requestId: 2u, payload: new byte[] { 0x0A, 0x0B }),
            NetworkFrames.StreamData(streamId: 3u, payload: new byte[] { 0xCA, 0xFE }),
        };

        var wireBytes = frames
            .SelectMany(f => PipelineTestHelpers.ToBytes(pipeline.Encode(f)))
            .ToArray();

        var seq = PipelineTestHelpers.ToSequence(wireBytes);

        for (var i = 0; i < frames.Length; i++)
        {
            var result = pipeline.Decode(ref seq, out var decoded);
            Assert.AreEqual(FrameDecodeResult.Success, result, $"Frame {i}: expected Success");
            Assert.IsNotNull(decoded, $"Frame {i}: decoded frame must not be null");
            PipelineTestHelpers.AssertFramesEqual(frames[i], decoded, $"Frame {i}");
        }

        Assert.AreEqual(0L, seq.Length, "Sequence must be empty after all frames are consumed.");
    }
}
