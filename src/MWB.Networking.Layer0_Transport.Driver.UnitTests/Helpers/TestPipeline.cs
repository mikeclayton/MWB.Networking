using Microsoft.Extensions.Logging.Abstractions;
using MWB.Networking.Layer1_Framing.Codec.Frames;
using MWB.Networking.Layer1_Framing.Codecs.Default.Network;
using MWB.Networking.Layer1_Framing.Codecs.LengthPrefixed.Transport;
using MWB.Networking.Layer1_Framing.Codecs.Null.Transport;
using MWB.Networking.Layer1_Framing.Pipeline;

namespace MWB.Networking.Layer0_Transport.Driver.UnitTests.Helpers;

/// <summary>
/// Factory helpers for creating <see cref="NetworkPipeline"/> instances in tests.
///
/// <list type="bullet">
///   <item>
///     <see cref="CreateLengthPrefixed"/> — the primary pipeline: uses a 4-byte big-endian
///     length-prefix transport codec, which correctly returns <c>NeedsMoreData</c> when a
///     frame is split across multiple reads. Use this for tests that exercise accumulation.
///   </item>
///   <item>
///     <see cref="CreateNullTransport"/> — treats ALL available bytes as a single complete
///     transport frame on each decode attempt. Use this for tests that inject raw corrupt
///     bytes to provoke <c>InvalidFrameEncoding</c>.
///   </item>
/// </list>
/// </summary>
internal static class TestPipeline
{
    // ------------------------------------------------------------------
    // Pipeline factories
    // ------------------------------------------------------------------

    /// <summary>
    /// Creates a pipeline backed by <see cref="LengthPrefixedTransportCodec"/> and
    /// <see cref="DefaultNetworkFrameCodec"/> with no intermediate frame codecs.
    /// </summary>
    internal static NetworkPipeline CreateLengthPrefixed() =>
        new NetworkPipeline(
            new DefaultNetworkFrameCodec(),
            [],
            new LengthPrefixedTransportCodec(NullLogger.Instance));

    /// <summary>
    /// Creates a pipeline backed by <see cref="NullTransportCodec"/> (all bytes = one frame)
    /// and <see cref="DefaultNetworkFrameCodec"/> with no intermediate frame codecs.
    /// </summary>
    internal static NetworkPipeline CreateNullTransport() =>
        new NetworkPipeline(
            new DefaultNetworkFrameCodec(),
            [],
            new NullTransportCodec());

    // ------------------------------------------------------------------
    // Encoding helper
    // ------------------------------------------------------------------

    /// <summary>
    /// Encodes <paramref name="frame"/> through <paramref name="pipeline"/> and returns
    /// the resulting wire bytes as a single, contiguous <see cref="byte"/> array.
    /// </summary>
    internal static byte[] EncodeToBytes(NetworkPipeline pipeline, NetworkFrame frame)
    {
        var segments = pipeline.Encode(frame);

        var totalLength = 0;
        foreach (var segment in segments)
        {
            totalLength += segment.Length;
        }

        var buffer = new byte[totalLength];
        var offset = 0;
        foreach (var segment in segments)
        {
            segment.CopyTo(buffer.AsMemory(offset));
            offset += segment.Length;
        }
        return buffer;
    }

    // ------------------------------------------------------------------
    // Frame comparison helper
    // ------------------------------------------------------------------

    /// <summary>
    /// Asserts that all semantic fields and the payload of <paramref name="actual"/>
    /// match <paramref name="expected"/>, since <see cref="NetworkFrame"/> does not
    /// override <c>Equals</c>.
    /// </summary>
    internal static void AssertFramesEqual(NetworkFrame expected, NetworkFrame actual)
    {
        Assert.AreEqual(expected.Kind, actual.Kind, "Kind mismatch.");
        Assert.AreEqual(expected.EventType, actual.EventType, "EventType mismatch.");
        Assert.AreEqual(expected.RequestId, actual.RequestId, "RequestId mismatch.");
        Assert.AreEqual(expected.RequestType, actual.RequestType, "RequestType mismatch.");
        Assert.AreEqual(expected.ResponseType, actual.ResponseType, "ResponseType mismatch.");
        Assert.AreEqual(expected.StreamId, actual.StreamId, "StreamId mismatch.");
        Assert.AreEqual(expected.StreamType, actual.StreamType, "StreamType mismatch.");
        CollectionAssert.AreEqual(
            expected.Payload.ToArray(),
            actual.Payload.ToArray(),
            "Payload mismatch.");
    }
}
