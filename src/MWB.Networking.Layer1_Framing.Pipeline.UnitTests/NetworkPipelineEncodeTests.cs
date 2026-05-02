using MWB.Networking.Layer1_Framing.Codec.Frames;
using MWB.Networking.Layer1_Framing.Pipeline.UnitTests.Helpers;

namespace MWB.Networking.Layer1_Framing.Pipeline.UnitTests;

/// <summary>
/// Tests for <see cref="NetworkPipeline.Encode"/>.
///
/// The standard test pipeline is:
///   <see cref="Codecs.Default.Network.DefaultNetworkFrameCodec"/>
///     → [<see cref="Codecs.Reverse.ReverseFrameCodec"/>]
///     → <see cref="Codecs.LengthPrefixed.Transport.LengthPrefixedTransportCodec"/>
///
/// Wire-byte expectations are derived from the codec specifications and verified
/// by two complementary tests: one for a single reverse codec, and one that
/// shows two reverse codecs produce the same output as zero codecs — proving
/// the pipeline applies frame codecs in the correct forward order.
/// </summary>
[TestClass]
public sealed class NetworkPipelineEncodeTests
{
    // -------------------------------------------------------------------------
    // Wire-byte correctness — standard pipeline
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Encode_MinimalEventFrame_WireBytesAreCorrect()
    {
        // NetworkFrames.Event(null):
        //   NetworkCodec produces header = [0x01 (Event), 0x00 (no flags)]
        //   ReverseCodec reverses:         [0x00, 0x01]
        //   LengthPrefix:  length=2        [0x00, 0x00, 0x00, 0x02, 0x00, 0x01]
        var pipeline = PipelineTestHelpers.CreateStandardPipeline();

        var wireBytes = PipelineTestHelpers.ToBytes(
            pipeline.Encode(NetworkFrames.Event(null)));

        CollectionAssert.AreEqual(
            new byte[] { 0x00, 0x00, 0x00, 0x02, 0x00, 0x01 },
            wireBytes);
    }

    [TestMethod]
    public void Encode_EventWithEventType_WireBytesAreCorrect()
    {
        // NetworkFrames.Event(eventType: 1):
        //   NetworkCodec header = [0x01 (Event), 0x01 (HasEventType), 0x00,0x00,0x00,0x01]
        //   ReverseCodec reverses: [0x01, 0x00,0x00,0x00, 0x01, 0x01]
        //   LengthPrefix: length=6 [0x00,0x00,0x00,0x06, 0x01,0x00,0x00,0x00,0x01,0x01]
        var pipeline = PipelineTestHelpers.CreateStandardPipeline();

        var wireBytes = PipelineTestHelpers.ToBytes(
            pipeline.Encode(NetworkFrames.Event(eventType: 1)));

        CollectionAssert.AreEqual(
            new byte[] { 0x00, 0x00, 0x00, 0x06,
                         0x01, 0x00, 0x00, 0x00, 0x01, 0x01 },
            wireBytes);
    }

    [TestMethod]
    public void Encode_RequestWithPayload_WireBytesAreCorrect()
    {
        // NetworkFrames.Request(requestId: 1, payload: [0xAA, 0xBB]):
        //   NetworkCodec:
        //     seg[0] = [0x02(Request), 0x02(HasRequestId), 0x00,0x00,0x00,0x01]
        //     seg[1] = [0xAA, 0xBB]
        //   ReverseCodec:
        //     seg[0] = reverse(seg[1]) = [0xBB, 0xAA]
        //     seg[1] = reverse(seg[0]) = [0x01, 0x00,0x00,0x00, 0x02, 0x02]
        //   LengthPrefix: total=8
        //     [0x00,0x00,0x00,0x08, 0xBB,0xAA, 0x01,0x00,0x00,0x00, 0x02,0x02]
        var pipeline = PipelineTestHelpers.CreateStandardPipeline();

        var wireBytes = PipelineTestHelpers.ToBytes(
            pipeline.Encode(NetworkFrames.Request(
                requestId: 1,
                payload: new byte[] { 0xAA, 0xBB })));

        CollectionAssert.AreEqual(
            new byte[] { 0x00, 0x00, 0x00, 0x08,
                         0xBB, 0xAA,
                         0x01, 0x00, 0x00, 0x00, 0x02, 0x02 },
            wireBytes);
    }

    // -------------------------------------------------------------------------
    // Codec ordering proof
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Encode_ZeroFrameCodecs_WireBytesMatchDirectNetworkPlusTrasport()
    {
        // Without any intermediate frame codec the transport receives the
        // NetworkCodec output verbatim.
        // NetworkFrames.Request(requestId: 1, payload: [0xAA, 0xBB]):
        //   NetworkCodec:
        //     seg[0] = [0x02, 0x02, 0x00,0x00,0x00,0x01]
        //     seg[1] = [0xAA, 0xBB]
        //   LengthPrefix: total=8
        //     [0x00,0x00,0x00,0x08, 0x02,0x02,0x00,0x00,0x00,0x01, 0xAA,0xBB]
        var pipeline = PipelineTestHelpers.CreateZeroCodecPipeline();

        var wireBytes = PipelineTestHelpers.ToBytes(
            pipeline.Encode(NetworkFrames.Request(
                requestId: 1,
                payload: new byte[] { 0xAA, 0xBB })));

        CollectionAssert.AreEqual(
            new byte[] { 0x00, 0x00, 0x00, 0x08,
                         0x02, 0x02, 0x00, 0x00, 0x00, 0x01,
                         0xAA, 0xBB },
            wireBytes);
    }

    [TestMethod]
    public void Encode_TwoReverseCodecs_WireBytesMatchZeroCodecs()
    {
        // Reversing twice is the identity transform.  A pipeline with two
        // ReverseFrameCodecs must produce identical wire bytes to one with zero
        // frame codecs — proving the pipeline applies them in forward order and
        // that double application cancels out.
        var frame = NetworkFrames.Request(
            requestId: 1,
            payload: new byte[] { 0xAA, 0xBB });

        var zeroBytes  = PipelineTestHelpers.ToBytes(
            PipelineTestHelpers.CreateZeroCodecPipeline().Encode(frame));

        var doubleBytes = PipelineTestHelpers.ToBytes(
            PipelineTestHelpers.CreateDoubleReversePipeline().Encode(frame));

        CollectionAssert.AreEqual(zeroBytes, doubleBytes,
            "Two reverse codecs must produce the same wire bytes as zero codecs (identity).");
    }

    // -------------------------------------------------------------------------
    // Output structure
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Encode_AnyFrame_ByteSegmentsIsNotEmpty()
    {
        // Even for the smallest valid frame the transport must emit at least
        // the 4-byte length prefix.
        var pipeline = PipelineTestHelpers.CreateStandardPipeline();

        var result = pipeline.Encode(NetworkFrames.Event(null));

        Assert.IsTrue(result.Segments.Length > 0,
            "Encode must produce at least one ByteSegments entry.");
        Assert.IsTrue(PipelineTestHelpers.ToBytes(result).Length >= 4,
            "Encoded output must be at least 4 bytes (length prefix).");
    }
}
