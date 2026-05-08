using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer0_Transport.Encoding;
using MWB.Networking.Layer1_Framing.Codec.Abstractions;
using MWB.Networking.Layer1_Framing.Codec.Frames;
using MWB.Networking.Layer1_Framing.Codecs.Default.Network;
using MWB.Networking.Layer1_Framing.Codecs.LengthPrefixed.Transport;
using MWB.Networking.Layer1_Framing.Codecs.Reverse.Frame;
using System.Buffers;

namespace MWB.Networking.Layer1_Framing.Pipeline.UnitTests.Helpers;

/// <summary>
/// Shared factory methods and assertion helpers for pipeline unit tests.
/// </summary>
internal static class PipelineTestHelpers
{
    // -------------------------------------------------------------------------
    // Standard pipeline configurations
    // -------------------------------------------------------------------------

    /// <summary>
    /// Creates the standard test pipeline:
    /// <see cref="DefaultNetworkFrameCodec"/> → [<see cref="ReverseFrameCodec"/>] → <see cref="LengthPrefixedTransportCodec"/>.
    /// </summary>
    internal static NetworkPipeline CreateStandardPipeline()
        => new(
            NullLogger.Instance,
            new DefaultNetworkFrameCodec(),
            [new ReverseFrameCodec()],
            new LengthPrefixedTransportCodec(NullLogger.Instance));

    /// <summary>
    /// Creates a pipeline with <em>no</em> intermediate frame codecs:
    /// <see cref="DefaultNetworkFrameCodec"/> → [] → <see cref="LengthPrefixedTransportCodec"/>.
    /// Used to verify that intermediate codecs are actually being applied.
    /// </summary>
    internal static NetworkPipeline CreateZeroCodecPipeline()
        => new(
            NullLogger.Instance,
            new DefaultNetworkFrameCodec(),
            [],
            new LengthPrefixedTransportCodec(NullLogger.Instance));

    /// <summary>
    /// Creates a pipeline with two <see cref="ReverseFrameCodec"/>s:
    /// <see cref="DefaultNetworkFrameCodec"/> → [Reverse, Reverse] → <see cref="LengthPrefixedTransportCodec"/>.
    /// Because reversing is its own inverse, double application is identical to
    /// zero intermediate codecs — useful for proving codec ordering.
    /// </summary>
    internal static NetworkPipeline CreateDoubleReversePipeline()
        => new(
            NullLogger.Instance,
            new DefaultNetworkFrameCodec(),
            [new ReverseFrameCodec(), new ReverseFrameCodec()],
            new LengthPrefixedTransportCodec(NullLogger.Instance));

    /// <summary>
    /// Creates a pipeline whose decode path always returns
    /// <see cref="Codec.FrameDecodeResult.InvalidFrameEncoding"/> from the
    /// <see cref="AlwaysFailFrameCodec"/> stage.
    /// Used to test atomicity: the transport sequence must not be advanced.
    /// </summary>
    internal static NetworkPipeline CreateAlwaysFailPipeline()
        => new(
            NullLogger.Instance,
            new DefaultNetworkFrameCodec(),
            [new AlwaysFailFrameCodec()],
            new LengthPrefixedTransportCodec(NullLogger.Instance));

    // -------------------------------------------------------------------------
    // ByteSegments helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Flattens all segments from a <see cref="ByteSegments"/> value into a
    /// single contiguous byte array.
    /// </summary>
    internal static byte[] ToBytes(ByteSegments segments)
        => segments.Segments.SelectMany(m => m.ToArray()).ToArray();

    // -------------------------------------------------------------------------
    // ReadOnlySequence<byte> helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Wraps a byte array in a single-segment <see cref="ReadOnlySequence{T}"/>.
    /// </summary>
    internal static ReadOnlySequence<byte> ToSequence(byte[] data)
        => new(data);

    // -------------------------------------------------------------------------
    // NetworkFrame assertion helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Asserts that <paramref name="actual"/> has the same fields as
    /// <paramref name="expected"/>.
    /// </summary>
    internal static void AssertFramesEqual(NetworkFrame expected, NetworkFrame actual, string? message = null)
    {
        var prefix = message is null ? string.Empty : $"{message}: ";

        Assert.AreEqual(expected.Kind,         actual.Kind,         $"{prefix}Kind mismatch");
        Assert.AreEqual(expected.EventType,    actual.EventType,    $"{prefix}EventType mismatch");
        Assert.AreEqual(expected.RequestId,    actual.RequestId,    $"{prefix}RequestId mismatch");
        Assert.AreEqual(expected.RequestType,  actual.RequestType,  $"{prefix}RequestType mismatch");
        Assert.AreEqual(expected.ResponseType, actual.ResponseType, $"{prefix}ResponseType mismatch");
        Assert.AreEqual(expected.StreamId,     actual.StreamId,     $"{prefix}StreamId mismatch");
        Assert.AreEqual(expected.StreamType,   actual.StreamType,   $"{prefix}StreamType mismatch");

        CollectionAssert.AreEqual(
            expected.Payload.ToArray(),
            actual.Payload.ToArray(),
            $"{prefix}Payload mismatch");
    }
}
