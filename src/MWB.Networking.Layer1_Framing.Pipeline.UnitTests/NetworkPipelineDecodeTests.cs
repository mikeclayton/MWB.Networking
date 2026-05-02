using MWB.Networking.Layer1_Framing.Codec;
using MWB.Networking.Layer1_Framing.Codec.Exceptions;
using MWB.Networking.Layer1_Framing.Codec.Frames;
using MWB.Networking.Layer1_Framing.Pipeline.UnitTests.Helpers;
using System.Buffers;

namespace MWB.Networking.Layer1_Framing.Pipeline.UnitTests;

/// <summary>
/// Tests for <see cref="NetworkPipeline.Decode"/>.
///
/// Covers three result categories:
/// <list type="bullet">
///   <item><see cref="FrameDecodeResult.NeedsMoreData"/> — insufficient transport bytes.</item>
///   <item><see cref="FrameDecodeResult.Success"/> — a complete, valid frame.</item>
///   <item><see cref="FrameDecodeResult.InvalidFrameEncoding"/> — transport bytes present but
///         structurally invalid at the network-frame level.</item>
/// </list>
/// Also verifies atomicity: the transport sequence is advanced only on
/// <see cref="FrameDecodeResult.Success"/>, never on failure.
/// </summary>
[TestClass]
public sealed class NetworkPipelineDecodeTests
{
    // -------------------------------------------------------------------------
    // NeedsMoreData
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Decode_EmptySequence_ReturnsNeedsMoreData()
    {
        var pipeline = PipelineTestHelpers.CreateStandardPipeline();
        var seq = ReadOnlySequence<byte>.Empty;

        var result = pipeline.Decode(ref seq, out _);

        Assert.AreEqual(FrameDecodeResult.NeedsMoreData, result);
    }

    [TestMethod]
    public void Decode_IncompleteTransportFrame_ReturnsNeedsMoreData()
    {
        // Four bytes is a complete length prefix but no payload — the transport
        // decoder requires the full frame before it returns true.
        var pipeline = PipelineTestHelpers.CreateStandardPipeline();
        var seq = PipelineTestHelpers.ToSequence(
            new byte[] { 0x00, 0x00, 0x00, 0x10 }); // length=16, payload missing

        var result = pipeline.Decode(ref seq, out _);

        Assert.AreEqual(FrameDecodeResult.NeedsMoreData, result);
    }

    [TestMethod]
    public void Decode_EmptySequence_DoesNotAdvanceSequence()
    {
        var pipeline = PipelineTestHelpers.CreateStandardPipeline();
        var seq = ReadOnlySequence<byte>.Empty;

        _ = pipeline.Decode(ref seq, out _);

        Assert.AreEqual(0L, seq.Length, "Sequence must remain empty after NeedsMoreData.");
    }

    [TestMethod]
    public void Decode_IncompleteTransportFrame_DoesNotAdvanceSequence()
    {
        var pipeline = PipelineTestHelpers.CreateStandardPipeline();
        var original = new byte[] { 0x00, 0x00, 0x00, 0x10 };
        var seq = PipelineTestHelpers.ToSequence(original);

        _ = pipeline.Decode(ref seq, out _);

        Assert.AreEqual(original.Length, seq.Length,
            "Sequence must not be advanced when more data is needed.");
    }

    // -------------------------------------------------------------------------
    // Success
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Decode_ValidMinimalEventFrame_ReturnsSuccess()
    {
        // Wire bytes for Event(null) through the standard pipeline (pre-computed):
        //   [0x00,0x00,0x00,0x02, 0x00,0x01]
        var pipeline = PipelineTestHelpers.CreateStandardPipeline();
        var seq = PipelineTestHelpers.ToSequence(
            new byte[] { 0x00, 0x00, 0x00, 0x02, 0x00, 0x01 });

        var result = pipeline.Decode(ref seq, out _);

        Assert.AreEqual(FrameDecodeResult.Success, result);
    }

    [TestMethod]
    public void Decode_ValidMinimalEventFrame_FrameFieldsAreCorrect()
    {
        var pipeline = PipelineTestHelpers.CreateStandardPipeline();
        var seq = PipelineTestHelpers.ToSequence(
            new byte[] { 0x00, 0x00, 0x00, 0x02, 0x00, 0x01 });

        pipeline.Decode(ref seq, out var frame);

        Assert.IsNotNull(frame);
        Assert.AreEqual(NetworkFrameKind.Event, frame.Kind);
        Assert.IsNull(frame.EventType);
        Assert.AreEqual(0, frame.Payload.Length);
    }

    [TestMethod]
    public void Decode_ValidRequestFrameWithPayload_FrameFieldsAreCorrect()
    {
        // Request(requestId: 1, payload: [0xAA, 0xBB]) — standard pipeline wire bytes:
        //   [0x00,0x00,0x00,0x08, 0xBB,0xAA, 0x01,0x00,0x00,0x00, 0x02,0x02]
        var pipeline = PipelineTestHelpers.CreateStandardPipeline();
        var seq = PipelineTestHelpers.ToSequence(
            new byte[] { 0x00, 0x00, 0x00, 0x08,
                         0xBB, 0xAA,
                         0x01, 0x00, 0x00, 0x00, 0x02, 0x02 });

        var result = pipeline.Decode(ref seq, out var frame);

        Assert.AreEqual(FrameDecodeResult.Success, result);
        Assert.IsNotNull(frame);
        Assert.AreEqual(NetworkFrameKind.Request, frame.Kind);
        Assert.AreEqual(1u, frame.RequestId);
        CollectionAssert.AreEqual(new byte[] { 0xAA, 0xBB }, frame.Payload.ToArray());
    }

    [TestMethod]
    public void Decode_ValidFrame_SequenceIsAdvancedByFullFrameLength()
    {
        // A successful decode must consume exactly the bytes for one frame,
        // leaving any trailing bytes intact.
        // Frame 1: Event(null) → [0x00,0x00,0x00,0x02, 0x00,0x01]
        // Frame 2: [0xAB, 0xCD] (arbitrary trailing bytes, not a complete frame)
        var frame1Bytes = new byte[] { 0x00, 0x00, 0x00, 0x02, 0x00, 0x01 };
        var trailingBytes = new byte[] { 0xAB, 0xCD };
        var combined = frame1Bytes.Concat(trailingBytes).ToArray();

        var pipeline = PipelineTestHelpers.CreateStandardPipeline();
        var seq = PipelineTestHelpers.ToSequence(combined);

        _ = pipeline.Decode(ref seq, out _);

        Assert.AreEqual(trailingBytes.Length, seq.Length,
            "After a successful decode the sequence must be advanced past the consumed frame.");
    }

    // -------------------------------------------------------------------------
    // InvalidFrameEncoding
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Decode_InvalidFrameContent_ReturnsInvalidFrameEncoding()
    {
        // Feed a 1-byte transport payload (length=1, payload=[0x42]).
        // After the transport decode succeeds, the ReverseFrameCodec passes the
        // byte through, and DefaultNetworkFrameCodec receives only 1 byte —
        // fewer than the 2 bytes required for Kind+Flags — so it returns
        // InvalidFrameEncoding.
        var pipeline = PipelineTestHelpers.CreateStandardPipeline();
        var seq = PipelineTestHelpers.ToSequence(
            new byte[] { 0x00, 0x00, 0x00, 0x01, 0x42 });

        var result = pipeline.Decode(ref seq, out _);

        Assert.AreEqual(FrameDecodeResult.InvalidFrameEncoding, result);
    }

    [TestMethod]
    public void Decode_FrameCodecReturnsInvalidEncoding_ReturnsInvalidFrameEncoding()
    {
        // AlwaysFailFrameCodec.Decode always returns InvalidFrameEncoding.
        // The pipeline must propagate that result.
        var pipeline = PipelineTestHelpers.CreateAlwaysFailPipeline();

        // Wire bytes: length=2, payload=[0x01, 0x00] — a structurally valid
        // transport frame, but the frame codec will reject it.
        var seq = PipelineTestHelpers.ToSequence(
            new byte[] { 0x00, 0x00, 0x00, 0x02, 0x01, 0x00 });

        var result = pipeline.Decode(ref seq, out _);

        Assert.AreEqual(FrameDecodeResult.InvalidFrameEncoding, result);
    }

    [TestMethod]
    public void Decode_FrameCodecReturnsInvalidEncoding_SequenceIsNotAdvanced()
    {
        // Atomicity: if a frame codec rejects the payload, the transport sequence
        // must not be consumed — the bytes must remain available for retry.
        var pipeline = PipelineTestHelpers.CreateAlwaysFailPipeline();
        var wireBytes = new byte[] { 0x00, 0x00, 0x00, 0x02, 0x01, 0x00 };
        var seq = PipelineTestHelpers.ToSequence(wireBytes);

        _ = pipeline.Decode(ref seq, out _);

        Assert.AreEqual(wireBytes.Length, seq.Length,
            "The transport sequence must not be advanced when a frame codec returns InvalidFrameEncoding.");
    }

    [TestMethod]
    public void Decode_InvalidFrameContent_SequenceIsNotAdvanced()
    {
        // Atomicity: transport succeeds but DefaultNetworkFrameCodec fails.
        // The original sequence length must be preserved.
        var wireBytes = new byte[] { 0x00, 0x00, 0x00, 0x01, 0x42 };
        var pipeline = PipelineTestHelpers.CreateStandardPipeline();
        var seq = PipelineTestHelpers.ToSequence(wireBytes);

        _ = pipeline.Decode(ref seq, out _);

        Assert.AreEqual(wireBytes.Length, seq.Length,
            "The transport sequence must not be advanced when the network frame codec returns InvalidFrameEncoding.");
    }

    // -------------------------------------------------------------------------
    // Fatal transport error
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Decode_CorruptLengthPrefix_ThrowsTransportDecodeException()
    {
        // A negative or over-limit length prefix is a fatal protocol violation.
        // The LengthPrefixedTransportCodec throws TransportDecodeException and
        // the pipeline must not suppress it.
        // [0xFF,0xFF,0xFF,0xFF] → signed int32 = -1 → invalid
        var pipeline = PipelineTestHelpers.CreateStandardPipeline();
        var seq = PipelineTestHelpers.ToSequence(
            new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0x00 });

        Assert.ThrowsExactly<TransportDecodeException>(() =>
            pipeline.Decode(ref seq, out _));
    }
}
